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
                return BadRequest("{}");
            }

            foreach (var item in items)
            {
                db.PendingDeletions.Add(db.PendingDeletions.Where(x => x.MediaId == item.MediaId && x.Version == item.Version).SingleOrDefault());
            }

            db.SaveChanges();
            log.LogInformation($"Scheduled deletion of items {string.Join(';', items.Select(x => $"{x.MediaId},{x.Version}"))}");
            return Ok("{}");
        }

        [HttpPost("revert")]
        public ObjectResult Revert([FromBody] DeletionInfo[] items)
        {
            if (items == null || items.Length == 0)
            {
                return BadRequest("{}");
            }

            foreach (var item in items)
            {
                db.PendingDeletions.Remove(db.PendingDeletions.Where(x => x.MediaId == item.MediaId && x.Version == item.Version).SingleOrDefault());
            }

            db.SaveChanges();
            log.LogInformation($"Reverted deleting items {string.Join(';', items.Select(x => $"{x.MediaId},{x.Version}"))}");
            return Ok("{}");


        }

        [HttpPost("confirm")]
        public ObjectResult Confirm([FromBody] DeletionInfo[] items)
        {
            if (items == null || items.Length == 0)
            {
                return BadRequest("{}");
            }

            Dictionary<string, FileManagement> managerCache = new Dictionary<string, FileManagement>();
            foreach (var item in items)
            {
                try
                {
                    var mediaItem = db.Media.Where(x => x.MediaId == item.MediaId).SingleOrDefault();

                    // Retrieve a file manager for this library.
                    if (!managerCache.TryGetValue(mediaItem.LibraryName, out FileManagement fileManager))
                    {
                        fileManager = new FileManagement(settings.Libraries.Where(x => x.Name == mediaItem.LibraryName).SingleOrDefault().StorageBackend.RetrieveBackend(), log);
                        managerCache[mediaItem.LibraryName] = fileManager;
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
                }
                catch (Exception ex)
                {
                    log.LogError($"Failed to delete item {ex.ToString()}");
                    return StatusCode(StatusCodes.Status500InternalServerError, $"{{Exception:\"{ex.Message}\"}}");
                }
            }

            return Ok("{}");
        }
    }
}
