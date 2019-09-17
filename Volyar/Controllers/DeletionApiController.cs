using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using Volyar.Models;
using VolyConverter.Complete;
using VolyConverter.Conversion;
using VolyConverter.Scanning;
using VolyDatabase;
using VolyExports;
using VolyFiles;

namespace Volyar.Controllers
{
    internal class FailedItem : DeletionInfo
    {
        public string Reason { get; set; }

        public static FailedItem Generate(DeletionInfo info, string reason = null)
        {
            return new FailedItem()
            {
                MediaId = info.MediaId,
                Version = info.Version,
                Reason = reason
            };
        }
    }

    internal class DeletionApiResult
    {
        public string Message { get; set; }
        public List<DeletionInfo> SucceededItems { get; set; }
        public List<FailedItem> FailedItems { get; set; }
    }

    [Route("external/api/delete")]
    public class DeletionApiController : Controller
    {
        private readonly VolyContext db;
        private readonly IDapperConnection dapper;
        private readonly VSettings settings;
        private readonly ILogger<ConversionApiController> log;

        public DeletionApiController(VolyContext context, IDapperConnection dapper, VSettings settings, ILogger<ConversionApiController> logger)
        {
            db = context;
            this.dapper = dapper;
            this.settings = settings;
            log = logger;
        }

