﻿using Microsoft.EntityFrameworkCore;
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
        private static readonly int migrationCountOffset = 4;
        // A chain of upgrade queries which, when run, upgrade the database by one version.
        // Ensure mostRecentVersion is equal to the final version, or not on the upgrade map. Otherwise upgrades might be run on a new database.
        private static readonly List<Action<VolyContext>> Upgrades = new List<Action<VolyContext>>()
        {
            new Action<VolyContext>(x =>
            {
                //x.Database.ExecuteSqlCommand("ALTER TABLE [MediaVariant] ADD [Built] BIT NOT NULL DEFAULT(1);");
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
