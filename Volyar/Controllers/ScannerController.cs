using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DEnc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volyar.Media;
using Volyar.Media.Scanning;
using Volyar.Models;

namespace Volyar.Controllers
{
    [Route("voly/internal/api/scan")]
    [ApiController]
    public class ScannerController : ControllerBase
    {
        private readonly DbContextOptions<VolyContext> dbOptions;
        private readonly VSettings settings;
        private readonly MediaScanner scanner;
        protected readonly ILogger<ScannerController> log;

        public ScannerController(DbContextOptions<VolyContext> dbOptions, VSettings settings, MediaScanner scanner, ILogger<ScannerController> logger)
        {
            this.dbOptions = dbOptions;
            this.settings = settings;
            this.scanner = scanner;
            log = logger;
        }

        [HttpPost("fullscan")]
        public void FullScan()
        {
            using (var outerContext = new VolyContext(dbOptions))
            {
                foreach (var library in settings.Libraries)
                {
                    scanner.ScheduleLibraryScan(library, outerContext);
                }
            }
        }

        [HttpPost("scanlib")]
        public IActionResult FullScan(string library)
        {
            var lib = settings.Libraries.Where(x => x.Name == library).FirstOrDefault();
            if (lib != null)
            {
                using (var outerContext = new VolyContext(dbOptions))
                {
                    scanner.ScheduleLibraryScan(lib, outerContext);
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
