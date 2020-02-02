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
    public class ScanFile
    {
        public string Path { get; private set; }
        public DateTimeOffset LastModified { get; private set; }
        public bool ZeroLength { get; private set; }

        private string sourceHash = null;
        public string SourceHash
        {
            get
            {
                if (sourceHash != null) { return sourceHash; }
                sourceHash = Hashing.HashFileMd5(Path);
                return sourceHash;
            }
        }

        private string outFilename = null;
        public string OutFilename
        {
            get
            {
                if (outFilename != null) { return outFilename; }
                outFilename = System.Net.WebUtility.UrlEncode($"{SourceHash.Substring(0, 8)}_{System.IO.Path.GetFileNameWithoutExtension(Path)}");
                return outFilename;
            }
        }

        public ScanFile(string path, DateTimeOffset lastModified, bool zeroLength)
        {
            Path = path;
            LastModified = lastModified;
            ZeroLength = zeroLength;
        }
    }

    public class LibraryScanner : ScanItem
    {
        private readonly IDistinctQueueProcessor<IConversionItem> converter;
        private readonly DbContextOptions<VolyContext> dbOptions;
        private readonly IStorage storageBackend;
        private readonly ILogger log;

        private readonly IEnumerable<IConversionPlugin> conversionPlugins;

        private readonly ILibrary library;
        private readonly HashSet<string> scanTheseFilesOnly = null;

        public LibraryScanner(ILibrary library, IStorage storageBackend, IDistinctQueueProcessor<IConversionItem> converter, DbContextOptions<VolyContext> dbOptions, IEnumerable<IConversionPlugin> conversionPlugins, ILogger log) : base(ScanType.Library, library.Name)
        {
            this.library = library;
            this.storageBackend = storageBackend;
            this.converter = converter;
            this.dbOptions = dbOptions;
            this.conversionPlugins = conversionPlugins;
            this.log = log;
        }

        public LibraryScanner(ILibrary library, IStorage storageBackend, IDistinctQueueProcessor<IConversionItem> converter, DbContextOptions<VolyContext> dbOptions, IEnumerable<IConversionPlugin> conversionPlugins, ILogger log, IEnumerable<string> scanTheseFilesOnly) : base(ScanType.FilteredLibrary, library.Name)
        {
            this.library = library;
            this.storageBackend = storageBackend;
            this.converter = converter;
            this.dbOptions = dbOptions;
            this.conversionPlugins = conversionPlugins;
            this.log = log;
            this.scanTheseFilesOnly = new HashSet<string>(scanTheseFilesOnly);
            Name = GenerateFilteredName(library.Name, this.scanTheseFilesOnly); // This is needed to allow multiple queued scans with different file filters.
        }

        private static string GenerateFilteredName(string name, HashSet<string> scanTheseFilesOnly)
        {
            if (scanTheseFilesOnly.Count == 0)
            {
                return name;
            }
            if (scanTheseFilesOnly.Count == 1)
            {
                return name + " " + scanTheseFilesOnly.First();
            }
            long hash = 1009;
            foreach (var item in scanTheseFilesOnly)
            {
                hash = unchecked(hash * 7979 + item.GetHashCode());
            }
            return name + " " + hash.ToString();
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
                IEnumerable<VolyDatabase.MediaItem> contextMediaItems;
                contextMediaItems = context.Media
                    .Where(x => x.LibraryName == library.Name)
                    .Select(x => new VolyDatabase.MediaItem() { MediaId = x.MediaId, SourcePath = x.SourcePath, SourceModified = x.SourceModified, SourceHash = x.SourceHash, IndexHash = x.IndexHash });

                if (scanTheseFilesOnly != null)
                {
                    // Reduce the data set to only items which are in our scan selection.
                    var extensionlessSTFO = scanTheseFilesOnly.Select(x => Path.GetFileNameWithoutExtension(x)).ToHashSet();
                    contextMediaItems = contextMediaItems.Where(x => extensionlessSTFO.Contains(Path.GetFileNameWithoutExtension(x.SourcePath)));
                }

                var currentLibrary = new Dictionary<string, VolyDatabase.MediaItem>();
                var dbMediaItemsNotFound = new HashSet<int>();
                foreach (var item in contextMediaItems)
                {
                    currentLibrary[Path.GetFileNameWithoutExtension(item.SourcePath)] = item;
                    dbMediaItemsNotFound.Add(item.MediaId);
                }

                var quality = new HashSet<IQuality>(library.Qualities);

                IEnumerable<ScanFile> foundFiles;
                if (scanTheseFilesOnly == null)
                {
                    log.LogInformation($"Scanning library {library.Name}...");
                    foundFiles = GetLibraryFiles(library.OriginPath);
                }
                else if (scanTheseFilesOnly.Count() == 0)
                {
                    log.LogWarning("Library scan was started with a specified scan set, but the scan set was empty.");
                    return false;
                }
                else
                {
                    log.LogInformation($"Scanning library {library.Name} for {scanTheseFilesOnly.Count()} files...");
                    foundFiles = GetLibraryFiles(library.OriginPath, scanTheseFilesOnly);
                }

                foreach (var file in foundFiles)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        log.LogInformation($"Halted scan of library {library.Name} (cancelled).");
                        return false;
                    }

                    var extension = Path.GetExtension(file.Path);
                    if (!library.ValidExtensions.Contains(extension)) { continue; }

                    bool existsInDb = currentLibrary.TryGetValue(Path.GetFileNameWithoutExtension(file.Path), out var existingEntry);
                    if (existsInDb) { dbMediaItemsNotFound.Remove(existingEntry.MediaId); } // Remove to track missing entries for later.

                    string seriesName = new DirectoryInfo(Directory.GetParent(file.Path).FullName).Name;
                    if (file.ZeroLength)
                    {
                        continue;
                    }
                    else if (existsInDb && string.IsNullOrWhiteSpace(existingEntry.IndexHash)) // Indicates previous failed conversion.
                    {
                        log.LogInformation($"Scheduling conversion of previously failed item at path {file.Path}");
                        using (var deletionContext = new VolyContext(dbOptions))
                        {
                            deletionContext.Media.Remove(existingEntry);
                            deletionContext.SaveChanges();
                        }
                        ScheduleConversion(library, quality, file.Path, file.LastModified, seriesName, file);
                    }
                    else if (existsInDb)
                    {
                        TryReconvert(quality, file, existingEntry, seriesName);
                    }
                    else
                    {
                        var oldVersion = context.Media.Where(x => x.SourceHash == file.SourceHash).SingleOrDefault();
                        if (oldVersion != null)
                        {
                            log.LogInformation($"Discovered renamed file. {oldVersion.SourcePath} => {file.Path}");
                            dbMediaItemsNotFound.Remove(oldVersion.MediaId);
                            oldVersion.SourcePath = file.Path;
                            context.Media.Update(oldVersion);
                            if (!TryReconvert(quality, file, oldVersion, seriesName))
                            {
                                // Not being reconverted, just update metadata.
                                RunPrePlugins(library, null, oldVersion, ConversionType.MetadataOnly);
                                context.Media.Update(oldVersion);
                                RunPostPlugins(library, context, null, oldVersion, null, ConversionType.MetadataOnly);
                            }
                        }
                        else
                        {
                            // Media doesn't exist in db, make it.
                            log.LogInformation($"Scheduling conversion of {file.Path}");
                            ScheduleConversion(library, quality, file.Path, file.LastModified, seriesName, file);
                        }
                    }
                }

                if (library.DeleteWithSource)
                {
                    // Queue delete for items not found in scan.
                    var inDb = context.PendingDeletions.Where(x => dbMediaItemsNotFound.Contains(x.MediaId)).Select(y => y.MediaId);
                    dbMediaItemsNotFound.ExceptWith(inDb);
                    context.PendingDeletions.AddRange(dbMediaItemsNotFound.Select(x => new PendingDeletion() { MediaId = x, Version = -1, Requestor = DeleteRequester.Scan }));
                }

                context.SaveChanges();

                log.LogInformation($"Scanning library {library.Name} complete.");

                return true;
            }
        }

        private static IEnumerable<ScanFile> GetLibraryFiles(string libraryPath)
        {
            var enumerationOptions = new EnumerationOptions() { RecurseSubdirectories = true, IgnoreInaccessible = true };
            return
                new FileSystemEnumerable<(string Path, DateTimeOffset lastModified, bool zeroLength)>(libraryPath,
                (ref FileSystemEntry entry) => (entry.ToFullPath(), CalculateLastAccess(entry.CreationTimeUtc, entry.LastWriteTimeUtc), entry.Length == 0), enumerationOptions)
                {
                    ShouldIncludePredicate = (ref FileSystemEntry entry) => { return !entry.IsDirectory; }
                }.Select(x => new ScanFile(x.Path, x.lastModified, x.zeroLength));
        }

        private static DateTimeOffset CalculateLastAccess(DateTimeOffset created, DateTimeOffset lastWritten)
        {
            return created > lastWritten ? created : lastWritten;
        }

        private IEnumerable<ScanFile> GetLibraryFiles(string libraryPath, IEnumerable<string> filter)
        {
            libraryPath = Path.GetFullPath(libraryPath);
            foreach (var file in filter)
            {
                if (Path.GetFullPath(file).StartsWith(libraryPath))
                {
                    FileInfo info = new FileInfo(file);
                    if (info.Exists)
                    {
                        try
                        {
                            using (info.OpenRead()) { }
                        }
                        catch (Exception ex)
                        {
                            log.LogWarning($"Failed to open file {file} exception {ex.Message}");
                            continue;
                        }
                        yield return new ScanFile(info.FullName, CalculateLastAccess(info.CreationTimeUtc, info.LastWriteTimeUtc), info.Length == 0);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if conversion was performed
        /// </summary>
        private bool TryReconvert(HashSet<IQuality> quality, ScanFile file, VolyDatabase.MediaItem existingEntry, string seriesName)
        {
            // Item exists, check if update is needed.
            // Item last modified date is newer than source, or older than source by more than a day.
            if (file.LastModified > existingEntry.SourceModified || existingEntry.SourceModified - file.LastModified > TimeSpan.FromDays(1))
            {
                string outFilename = $"{file.SourceHash.Substring(0, 8)}_{Path.GetFileNameWithoutExtension(file.Path)}";
                if (file.SourceHash != existingEntry.SourceHash)
                {
                    // Update needed.
                    log.LogInformation($"Scheduling re-conversion of {file.Path}");
                    ScheduleReconversion(library, quality, file.Path, file.LastModified, existingEntry.MediaId, seriesName, file);
                    return true;
                }
            }
            return false;
        }

        private void ScheduleConversion(ILibrary library, HashSet<IQuality> quality, string sourcePath, DateTimeOffset lastWrite, string seriesName, ScanFile file)
        {
            var newMedia = new VolyDatabase.MediaItem()
            {
                SourcePath = sourcePath,
                SourceModified = lastWrite,
                SourceHash = file.SourceHash,
                LibraryName = library.Name,
                Name = Path.GetFileNameWithoutExtension(sourcePath),
                SeriesName = seriesName
            };
            var conversionItem = new ConversionItem(newMedia.SeriesName, newMedia.Name, sourcePath, library.TempPath, file.OutFilename, quality, library.ForceFramerate, (sender, result) =>
            {
                int? addedKey = null;
                try
                {
                    using (var innerContext = new VolyContext(dbOptions))
                    {
                        // Save the new media item to db.
                        newMedia.Duration = result.FileDuration;
                        newMedia.IndexName = Path.GetFileName(result.DashFilePath);
                        newMedia.IndexHash = "";
                        newMedia.Metadata = Newtonsoft.Json.JsonConvert.SerializeObject(result.Metadata);
                        innerContext.Media.Add(newMedia);
                        innerContext.SaveChanges(); // Need to save changes now to yield a MediaId from the database.

                        // Set to allow deletion of this key on failure.
                        addedKey = newMedia.MediaId;

                        // Must be done after initial push to allow reconvert on error.
                        newMedia.IndexHash = Hashing.HashFileMd5(result.DashFilePath);

                        // Update the transaction log.
                        innerContext.TransactionLog.Add(new TransactionLog()
                        {
                            TableName = TransactionTableType.MediaItem,
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
                        var addedFiles = new List<string>();
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
                }
                catch (Exception)
                {
                    if (addedKey != null)
                    {
                        try
                        {
                            using (var abortContext = new VolyContext(dbOptions))
                            {
                                abortContext.Media.Remove(abortContext.Media.Where(x => x.MediaId == addedKey.Value).SingleOrDefault());
                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogError($"Failed to abort media ID {addedKey.Value} at path {sourcePath}. This item may require manual reconversion. Ex: {ex.ToString()}");
                        }
                    }
                    throw;
                }
            },
            (ex) =>
            {
                log.LogError($"Failed to convert {sourcePath} -- Ex: {ex.ToString()}");
            });

            RunPrePlugins(library, conversionItem, newMedia, ConversionType.Conversion);

            converter.AddItem(conversionItem);
        }

        private void ScheduleReconversion(ILibrary library, HashSet<IQuality> quality, string sourcePath, DateTimeOffset lastWrite, int mediaId, string seriesName, ScanFile file)
        {
            var newMedia = new VolyDatabase.MediaItem()
            {
                SourcePath = sourcePath,
                SourceModified = lastWrite,
                SourceHash = file.SourceHash,
                LibraryName = library.Name,
                Name = Path.GetFileNameWithoutExtension(sourcePath),
                SeriesName = seriesName
            };
            var conversionItem = new ConversionItem(newMedia.SeriesName, newMedia.Name, sourcePath, library.TempPath, file.OutFilename, quality, library.ForceFramerate, (sender, result) =>
            {
                using (var innerContext = new VolyContext(dbOptions))
                {
                    int oldVersion = innerContext.Media.Where(x => x.MediaId == mediaId).SingleOrDefault().Version;
                    int newVersion = innerContext.MediaFiles.Where(x => x.MediaId == mediaId).Max(y => y.Version) + 1;

                    var inDb = innerContext.Media.Where(x => x.MediaId == mediaId).SingleOrDefault();
                    if (inDb != null)
                    {
                        inDb.Version = newVersion;
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
                        AddMediaFilesToMedia(innerContext, inDb.MediaId, library.TempPath, files, newVersion);

                        HandleSourceFate(sourcePath);
                    }
                    else
                    {
                        log.LogError($"Failed to find media item {mediaId}");
                    }

                    // Set files on disk variable for the storage backend operations below.
                    var addedFiles = new List<string>();
                    addedFiles.Add(result.DashFilePath);
                    addedFiles.AddRange(result.MediaFiles.Select(x => Path.Combine(library.TempPath, x)));

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
                        Requestor = DeleteRequester.Scan
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
                        log.LogWarning($"Failed to run pre-plugin {plugin.Name} on item {conversionItem?.SourcePath ?? mediaItem.SourcePath} in library {library.Name}. Ex: {ex.ToString()}");
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
                        log.LogWarning($"Failed to run post-plugin {plugin.Name} on item {conversionItem?.SourcePath ?? mediaItem.SourcePath} in library {library.Name}. Ex: {ex.ToString()}");
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
