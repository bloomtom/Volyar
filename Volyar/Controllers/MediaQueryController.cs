using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volyar.Models;
using VolyDatabase;
using VolyExports;

namespace Volyar.Controllers
{
    [Route("external/api/media")]
    public class MediaQueryController
    {
        private readonly VolyContext db;
        protected readonly ILogger<ScannerController> log;

        public MediaQueryController(VolyContext context, ILogger<ScannerController> logger)
        {
            db = context;
            log = logger;
        }

        [HttpGet("diff/{transactionId:long?}")]
        public IActionResult Diff(long transactionId)
        {
            log.LogInformation($"Diff requested against transaction ID {transactionId}");

            Differential result;

            using (var transaction = db.Database.BeginTransaction())
            {
                long maxLogKey = db.TransactionLog.DefaultIfEmpty().Max(x => x.TransactionId);

                // Select items deleted.
                IEnumerable<Deletion> removed = db.TransactionLog
                    .Where(x => x.TransactionId > transactionId && x.TransactionId <= maxLogKey && x.Type == TransactionType.Delete)
                    .Select(x => new Deletion(x.TableName, x.Key));

                // Select distinct media items added but not deleted.
                IEnumerable<IMediaItem> added = db.Media.FromSql("SELECT MediaItem.* FROM MediaItem" +
                    " INNER JOIN (SELECT DISTINCT [Key] FROM TransactionLog TL WHERE TL.Type = 0 AND TL.TableName = 'MediaItem' AND TL.TransactionId > {0} AND TL.TransactionId <= {1}) TKeys ON TKeys.[Key] = MediaItem.MediaId" +
                    " LEFT JOIN (SELECT [Key] FROM TransactionLog TL WHERE TL.Type = 2 AND TL.TableName = 'MediaItem' AND TL.TransactionId > {0} AND TL.TransactionId <= {1}) TExcept ON TExcept.[Key] = MediaItem.MediaId WHERE TExcept.[Key] IS NULL",
                    transactionId, maxLogKey);

                // Select distinct media items changed but not added or deleted.
                IEnumerable<IMediaItem> changed = db.Media.FromSql("SELECT MediaItem.* FROM MediaItem" +
                    " INNER JOIN (SELECT DISTINCT [Key] FROM TransactionLog TL WHERE TL.Type = 1 AND TL.TableName = 'MediaItem' AND TL.TransactionId > {0} AND TL.TransactionId <= {1}) TKeys ON TKeys.[Key] = MediaItem.MediaId" +
                    " LEFT JOIN (SELECT [Key] FROM TransactionLog TL WHERE TL.Type IN (0, 2) AND TL.TableName = 'MediaItem' AND TL.TransactionId > {0} AND TL.TransactionId <= {1}) TExcept ON TExcept.[Key] = MediaItem.MediaId WHERE TExcept.[Key] IS NULL",
                    transactionId, maxLogKey);

                result = new Differential() { CurrentKey = maxLogKey, Deletions = removed, Additions = added.Select(x => VolyExports.MediaItem.Convert(x)), Modifications = changed.Select(x => VolyExports.MediaItem.Convert(x)) };

                transaction.Rollback(); // This was a read only transaction.
            }

            return new JsonResult(result);
        }

        [HttpGet()]
        public IActionResult AllMedia()
        {
            log.LogInformation($"All media requested.");

            var result = new Dictionary<string, object>();
            using (var transaction = db.Database.BeginTransaction())
            {
                result = new Dictionary<string, object>
                {
                    { "diffBase", db.TransactionLog.DefaultIfEmpty().Max(x => x.TransactionId) },
                    { "media", db.Media }
                };
            }

            return new JsonResult(result);
        }
    }
}
