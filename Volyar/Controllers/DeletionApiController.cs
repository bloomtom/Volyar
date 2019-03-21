using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using VolyConverter.Complete;
using VolyConverter.Conversion;
using VolyConverter.Scanning;
using VolyDatabase;
using VolyExports;

namespace Volyar.Controllers
{
    [Route("external/api/delete")]
    public class DeletionApiController : Controller
    {
        private readonly VolyContext db;
        private readonly ILogger<ConversionApiController> log;

        public DeletionApiController(VolyContext context, ILogger<ConversionApiController> logger)
        {
            db = context;
            log = logger;
        }

        [HttpGet("pending")]
        public IActionResult GetPending()
        {
            IEnumerable<IMediaItem> added = db.Media.FromSql(@"
                    SELECT
                        PendingDeletions.MediaId,
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

        [HttpPost("revert")]
        public ObjectResult Revert([FromBody] int[] items)
        {
            db.PendingDeletions.RemoveRange(db.PendingDeletions.Where(x => items.Contains(x.MediaId)));
            db.SaveChanges();
            log.LogInformation($"Reverted deleting items {string.Join(',', items)}");
            return Ok("{}");
        }

        [HttpPost("confirm")]
        public ObjectResult Confirm([FromBody] int[] items)
        {
            log.LogInformation($"Deleted items {string.Join(',', items)}");
            return Ok("{}");
        }
    }
}
