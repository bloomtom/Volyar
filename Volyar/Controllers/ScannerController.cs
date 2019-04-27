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
using VolyExternalApiAccess.Darr;

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

        [HttpPost("full")]
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

        [HttpPost("library/{libraryName}")]
        public IActionResult LibraryScan(string libraryName)
        {
            return LibraryScan(libraryName, null);
        }

        [HttpPost("library/{libraryName}/filtered")]
        public IActionResult LibraryScan(string libraryName, [FromBody]List<string> scanFilter)
        {
            log.LogInformation($"Scan of library {libraryName} requested.");

            var library = settings.Libraries.Where(x => x.Name == libraryName).FirstOrDefault();
            if (library != null)
            {
                using (var outerContext = new VolyContext(dbOptions))
                {
                    scanner.ScheduleLibraryScan(library, library.StorageBackend.RetrieveBackend(), outerContext, scanFilter);
                }
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("sonarr/{libraryName}")]
        public IActionResult SonarrScan(string libraryName, [FromBody]WebHookBody body)
        {
            if (body == null) { return StatusCode(400); }
            if (body.EventType == "Test") { return Ok(); }
            if (body.EventType != "Download") { return StatusCode(400); }

            string path = null;
            if (body.EpisodeFile != null)
            {
                path = Path.Combine(body.Series.Path, body.EpisodeFile.RelativePath);
            }
            else if (body.MovieFile != null)
            {
                path = Path.Combine(body.Movie.FolderPath, body.MovieFile.RelativePath);
            }
            else
            {
                return StatusCode(400);
            }
            if (path == null)
            {
                log.LogInformation($"Sonarr scan requested, but the parsed path was null.");
                return StatusCode(400);
            }

            log.LogInformation($"Sonarr scan requested for file: {path}");

            return LibraryScan(libraryName, new List<string>() { path });
        }
    }
}
