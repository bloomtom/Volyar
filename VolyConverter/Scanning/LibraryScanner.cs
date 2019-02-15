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

namespace VolyConverter.Scanning
{
    public class LibraryScanner : ScanItem
    {
        private readonly IDistinctQueueProcessor<IConversionItem> converter;
        private readonly DbContextOptions<VolyContext> dbOptions;
        private readonly IStorage storageBackend;
        private readonly ILogger log;

        private readonly IEnumerable<ConversionPlugin> conversionPlugins;

        private readonly ILibrary library;

        public LibraryScanner(ILibrary library, IStorage storageBackend, IDistinctQueueProcessor<IConversionItem> converter, DbContextOptions<VolyContext> dbOptions, IEnumerable<ConversionPlugin> conversionPlugins, ILogger log) : base(ScanType.Library, library.Name)
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
                    // Delete db items not found in scan.
                    context.Media.RemoveRange(currentLibrary.Values);
                }

                context.SaveChanges();

                log.LogInformation($"Scanning library {library.Name} complete.");

                return true;
            }
        }

        private void ScheduleConversion(ILibrary library, HashSet<IQuality> quality, string sourcePath, DateTimeOffset lastWrite, string seriesName, string sourceHash, string outFilename)
        {
            converter.AddItem(new ConversionItem(sourcePath, library.TempPath, outFilename, quality, library.ForceFramerate, (sender, result) =>
            {
                var addedFiles = new List<string>();

                using (var innerContext = new VolyContext(dbOptions))
                {
                    // Save the new media item to db.
                    var newMedia = new VolyDatabase.MediaItem()
                    {
                        SourcePath = sourcePath,
                        SourceModified = lastWrite,
                        SourceHash = sourceHash,
                        Duration = result.FileDuration,
                        IndexName = Path.GetFileName(result.DashFilePath),
                        IndexHash = Hashing.HashFileMd5(result.DashFilePath),
                        LibraryName = library.Name,
                        Name = Path.GetFileNameWithoutExtension(sourcePath),
                        SeriesName = seriesName,
                        Metadata = Newtonsoft.Json.JsonConvert.SerializeObject(result.Metadata)
                    };
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
                    AddMediaFilesToMedia(innerContext, newMedia.MediaId, library.TempPath, result.MediaFiles);

                    HandleSourceFate(sourcePath);

                    // Set files on disk variable for the storage backend operations below.
                    addedFiles.Add(result.DashFilePath);
                    addedFiles.AddRange(result.MediaFiles.Select(x => Path.Combine(library.TempPath, x)));

                    // Set up progress reporting.
                    var progress = sender.Progress.ToList();
                    var uploadProgress = addedFiles.Select(x => new DescribedProgress($"Upload {x}", 0)).ToList();
                    progress.AddRange(uploadProgress);
                    sender.Progress = progress;

                    UploadFiles(addedFiles, uploadProgress);

                    RunPlugins(library, innerContext, sender, newMedia, result, ConversionType.Conversion);

                    innerContext.SaveChanges();
                    log.LogInformation($"Converted {sourcePath}");
                }
            },
            (ex) =>
            {
                log.LogError($"Failed to convert {sourcePath} -- Ex: {ex.ToString()}");
            }));
        }

        private void ScheduleReconversion(ILibrary library, HashSet<IQuality> quality, string sourcePath, DateTimeOffset lastWrite, int mediaId, string sourceHash, string outFilename)
        {
            converter.AddItem(new ConversionItem(sourcePath, library.TempPath, outFilename, quality, library.ForceFramerate, (sender, result) =>
            {
                var addedFiles = new List<string>();
                var removedFiles = new List<string>();

                using (var innerContext = new VolyContext(dbOptions))
                {
                    removedFiles = innerContext.MediaFile.Where(x => x.MediaId == mediaId).Select(x => x.Filename).ToList();
                    innerContext.MediaFile.RemoveRange(innerContext.MediaFile.Where(x => x.MediaId == mediaId));
                    var inDb = innerContext.Media.Where(x => x.MediaId == mediaId).SingleOrDefault();
                    if (inDb != null)
                    {
                        inDb.SourcePath = sourcePath;
                        inDb.SourceModified = lastWrite;
                        inDb.SourceHash = sourceHash;
                        inDb.Duration = result.FileDuration;
                        inDb.IndexName = Path.GetFileName(result.DashFilePath);
                        inDb.IndexHash = Hashing.HashFileMd5(result.DashFilePath);
                        inDb.Metadata = Newtonsoft.Json.JsonConvert.SerializeObject(result.Metadata);
                        innerContext.Update(inDb);

                        AddMediaFilesToMedia(innerContext, inDb.MediaId, library.TempPath, result.MediaFiles);

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
                    var deleteProgress = new DescribedProgress("Delete Old", 0);
                    progress.Add(deleteProgress);
                    sender.Progress = progress;

                    UploadFiles(addedFiles, uploadProgress);

                    // Delete files.
                    int deleteCount = 0;
                    foreach (var file in removedFiles)
                    {
                        storageBackend.DeleteAsync(Path.GetFileName(file));
                        deleteCount++;
                        deleteProgress.Progress = deleteCount / removedFiles.Count;
                    }

                    RunPlugins(library, innerContext, sender, inDb, result, ConversionType.Conversion);

                    innerContext.SaveChanges();
                    log.LogInformation($"Converted {sourcePath}");
                }
            },
            (ex) =>
            {
                log.LogError($"Failed to re-convert {sourcePath}, database not updated. -- Ex: {ex.ToString()}");
            }));
        }

        private void UploadFiles(List<string> addedFiles, List<DescribedProgress> uploadProgress)
        {
            for (int i = 0; i < addedFiles.Count; i++)
            {
                // Avoid including i directly in the following without Waiting on the task, or i will be changed during execution.
                storageBackend.UploadAsync(Path.GetFileName(addedFiles[i]), addedFiles[i], true, progress: new NaiveProgress<ICopyProgress>(new Action<ICopyProgress>((e) =>
                {
                    uploadProgress[i].Progress = e.PercentComplete;
                }))).Wait();
            }
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

        private void RunPlugins(ILibrary library, VolyContext context, IConversionItem conversionItem, VolyDatabase.MediaItem mediaItem, DashEncodeResult result, ConversionType type)
        {
            foreach (var plugin in conversionPlugins)
            {
                try
                {
                    plugin.Action.Invoke(new ConversionPluginArgs(library, context, conversionItem, mediaItem, result, type, log));
                }
                catch (Exception ex)
                {
                    log.LogWarning($"Failed to run plugin {plugin.Name} on item {conversionItem.SourcePath} in library {library.Name}. Ex: {ex.ToString()}");
                }
            }
        }

        private void AddMediaFilesToMedia(VolyContext context, int mediaId, string basePath, IEnumerable<string> mediaFiles)
        {
            foreach (var f in mediaFiles)
            {
                var fInfo = new FileInfo(Path.Combine(basePath, f));
                if (!fInfo.Exists)
                {
                    log.LogError($"Expected file belonging to {mediaId} but did not find it at: {fInfo.FullName}");
                    continue;
                }

                context.MediaFile.Add(new MediaFile()
                {
                    MediaId = mediaId,
                    Filename = f,
                    Filesize = fInfo.Length
                });
            }
        }
    }
}
