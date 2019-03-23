using DEnc;
using DQP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Threading.Tasks;
using VolyConverter.Conversion;
using MStorage;
using HttpProgress;
using NaiveProgress;
using VolyExports;
using VolyDatabase;
using Microsoft.EntityFrameworkCore;
using MStorage.WebStorage;
using VolyConverter.Plugin;
using VolyFiles;

namespace VolyConverter.Scanning
{
    public class LibraryScanner : ScanItem
    {
        private readonly IDistinctQueueProcessor<IConversionItem> converter;
        private readonly DbContextOptions<VolyContext> dbOptions;
        private readonly IStorage storageBackend;
        private readonly ILogger log;

        private readonly IEnumerable<IConversionPlugin> conversionPlugins;

        private readonly ILibrary library;

        public LibraryScanner(ILibrary library, IStorage storageBackend, IDistinctQueueProcessor<IConversionItem> converter, DbContextOptions<VolyContext> dbOptions, IEnumerable<IConversionPlugin> conversionPlugins, ILogger log) : base(ScanType.Library, library.Name)
        {
            this.library = library;
            this.storageBackend = storageBackend;
            this.converter = converter;
            this.dbOptions = dbOptions;
            this.conversionPlugins = conversionPlugins;
            this.log = log;
        }

        public override bool Scan()
        {
            if (!library.Enable)
            {
                log.LogInformation($"Library {library.Name} skipped (disabled).");
                return false;
            }
            else if (CancellationToken.IsCancellationRequested)
            {
                log.LogInformation($"Library {library.Name} skipped (cancelled).");
                return false;
            }
            else if (!Directory.Exists(library.OriginPath))
            {
                log.LogWarning($"Scanning library {library.Name} failed: Directory does not exist.");
                return false;
            }

            using (var context = new VolyContext(dbOptions))
            {
                var currentLibrary = context.Media
                    .Where(x => x.LibraryName == library.Name)
                    .Select(x => new VolyDatabase.MediaItem() { MediaId = x.MediaId, SourcePath = x.SourcePath, SourceModified = x.SourceModified, SourceHash = x.SourceHash })
                    .ToDictionary(k => k.SourcePath);
                var quality = new HashSet<IQuality>(library.Qualities);

                log.LogInformation($"Scanning library {library.Name}...");

                var o = new EnumerationOptions() { RecurseSubdirectories = true, IgnoreInaccessible = true };
                var foundFiles =
                    new FileSystemEnumerable<(string Path, DateTimeOffset lastModified, bool zeroLength)>(library.OriginPath,
                    (ref FileSystemEntry entry) => (entry.ToFullPath(), entry.CreationTimeUtc > entry.LastWriteTimeUtc ? entry.CreationTimeUtc : entry.LastWriteTimeUtc, entry.Length == 0), o)
                    {
                        ShouldIncludePredicate = (ref FileSystemEntry entry) => { return !entry.IsDirectory; }
                    };

                foreach (var file in foundFiles)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        log.LogInformation($"Halted scan of library {library.Name} (cancelled).");
                        return false;
                    }

                    var extension = Path.GetExtension(file.Path);
                    if (!library.ValidExtensions.Contains(extension)) { continue; }

                    bool existsInDb = currentLibrary.TryGetValue(file.Path, out var existingEntry);
                    if (existsInDb) { currentLibrary.Remove(file.Path); } // Remove to track missing entries for later.

                    if (file.zeroLength)
                    {
                        continue;
                    }
                    else if (existsInDb)
                    {
                        // Item exists, check if update is needed.
                        if (file.lastModified > existingEntry.SourceModified)
                        {
                            string sourceHash = Hashing.HashFileMd5(file.Path);
                            if (sourceHash != existingEntry.SourceHash)
                            {
                                // Update needed.
                                log.LogInformation($"Scheduling re-conversion of {file.Path}");
                                string outFilename = $"{sourceHash.Substring(0, 8)}_{Path.GetFileNameWithoutExtension(file.Path)}";
                                ScheduleReconversion(library, quality, file.Path, file.lastModified, existingEntry.MediaId, sourceHash, outFilename);
                            }
                        }
                    }
                    else
                    {
                        // Media doesn't exist in db, make it.
                        log.LogInformation($"Scheduling conversion of {file.Path}");
                        string sourceHash = Hashing.HashFileMd5(file.Path);
                        string outFilename = $"{sourceHash.Substring(0, 8)}_{Path.GetFileNameWithoutExtension(file.Path)}";
                        ScheduleConversion(library, quality, file.Path, file.lastModified, (new DirectoryInfo(Directory.GetParent(file.Path).FullName)).Name, sourceHash, outFilename);
                    }
                }

                if (library.DeleteWithSource)
                {
                    // Queue delete for items not found in scan.
                    var toAdd = currentLibrary.Values.Select(x => x.MediaId);
                    var inDb = context.PendingDeletions.Where(x => toAdd.Contains(x.MediaId)).Select(y => y.MediaId);
                    var notInDb = currentLibrary.Values.Where(x => !inDb.Contains(x.MediaId));
                    context.PendingDeletions.AddRange(notInDb.Select(x => new PendingDeletion() { MediaId = x.MediaId, Version = -1, Requestor = DeleteRequestor.Scan }));
                }

