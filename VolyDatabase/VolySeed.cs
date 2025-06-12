using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VolyDatabase;

namespace VolyDatabase
{
    public class VolySeed
    {
        private static readonly int migrationCountOffset = 4; // Increment this for each old entry that's removed from Upgrades.

        // A chain of upgrade queries which, when run, upgrade the database by one version each.
        private static readonly List<Action<VolyContext>> Upgrades = new List<Action<VolyContext>>()
        {
            new(x =>
            {
                //x.Database.ExecuteSqlCommand("ALTER TABLE [MediaVariant] ADD [Built] BIT NOT NULL DEFAULT(1);");
            }),
            new(x =>
            {
                x.Database.ExecuteSqlRaw("ALTER TABLE [MediaItem] ADD [SeasonNumber] INTEGER;");
                x.Database.ExecuteSqlRaw("ALTER TABLE [MediaItem] ADD [EpisodeNumber] INTEGER;");
                x.Database.ExecuteSqlRaw("ALTER TABLE [MediaItem] ADD [ImdbId] VARCHAR;");
                x.Database.ExecuteSqlRaw("ALTER TABLE [MediaItem] ADD [TvdbId] VARCHAR;");
                x.Database.ExecuteSqlRaw("ALTER TABLE [MediaItem] ADD [TvmazeId] VARCHAR;");
            }),
            new(x =>
            {
                x.Database.ExecuteSqlRaw("ALTER TABLE [MediaItem] ADD [TmdbId] VARCHAR;");
                x.Database.ExecuteSqlRaw("ALTER TABLE [MediaItem] ADD [AbsoluteEpisodeNumber] INTEGER;");
            }),
            new(x =>
            {
                x.Database.ExecuteSqlRaw("CREATE TABLE [PendingDeletions] ([MediaId] INTEGER, [Version] INTEGER NOT NULL, [Requestor] INTEGER, PRIMARY KEY([MediaId], [Version]));");
                x.Database.ExecuteSqlRaw("ALTER TABLE [MediaVariant] ADD [Version] INTEGER NOT NULL DEFAULT (0);");
                x.Database.ExecuteSqlRaw("ALTER TABLE [MediaItem] ADD [Version] INTEGER NOT NULL DEFAULT (0);");
            }),
            new(x =>
            {
                x.Database.ExecuteSqlRaw("UPDATE TransactionLog SET TableName = 1, Date = 1553580240;");
            }),
            new(x =>
            {
                if (x.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
                {
                    x.Database.ExecuteSqlRaw(@"
                    UPDATE MediaItem 
                    SET
                        CreateDate = (unixepoch(CreateDate) * 10000000) + substr(CreateDate, 21, 7) + 621355968000000000,
                        SourceModified = (unixepoch(SourceModified) * 10000000) + substr(SourceModified, 21, 7) + 621355968000000000;
                    ");
                }
            })
        };

        private static int UpgradesOffsetCount { get { return Upgrades.Count + migrationCountOffset; } }

        public static int UpgradesOffsetIndex(int index) { return index - migrationCountOffset; }

        /// <summary>
        /// Initializes the database if it doesn't exist, and upgrades it to the latest version if it does.
        /// </summary>
        public static void Initialize(VolyContext context, ILogger log)
        {
            context.Database.EnsureCreated();
            Upgrade(context, log);
        }

        /// <summary>
        /// I don't like the built in EF upgrade system.
        /// </summary>
        private static void Upgrade(VolyContext context, ILogger log)
        {
            var dbVersion = context.Configuration.Where(x => x.Key == "version").FirstOrDefault();
            if (dbVersion == null || string.IsNullOrWhiteSpace(dbVersion.Value) || !int.TryParse(dbVersion.Value, out int version))
            {
                context.Configuration.Add(new Configuration() { Key = "version", Value = UpgradesOffsetCount.ToString() });
                context.SaveChanges();
                if (log != null) { log.LogInformation($"Database created at version {UpgradesOffsetCount}."); }
            }
            else
            {
                while (version < UpgradesOffsetCount)
                {
                    Upgrades[UpgradesOffsetIndex(version)].Invoke(context);
                    version++;
                }
                if (dbVersion.Value != UpgradesOffsetCount.ToString())
                {
                    dbVersion.Value = UpgradesOffsetCount.ToString();
                    context.SaveChanges();
                    if (log != null) { log.LogInformation($"Database upgraded to version {dbVersion.Value}."); }
                }
            }
        }
    }
}
