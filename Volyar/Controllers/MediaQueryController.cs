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
        private const string selectMaxKey = "SELECT MAX([TransactionId]) FROM TransactionLog";
        private const string selectDeletedQuery = @"
                    SELECT DISTINCT [Key], [TableName] 'Table'
                    FROM TransactionLog TL 
                    WHERE
                        TL.Type = 2 AND
                        TL.TableName = 1 AND
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
                            TL.TableName = 1 AND
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
                        TL.TableName = 1 AND
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
                            TL.TableName = 1 AND
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
                            TL.TableName = 1 AND
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
            QueryParameter parameters = new QueryParameter() { TransactionId = transactionId, Library = library };
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
                parameters.MaxLogKey = connection.QuerySingle<long>(selectMaxKey);

                // Select items deleted.
                var removed = connection.Query<Deletion>(selectDeletedQuery, parameters);

                // Select distinct media items added but not deleted.
                var added = connection.Query<VolyExports.MediaItem>(selectAddedQuery, parameters);

                // Select distinct media items changed but not added or deleted.
                var changed = connection.Query<VolyExports.MediaItem>(selectModifiedQuery, parameters);

                result = new Differential()
                {
                    CurrentKey = parameters.MaxLogKey,
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

        [HttpGet("manager")]
        public IActionResult Manager(string query, int limit, int page, string orderBy, int ascending, int byColumn)
        {
            if (string.IsNullOrWhiteSpace(orderBy) || orderBy == "null") { orderBy = "MediaId"; }
            List<VolyDatabase.MediaItem> result;
            int totalCount = 0;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = new MediaManagerQuery(query);
                totalCount = Filter(q).Count();
                result = Filter(q)
                    .OrderBy(orderBy, ascending == 0)
                    .Skip(limit * (page - 1)).Take(limit)
                    .AsNoTracking().ToList();
            }
            else
            {
                totalCount = db.Media.Count();
                result = db.Media
                    .OrderBy(orderBy, ascending == 0)
                    .Skip(limit * (page - 1)).Take(limit)
                    .AsNoTracking().ToList();
            }
            result = result.TakeLast(limit).ToList();

            return new JsonResult(new Dictionary<string, object>
                {
                    { "data", result },
                    { "count", totalCount }
                });
        }

        [HttpGet("item/{mediaId:int?}")]
        public IActionResult GetItem(int mediaId)
        {
            if (mediaId < 0) { return new StatusCodeResult(400); }
            var existing = db.Media.Where(x => x.MediaId == mediaId).FirstOrDefault();
            if (existing == null) { return new StatusCodeResult(404); }
            return new JsonResult(existing);
        }

        [HttpPut("item/")]
        public StatusCodeResult PutItem([FromBody] VolyDatabase.MediaItem updatedItem)
        {
            if (updatedItem == null) { return new StatusCodeResult(400); }

            var existing = db.Media.Where(x => x.MediaId == updatedItem.MediaId).FirstOrDefault();
            if (existing == null) { return new StatusCodeResult(404); }

            if (existing.SeriesName != updatedItem.SeriesName ||
                existing.Name != updatedItem.Name ||
                existing.SeasonNumber != updatedItem.SeasonNumber ||
                existing.EpisodeNumber != updatedItem.EpisodeNumber ||
                existing.AbsoluteEpisodeNumber != updatedItem.AbsoluteEpisodeNumber ||
                existing.ImdbId != updatedItem.ImdbId ||
                existing.TmdbId != updatedItem.TmdbId ||
                existing.TvdbId != existing.TvdbId ||
                existing.TvmazeId != existing.TvmazeId ||
                existing.Version != updatedItem.Version)
            {
                if (existing.Version != updatedItem.Version &&
                    !db.MediaFiles.Where(x => x.MediaId == updatedItem.MediaId && x.Version == updatedItem.Version).Any())
                {
                    // Requested assignment to a version which does not exist.
                    return new StatusCodeResult(400);
                }

                existing.SeriesName = updatedItem.SeriesName;
                existing.Name = updatedItem.Name;
                existing.SeasonNumber = updatedItem.SeasonNumber;
                existing.EpisodeNumber = updatedItem.EpisodeNumber;
                existing.AbsoluteEpisodeNumber = updatedItem.AbsoluteEpisodeNumber;
                existing.ImdbId = updatedItem.ImdbId;
                existing.TmdbId = updatedItem.TmdbId;
                existing.TvdbId = existing.TvdbId;
                existing.TvmazeId = existing.TvmazeId;
                existing.Version = updatedItem.Version;
                db.Media.Update(existing);
                db.SaveChanges();
            }

            return new StatusCodeResult(204);
        }

        private IQueryable<VolyDatabase.MediaItem> Filter(MediaManagerQuery q)
        {
            return db.Media.Where(x =>
                                q.ID != null ? x.MediaId == q.ID : true &&
                                q.LibraryName != null ? x.LibraryName == q.LibraryName : true &&
                                q.SeriesName != null ? EF.Functions.Like(x.Name, $"{q.SeriesName}%") : true &&
                                q.EpisodeName != null ? EF.Functions.Like(x.Name, $"{q.EpisodeName}%") : true &&
                                q.GeneralQuery != null ? EF.Functions.Like(x.SeriesName, $"%{q.GeneralQuery}%") || EF.Functions.Like(x.Name, $"%{q.GeneralQuery}%") : true);
        }
    }
}