        [HttpGet("pending")]
        public IActionResult GetPending()
        {
            using (var connection = dapper.NewConnection())
            {
                connection.Open();

                SqlMapper.AddTypeHandler(typeof(TimeSpan), new TimeSpanHandler());
                SqlMapper.AddTypeHandler(typeof(DateTimeOffset), new DateTimeOffsetHandler());
                var added = connection.Query<VolyExports.MediaItem>(@"
                    SELECT
                        PendingDeletions.MediaId,
                        PendingDeletions.Version,
                        IndexName,
                        IndexHash,
                        LibraryName,
                        SeriesName,
                        ImdbId,
                        TmdbId,
                        TvdbId,
                        TvmazeId,
                        Name,
                        SeasonNumber,
                        EpisodeNumber,
                        AbsoluteEpisodeNumber,
                        CreateDate,
                        SourcePath,
                        SourceModified,
                        SourceHash,
                        Duration,
                        Metadata
                    FROM
                        PendingDeletions
                    LEFT JOIN
                        MediaItem ON PendingDeletions.MediaId = MediaItem.MediaId
                ");

                return new ObjectResult(Newtonsoft.Json.JsonConvert.SerializeObject(added));
            }
        }

        [HttpPost("schedule")]
        public ObjectResult Schedule([FromBody] DeletionInfo[] items)
        {
            if (items == null || items.Length == 0)
            {
                return NullResponse();
            }

            var scheduled = new List<DeletionInfo>();
            var failed = new List<FailedItem>();
            foreach (var item in items)
            {
                var delete = db.Media.Where(x => x.MediaId == item.MediaId).SingleOrDefault();
                if (delete != null)
                {
                    db.PendingDeletions.Add(new PendingDeletion() { MediaId = delete.MediaId, Version = item.Version, Requestor = DeleteRequester.User });
                    scheduled.Add(item);
                }
                else
                {
                    failed.Add(FailedItem.Generate(item, "Not found"));
                }
            }

            db.SaveChanges();
            log.LogInformation($"Scheduled deletion of items {string.Join(';', scheduled.Select(x => $"{x.MediaId},{x.Version}"))}");
            return GenerateOverallStatus(scheduled, failed, "scheduled");
        }

        [HttpPost("revert")]
        public ObjectResult Revert([FromBody] DeletionInfo[] items)
        {
            if (items == null || items.Length == 0)
            {
                return NullResponse();
            }

            var reverted = new List<DeletionInfo>();
            var failed = new List<FailedItem>();
            foreach (var item in items)
            {
                var revert = db.PendingDeletions.Where(x => x.MediaId == item.MediaId && x.Version == item.Version).SingleOrDefault();
                if (revert != null)
                {
                    db.PendingDeletions.Remove(revert);
                    reverted.Add(item);
                }
                else
                {
                    failed.Add(FailedItem.Generate(item, "Not found"));
                }
            }

            db.SaveChanges();
            log.LogInformation($"Reverted deleting items {string.Join(';', reverted.Select(x => $"{x.MediaId},{x.Version}"))}");
            return GenerateOverallStatus(reverted, failed, "reverted");
        }

        private ObjectResult NullResponse()
        {
            return NullResponse();
        }

        [HttpPost("confirm")]
        public ObjectResult Confirm([FromBody] DeletionInfo[] items)
        {
            if (items == null || items.Length == 0)
            {
                return BadRequest(Newtonsoft.Json.JsonConvert.SerializeObject(new DeletionApiResult()
                {
                    Message = "Given collection of items was null or empty.",
                    FailedItems = null,
                    SucceededItems = null
                }));
            }

            var deleted = new List<DeletionInfo>();
            var failed = new List<FailedItem>();
            Dictionary<string, FileManagement> managerCache = new Dictionary<string, FileManagement>();
            foreach (var item in items)
            {
                try
                {
                    var mediaItem = db.Media.Where(x => x.MediaId == item.MediaId).SingleOrDefault();

                    FileManagement fileManager = null;
                    try
                    {
                        // Retrieve a file manager for this library.
                        if (!managerCache.TryGetValue(mediaItem.LibraryName, out fileManager))
                        {
                            var library = settings.Libraries.Where(x => x.Name == mediaItem.LibraryName).SingleOrDefault();
                            if (library != null && library.StorageBackend != null)
                            {
                                fileManager = new FileManagement(library.StorageBackend.RetrieveBackend(), log);
                                managerCache[mediaItem.LibraryName] = fileManager;
                            }
                            else
                            {
                                failed.Add(FailedItem.Generate(item, $"Library '{mediaItem.LibraryName}' does not exist in vsettings."));
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failed.Add(FailedItem.Generate(item, $"Failed to generate storage backend: {ex.Message}"));
                        continue;
                    }

                    // Discover the files to delete.
                    IEnumerable<MediaFile> deleteEntries;
                    if (item.Version == -1)
                    {
                        deleteEntries = db.MediaFiles.Where(x => x.MediaId == item.MediaId);
                    }
                    else
                    {
                        deleteEntries = db.MediaFiles.Where(x => x.MediaId == item.MediaId && x.Version == item.Version);
                    }

                    // Delete files.
                    foreach (var file in deleteEntries)
                    {
                        fileManager.DeleteFromBackend(file.Filename);
                        log.LogInformation($"Deleted {file.Filename}");
                    }

                    // Update database
                    db.PendingDeletions.RemoveRange(db.PendingDeletions.Where(x => x.MediaId == item.MediaId && x.Version == x.Version));
                    if (item.Version == -1 || mediaItem.Version == item.Version)
                    {
                        db.Media.Remove(db.Media.Where(x => x.MediaId == item.MediaId).SingleOrDefault());
                    }
                    else
                    {
                        db.MediaFiles.RemoveRange(db.MediaFiles.Where(x => x.MediaId == item.MediaId && x.Version == item.Version));
                    }

                    db.SaveChanges();

                    deleted.Add(item);
                }
                catch (Exception ex)
                {
                    log.LogError($"Failed to delete item {item.MediaId} version {item.Version}: {ex.ToString()}");
                    failed.Add(FailedItem.Generate(item, $"Unknown error. Exception: {ex.Message}"));
                }
            }

            return GenerateOverallStatus(deleted, failed, "deleted");
        }

        private ObjectResult GenerateOverallStatus(List<DeletionInfo> deleted, List<FailedItem> failed, string actionDescription = "processed")
        {
            if (failed.Count == 0)
            {
                return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(new DeletionApiResult()
                {
                    Message = $"All items {actionDescription}.",
                    FailedItems = null,
                    SucceededItems = deleted
                }));
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, Newtonsoft.Json.JsonConvert.SerializeObject(new DeletionApiResult()
                {
                    Message = $"Some items could not be {actionDescription}.",
                    FailedItems = failed,
                    SucceededItems = deleted
                }));
            }
        }
    }
}
