using DEnc;
using DQP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volyar.Media.Conversion;
using Volyar.Models;

namespace Volyar.Media.Scanning
{
    public class MediaDatabase
    {
        public DbContextOptions<VolyContext> Database { get; set; }
    }

    public class MediaScanner : DistinctQueueProcessor<IScanItem>
    {
        private readonly MediaDatabase dbOptions;
        private readonly VSettings settings;
        private readonly MediaConverter converter;
        protected readonly ILogger<MediaScanner> log;

        public MediaScanner(MediaDatabase dbOptions, VSettings settings, MediaConverter converter, ILogger<MediaScanner> logger)
        {
            this.dbOptions = dbOptions;
            this.settings = settings;
            this.converter = converter;
            log = logger;

            Parallelization = 1; // Process linearly.
        }

        public void ScheduleLibraryScan(Library library, VolyContext context)
        {
            AddItem(new LibraryScanItem(library, converter, dbOptions.Database, log, settings.DeleteWithSource, settings.TruncateSource));
        }

        protected override void Process(IScanItem item)
        {
            item.Scan();
        }

        protected override void Error(IScanItem item, Exception ex)
        {
            log.LogError($"Exception while converting {item.ToString()} -- {ex.Message}");
        }
    }
}
