using DEnc;
using DQP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VolyConverter.Conversion;
using MStorage;
using Microsoft.EntityFrameworkCore;
using VolyDatabase;

namespace VolyConverter.Scanning
{
    public class LibraryScanningQueue : DistinctQueueProcessor<IScanItem>
    {
        private readonly MediaDatabase dbOptions;
        private readonly IDistinctQueueProcessor<IConversionItem> converter;
        protected readonly ILogger<LibraryScanningQueue> log;

        public LibraryScanningQueue(MediaDatabase dbOptions, IDistinctQueueProcessor<IConversionItem> converter, ILogger<LibraryScanningQueue> logger)
        {
            this.dbOptions = dbOptions;
            this.converter = converter;
            log = logger;

            Parallelization = 1; // Process linearly.
        }

        public void ScheduleLibraryScan(Library library, IStorage storageBackend, VolyContext context)
        {
            AddItem(new LibraryScanner(library, storageBackend, converter, dbOptions.Database, log));
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
