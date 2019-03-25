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
using Dapper;

namespace Volyar.Controllers
{
    internal class QueryParameter
    {
        public long TransactionId { get; set; }
        public long MaxLogKey { get; set; }
        public string Library { get; set; }
    }

    [Route("external/api/media")]
    public class MediaQueryController
    {
        private const string selectMaxKey = "SELECT MAX([Key]) FROM TransactionLog";
        private const string selectDeletedQuery = @"
                    SELECT DISTINCT [Key]
                    FROM TransactionLog TL 
                    WHERE
                        TL.Type = 2 AND
                        TL.TableName = 'MediaItem' AND
                        TL.TransactionId > @TransactionId AND
                        TL.TransactionId <= @MaxLogKey";
        private const string selectAddedQuery = @"
                    SELECT 
                        MediaItem.*
                    FROM
                        MediaItem
                    INNER JOIN
                    (
                        SELECT DISTINCT [Key]
                        FROM TransactionLog TL 
                        WHERE
                            TL.Type = 0 AND
                            TL.TableName = 'MediaItem' AND
                            TL.TransactionId > @TransactionId AND
                            TL.TransactionId <= @MaxLogKey
                    ) TKeys
                    ON TKeys.[Key] = MediaItem.MediaId
                    LEFT JOIN
                    (
                        SELECT [Key]
                        FROM TransactionLog TL
                        WHERE
                        TL.Type = 2 AND
                        TL.TableName = 'MediaItem' AND
                        TL.TransactionId > @TransactionId AND
                        TL.TransactionId <= @MaxLogKey
                    ) TExcept
                    ON TExcept.[Key] = MediaItem.MediaId
                    WHERE TExcept.[Key] IS NULL";
        private const string selectModifiedQuery = @"
                    SELECT MediaItem.*
                    FROM MediaItem
                    INNER JOIN
                    (
                        SELECT DISTINCT [Key]
                        FROM TransactionLog TL
                        WHERE
                            TL.Type = 1 AND
                            TL.TableName = 'MediaItem' AND
                            TL.TransactionId > @TransactionId AND
                            TL.TransactionId <= @MaxLogKey
                    ) TKeys
                    ON TKeys.[Key] = MediaItem.MediaId
                    LEFT JOIN
                    (
                        SELECT [Key]
                        FROM TransactionLog TL
                        WHERE
                            TL.Type IN (0, 2) AND
                            TL.TableName = 'MediaItem' AND
                            TL.TransactionId > @TransactionId AND
                            TL.TransactionId <= @MaxLogKey
                    ) TExcept ON TExcept.[Key] = MediaItem.MediaId
                    WHERE TExcept.[Key] IS NULL";

        private readonly VolyContext db;
        private readonly IDapperConnection dapper;
        protected readonly ILogger<ScannerController> log;

        public MediaQueryController(VolyContext context, IDapperConnection dapper, ILogger<ScannerController> logger)
        {
            db = context;
            this.dapper = dapper;
            log = logger;
        }

        [HttpGet("diff/{library}/{transactionId:long?}")]
        public IActionResult LibraryDiff(string library, long transactionId)
        {
            library = System.Net.WebUtility.UrlDecode(library);
            log.LogInformation($"Diff requested against library {library} transaction ID {transactionId}");

            Differential result = SelectDifferential(transactionId, library);

            return new JsonResult(result);
        }

        [HttpGet("diff/{transactionId:long?}")]
        public IActionResult Diff(long transactionId)
        {
            log.LogInformation($"Diff requested against transaction ID {transactionId}");

            Differential result = SelectDifferential(transactionId);

            return new JsonResult(result);
        }

        private Differential SelectDifferential(long transactionId, string library = null)
        {
            string selectMaxKey = MediaQueryController.selectMaxKey;
            string selectDeletedQuery = MediaQueryController.selectDeletedQuery;
            string selectAddedQuery = MediaQueryController.selectAddedQuery;
            string selectModifiedQuery = MediaQueryController.selectModifiedQuery;
            QueryParameter paramters = new QueryParameter() { TransactionId = transactionId, Library = library };
            if (library != null)
            {
                selectAddedQuery += " AND MediaItem.LibraryName = @Library";
                selectModifiedQuery += " AND MediaItem.LibraryName = @Library";
            }

            Differential result;
            var connection = dapper.NewConnection();
            connection.Open();
            SqlMapper.AddTypeHandler(typeof(TimeSpan), new TimeSpanHandler());
            SqlMapper.AddTypeHandler(typeof(DateTimeOffset), new DateTimeOffsetHandler());
            using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead))
            {
                paramters.MaxLogKey = connection.QuerySingle<long>(selectMaxKey);

                // Select items deleted.
                var removed = connection.Query<Deletion>(selectDeletedQuery, paramters);

                // Select distinct media items added but not deleted.
                var added = connection.Query<VolyExports.MediaItem>(selectAddedQuery, paramters);

                // Select distinct media items changed but not added or deleted.
                var changed = connection.Query<VolyExports.MediaItem>(selectModifiedQuery, paramters);

                result = new Differential()
                {
                    CurrentKey = paramters.MaxLogKey,
                    Deletions = removed,
                    Additions = added,
                    Modifications = changed
                };

                transaction.Rollback(); // This was a read only transaction.
            }

            return result;
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
