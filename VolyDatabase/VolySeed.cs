using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VolyDatabase;

namespace VolyDatabase
{
    public class VolySeed
    {
        private static string mostRecentVersion = "4";
        // A chain of upgrade queries which, when run, upgrade the database by one version.
        // Ensure mostRecentVersion is equal to the final version, or not on the upgrade map. Otherwise upgrades might be run on a new database.
        private static readonly Dictionary<string, Func<VolyContext, string>> Upgrades = new Dictionary<string, Func<VolyContext, string>>()
        {
            { "1", new Func<VolyContext, string>(x =>
            {
                x.Database.ExecuteSqlCommand("ALTER TABLE [MediaVariant] ADD [Built] BIT NOT NULL DEFAULT(1);");
                return "2";
            }) },
            { "2", new Func<VolyContext, string>(x =>
            {
                x.Database.ExecuteSqlCommand("ALTER TABLE [MediaItem] ADD [IndexName] VARCHAR;");
                x.Database.ExecuteSqlCommand("ALTER TABLE [MediaItem] ADD [ForeignDbType] VARCHAR");
                x.Database.ExecuteSqlCommand("ALTER TABLE [MediaItem] ADD [ForeignDbKey] VARCHAR;");
                return "3";
            }) },
            { "3", new Func<VolyContext, string>(x =>
            {
                x.Database.ExecuteSqlCommand("ALTER TABLE [MediaVariant] ADD [Preset] VARCHAR;");
                return mostRecentVersion;
            }) }
        };

        /// <summary>
        /// Initializes the database if it doesn't exist, and upgrades it to the latest version if it does.
        /// </summary>
        /// <param name="context"></param>
        public static void Initialize(VolyContext context)
        {
            context.Database.EnsureCreated();
            Upgrade(context);
        }

        /// <summary>
        /// I don't like the built in EF upgrade system.
        /// </summary>
        /// <param name="context"></param>
        private static void Upgrade(VolyContext context)
        {
            var version = context.Configuration.Where(x => x.Key == "version").FirstOrDefault();
            if (version == null)
            {
                context.Configuration.Add(new Configuration() { Key = "version", Value = mostRecentVersion });
                context.SaveChanges();
            }
            else if (Upgrades.ContainsKey(version.Value))
            {
                // Upgrade the database by one step.
                version.Value = Upgrades[version.Value].Invoke(context);
                context.SaveChanges();

                // Recurse.
                Upgrade(context);
            }
        }
    }
}
