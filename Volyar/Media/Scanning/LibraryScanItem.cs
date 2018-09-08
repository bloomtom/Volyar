using DEnc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Threading.Tasks;
using Volyar.Media.Conversion;
using Volyar.Models;

namespace Volyar.Media.Scanning
{
    public class LibraryScanItem : ScanItem
    {
        private readonly MediaConverter converter;
        private readonly DbContextOptions<VolyContext> dbOptions;
        private readonly ILogger log;

        private readonly bool deleteWithSource;
        private readonly bool truncateSource;

        private readonly Library library;

        public LibraryScanItem(Library library, MediaConverter converter, DbContextOptions<VolyContext> dbOptions, ILogger log, bool deleteWithSource, bool truncateSource) : base(ScanType.Library, library.Name)
        {
            this.library = library;
            this.converter = converter;
            this.dbOptions = dbOptions;
            this.log = log;
            this.deleteWithSource = deleteWithSource;
            this.truncateSource = truncateSource;
        }

        public override bool Scan()
        {
            if (!Directory.Exists(library.OriginPath))
            {
                log.LogWarning($"Scanning library {library.Name} failed: Directory does not exist.");
                return false;
            }

            using (var context = new VolyContext(dbOptions))
            {
                var currentLibrary = context.Media
                    .Where(x => x.LibraryName == library.Name)
                    .Select(x => new MediaItem() { MediaId = x.MediaId, SourcePath = x.SourcePath, SourceModified = x.SourceModified, SourceHash = x.SourceHash })
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

                if (deleteWithSource)
                {
                    // Delete db items not found in scan.
                    context.Media.RemoveRange(currentLibrary.Values);
                }

                context.SaveChanges();

                log.LogInformation($"Scanning library {library.Name} complete.");

                return true;
            }
        }

        private void ScheduleConversion(Library library, HashSet<IQuality> quality, string sourcePath, DateTimeOffset lastWrite, string seriesName, string sourceHash, string outFilename)
        {
            converter.AddItem(new ConversionItem(sourcePath, library.StoragePath, outFilename, quality, library.ForceFramerate, (result) =>
            {
                using (var innerContext = new VolyContext(dbOptions))
                {
                    // Save the new media item to db.
                    var newMedia = new MediaItem()
                    {
                        SourcePath = sourcePath,
                        SourceModified = lastWrite,
                        SourceHash = sourceHash,
                        Duration = result.FileDuration,
                        IndexName = Path.GetFileName(result.DashFilePath),
                        IndexHash = Hashing.HashFileMd5(result.DashFilePath),
                        LibraryName = library.Name,
                        Name = Path.GetFileNameWithoutExtension(sourcePath),
                        SeriesName = seriesName
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
                    var mediaFiles = result.MediaFiles;
                    AddMediaFilesToMedia(innerContext, newMedia.MediaId, library.StoragePath, mediaFiles);

                    HandleSourceTruncation(sourcePath);

                    innerContext.SaveChanges();
                }
            },
            (ex) =>
            {
                log.LogError($"Failed to convert {sourcePath} -- Ex: {ex.ToString()}");
            }));
        }

        private void ScheduleReconversion(Library library, HashSet<IQuality> quality, string sourcePath, DateTimeOffset lastWrite, int mediaId, string sourceHash, string outFilename)
        {
            converter.AddItem(new ConversionItem(sourcePath, library.StoragePath, outFilename, quality, library.ForceFramerate, (result) =>
            {
                using (var innerContext = new VolyContext(dbOptions))
                {
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
                        innerContext.Update(inDb);

                        var mediaFiles = result.MediaFiles;
                        AddMediaFilesToMedia(innerContext, inDb.MediaId, library.StoragePath, mediaFiles);

                        HandleSourceTruncation(sourcePath);
                    }
                    else
                    {
                        log.LogError($"Failed to find media item {mediaId}");
                    }

                    innerContext.SaveChanges();
                }
            },
            (ex) =>
            {
                log.LogError($"Failed to re-convert {sourcePath}, database not updated. -- Ex: {ex.ToString()}");
            }));
        }

        private void HandleSourceTruncation(string sourcePath)
        {
            if (truncateSource)
            {
                File.WriteAllBytes(sourcePath, new byte[0]);
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
