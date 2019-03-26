using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DEnc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volyar.Models;
using VolyConverter.Scanning;
using VolyDatabase;

namespace Volyar.Controllers
{
    [Route("internal/api/scan")]
    [ApiController]
    public class ScannerController : ControllerBase
    {
        private readonly DbContextOptions<VolyContext> dbOptions;
        private readonly VSettings settings;
        private readonly LibraryScanningQueue scanner;
        protected readonly ILogger<ScannerController> log;

        public ScannerController(DbContextOptions<VolyContext> dbOptions, VSettings settings, LibraryScanningQueue scanner, ILogger<ScannerController> logger)
        {
            this.dbOptions = dbOptions;
            this.settings = settings;
            this.scanner = scanner;
            log = logger;
        }

        [HttpPost("fullscan")]
        public void FullScan()
        {
            log.LogInformation($"Full scan requested.");

            using (var outerContext = new VolyContext(dbOptions))
            {
                foreach (var library in settings.Libraries)
                {
                    scanner.ScheduleLibraryScan(library, library.StorageBackend.RetrieveBackend(), outerContext);
                }
            }
        }

        [HttpPost("scanlib/{libraryName}")]
        public IActionResult FullScan(string libraryName)
        {
            log.LogInformation($"Scan of library {libraryName} requested.");

            var library = settings.Libraries.Where(x => x.Name == libraryName).FirstOrDefault();
            if (library != null)
            {
                using (var outerContext = new VolyContext(dbOptions))
                {
                    scanner.ScheduleLibraryScan(library, library.StorageBackend.RetrieveBackend(), outerContext);
                }
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