                context.SaveChanges();

                log.LogInformation($"Scanning library {library.Name} complete.");

                return true;
            }
        }

        private void ScheduleConversion(ILibrary library, HashSet<IQuality> quality, string sourcePath, DateTimeOffset lastWrite, string seriesName, string sourceHash, string outFilename)
        {
            var newMedia = new VolyDatabase.MediaItem()
            {
                SourcePath = sourcePath,
                SourceModified = lastWrite,
                SourceHash = sourceHash,
                LibraryName = library.Name,
                Name = Path.GetFileNameWithoutExtension(sourcePath),
                SeriesName = seriesName
            };
            var conversionItem = new ConversionItem(newMedia.SeriesName, newMedia.Name, sourcePath, library.TempPath, outFilename, quality, library.ForceFramerate, (sender, result) =>
            {
                var addedFiles = new List<string>();

                using (var innerContext = new VolyContext(dbOptions))
                {
                    // Save the new media item to db.
                    newMedia.Duration = result.FileDuration;
                    newMedia.IndexName = Path.GetFileName(result.DashFilePath);
                    newMedia.IndexHash = Hashing.HashFileMd5(result.DashFilePath);
                    newMedia.Metadata = Newtonsoft.Json.JsonConvert.SerializeObject(result.Metadata);

                    innerContext.Media.Add(newMedia);

                    innerContext.SaveChanges();

                    // Update the transaction log.
                    innerContext.TransactionLog.Add(new TransactionLog()
                    {
                        TableName = "MediaItem",
                        Type = TransactionType.Insert,
                        Key = newMedia.MediaId
                    });

                    // Add associated files to the db.
                    var files = new List<string>(result.MediaFiles)
                    {
                        Path.GetFileName(result.DashFilePath)
                    };
                    AddMediaFilesToMedia(innerContext, newMedia.MediaId, library.TempPath, files);

                    HandleSourceFate(sourcePath);

                    // Set files on disk variable for the storage backend operations below.
                    addedFiles.Add(result.DashFilePath);
                    addedFiles.AddRange(result.MediaFiles.Select(x => Path.Combine(library.TempPath, x)));

                    // Set up progress reporting.
                    var progress = sender.Progress.ToList();
                    var uploadProgress = addedFiles.Select(x => new DescribedProgress($"Upload {x}", 0)).ToList();
                    progress.AddRange(uploadProgress);
                    sender.Progress = progress;

                    var fileManagement = new FileManagement(storageBackend, log);
                    fileManagement.UploadFiles(addedFiles, uploadProgress);

                    RunPostPlugins(library, innerContext, sender, newMedia, result, ConversionType.Conversion);

                    innerContext.SaveChanges();
                    log.LogInformation($"Converted {sourcePath}");
                }
            },
            (ex) =>
            {
                log.LogError($"Failed to convert {sourcePath} -- Ex: {ex.ToString()}");
            });

            RunPrePlugins(library, conversionItem, newMedia, ConversionType.Conversion);

            converter.AddItem(conversionItem);
        }

        private void ScheduleReconversion(ILibrary library, HashSet<IQuality> quality, string sourcePath, DateTimeOffset lastWrite, int mediaId, string sourceHash, string outFilename)
        {
            var newMedia = new VolyDatabase.MediaItem()
            {
                SourcePath = sourcePath,
                SourceModified = lastWrite,
                SourceHash = sourceHash,
                LibraryName = library.Name,
                Name = Path.GetFileNameWithoutExtension(sourcePath),
            };
            var conversionItem = new ConversionItem(newMedia.SeriesName, newMedia.Name, sourcePath, library.TempPath, outFilename, quality, library.ForceFramerate, (sender, result) =>
            {
                var addedFiles = new List<string>();

                using (var innerContext = new VolyContext(dbOptions))
                {
                    int oldVersion = innerContext.Media.Where(x => x.MediaId == mediaId).SingleOrDefault().Version;
                    int newVersion = innerContext.MediaFiles.Where(x => x.MediaId == mediaId).Max(y => y.Version) + 1;

                    var inDb = innerContext.Media.Where(x => x.MediaId == mediaId).SingleOrDefault();
                    if (inDb != null)
                    {
                        inDb.SourcePath = newMedia.SourcePath;
                        inDb.SourceModified = newMedia.SourceModified;
                        inDb.SourceHash = newMedia.SourceHash;
                        inDb.SeriesName = newMedia.SeriesName;
                        inDb.Name = newMedia.Name;

                        if (newMedia.SeasonNumber != 0) { inDb.SeasonNumber = newMedia.SeasonNumber; }
                        if (newMedia.EpisodeNumber != 0) { inDb.EpisodeNumber = newMedia.EpisodeNumber; }
                        if (newMedia.AbsoluteEpisodeNumber != 0) { inDb.AbsoluteEpisodeNumber = newMedia.AbsoluteEpisodeNumber; }
                        if (newMedia.ImdbId != null) { inDb.ImdbId = newMedia.ImdbId; }
                        if (newMedia.TmdbId != null) { inDb.TmdbId = newMedia.TmdbId; }
                        if (newMedia.TvdbId != null) { inDb.TvdbId = newMedia.TvdbId; }
                        if (newMedia.TvmazeId != null) { inDb.TvmazeId = newMedia.TvmazeId; }

                        inDb.Duration = result.FileDuration;
                        inDb.IndexName = Path.GetFileName(result.DashFilePath);
                        inDb.IndexHash = Hashing.HashFileMd5(result.DashFilePath);
                        inDb.Metadata = Newtonsoft.Json.JsonConvert.SerializeObject(result.Metadata);
                        innerContext.Update(inDb);

                        var files = new List<string>(result.MediaFiles)
                        {
                            Path.GetFileName(result.DashFilePath)
                        };
                        AddMediaFilesToMedia(innerContext, inDb.MediaId, library.TempPath, result.MediaFiles, newVersion);

                        HandleSourceFate(sourcePath);
                    }
                    else
                    {
                        log.LogError($"Failed to find media item {mediaId}");
                    }

                    // Set files on disk variable for the storage backend operations below.
                    addedFiles.Add(result.DashFilePath);
                    addedFiles.AddRange(result.MediaFiles);

                    // Set up progress reporting.
                    var progress = sender.Progress.ToList();
                    var uploadProgress = addedFiles.Select(x => new DescribedProgress($"Upload {x}", 0)).ToList();
                    progress.AddRange(uploadProgress);
                    sender.Progress = progress;

                    var fileManagement = new FileManagement(storageBackend, log);
                    fileManagement.UploadFiles(addedFiles, uploadProgress);

                    innerContext.PendingDeletions.Add(new PendingDeletion()
                    {
                        MediaId = mediaId,
                        Version = oldVersion,
                        Requestor = DeleteRequestor.Scan
                    });

                    RunPostPlugins(library, innerContext, sender, inDb, result, ConversionType.Conversion);

                    innerContext.SaveChanges();
                    log.LogInformation($"Converted {sourcePath}");
                }
            },
            (ex) =>
            {
                log.LogError($"Failed to re-convert {sourcePath}, database not updated. -- Ex: {ex.ToString()}");
            });

            RunPrePlugins(library, conversionItem, newMedia, ConversionType.Reconversion);

            converter.AddItem(conversionItem);
        }

        private void HandleSourceFate(string sourcePath)
        {
            switch (library.SourceHandling.ToLowerInvariant())
            {
                case "none":
                    break;
                case "truncate":
                    File.WriteAllBytes(sourcePath, new byte[0]);
                    break;
                case "delete":
                    File.Delete(sourcePath);
                    break;
                default:
                    log.LogWarning($"Invalid source handling for library {library.Name}. Supported options are: none, truncate, delete.");
                    break;
            }
        }

        private void RunPrePlugins(ILibrary library, IConversionItem conversionItem, VolyDatabase.MediaItem mediaItem, ConversionType type)
        {
            foreach (var plugin in conversionPlugins)
            {
                if (plugin is PreConversionPlugin pre)
                {
                    try
                    {
                        pre.Action.Invoke(new PreConversionPluginArgs(library, conversionItem, mediaItem, type, log));
                    }
                    catch (Exception ex)
                    {
                        log.LogWarning($"Failed to run pre-plugin {plugin.Name} on item {conversionItem.SourcePath} in library {library.Name}. Ex: {ex.ToString()}");
                    }
                }
            }
        }

        private void RunPostPlugins(ILibrary library, VolyContext context, IConversionItem conversionItem, VolyDatabase.MediaItem mediaItem, DashEncodeResult result, ConversionType type)
        {
            foreach (var plugin in conversionPlugins)
            {
                if (plugin is PostConversionPlugin post)
                {
                    try
                    {
                        post.Action.Invoke(new PostConversionPluginArgs(library, context, conversionItem, mediaItem, result, type, log));
                    }
                    catch (Exception ex)
                    {
                        log.LogWarning($"Failed to run post-plugin {plugin.Name} on item {conversionItem.SourcePath} in library {library.Name}. Ex: {ex.ToString()}");
                    }
                }
            }
        }

        private void AddMediaFilesToMedia(VolyContext context, int mediaId, string basePath, IEnumerable<string> mediaFiles, int version = 0)
        {
            foreach (var f in mediaFiles)
            {
                var fInfo = new FileInfo(Path.Combine(basePath, f));
                if (!fInfo.Exists)
                {
                    log.LogError($"Expected file belonging to {mediaId} but did not find it at: {fInfo.FullName}");
                    continue;
                }

                context.MediaFiles.Add(new MediaFile()
                {
                    MediaId = mediaId,
                    Filename = f,
                    Filesize = fInfo.Length,
                    Version = version
                });
            }
        }
    }
}
