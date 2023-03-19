using DEnc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using VolyConverter.Complete;
using VolyConverter.Conversion;
using VolyConverter.Scanning;
using VolyExports;

namespace Volyar.Controllers
{
    [Route("external/api/conversion")]
    public class ConversionApiController : Controller
    {
        private readonly LibraryScanningQueue libraryScanner;
        private readonly MediaConversionQueue converter;
        private readonly ICompleteItems<IExportableConversionItem> completeItems;
        private readonly ILogger<ConversionApiController> log;

        public ConversionApiController(LibraryScanningQueue libraryScanner, MediaConversionQueue converter, ICompleteItems<IExportableConversionItem> completeItems, ILogger<ConversionApiController> logger)
        {
            this.libraryScanner = libraryScanner;
            this.converter = converter;
            this.completeItems = completeItems;
            log = logger;
        }

        [HttpGet("complete")]
        public IActionResult GetCompletedItems()
        {
            return new ObjectResult(Newtonsoft.Json.JsonConvert.SerializeObject(completeItems.ToArray()));
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            string json = GetStatus();
            return new ObjectResult(json);
        }

        [HttpGet("teststatus")]
        public IActionResult TestStatus(long transactionId)
        {
            var quality = Quality.GenerateDefaultQualities(DEnc.DefaultQuality.medium, DEnc.H264Preset.fast).ToHashSet();

            var items = new Dictionary<string, object>
            {
                {
                    "queued",
                    new List<ExportableConversionItem>()
                    {
                        new ConversionItem("SomeLibrary", "The TEST", "Test #1", "/home/test/vid/vid1.mkv", "/home/output/", "testitem", quality),
                        new ConversionItem("SomeLibrary", null, null, "/home/test/vid/vid2.mkv", "/home/output/", "testitem2", quality)
                    }
                },
                {
                    "processing",
                    new List<ExportableConversionItem>()
                    {
                        new ConversionItem("SomeLibrary", "The TEST", "Test #3","/home/test/vid/vid3.mkv", "/home/output/", "processItem", quality)
                        {
                            Progress = new List<DescribedProgress>()
                            {
                                new DescribedProgress("Encoding", 0.25)
                            }
                        },
                        new ConversionItem("SomeLibrary", null, null, "/home/test/vid/vid4.mkv", "/home/output/", "processItem2", quality)
                        {
                            Progress = new List<DescribedProgress>()
                            {
                                new DescribedProgress("Encoding", 1),
                                new DescribedProgress("Upload", 0.47)
                            }
                        },
                        new ConversionItem("SomeLibrary", null, null, "/home/test/vid/vid5.mkv", "/home/output/", "processItem3", quality)
                        {
                            Progress = new List<DescribedProgress>()
                            {
                                new DescribedProgress("Encoding", 1),
                                new DescribedProgress(@"Upload C:\path\to\the\temp\directory\long\path\videos\temp\vid5 video something.mp4", 0.36),
                                new DescribedProgress(@"Upload C:\path\to\the\temp\directory\long\path\videos\temp\vid5 audio something.eng.mp4", 0.11),
                                new DescribedProgress(@"Upload C:\path\to\the\temp\directory\long\path\videos\temp\vid5 audio something.jpn.mp4", 0.68),
                                new DescribedProgress(@"Upload C:\path\to\the\temp\directory\long\path\videos\temp\vid5 something.eng.vtt", 1),
                                new DescribedProgress(@"Upload C:\path\to\the\temp\directory\long\path\videos\temp\vid5 something.jpn.vtt", 1)
                            }
                        }
                    }
                }
            };
            return new ObjectResult(Newtonsoft.Json.JsonConvert.SerializeObject(items));
        }

        [HttpGet("testcomplete")]
        public IActionResult TestCompletedItems()
        {
            var quality = Quality.GenerateDefaultQualities(DEnc.DefaultQuality.medium, DEnc.H264Preset.fast).ToHashSet();

            var items = new CompleteItems<ExportableConversionItem>()
            {
                new ConversionItem("SomeLibrary", "The COMPLETE", "Test #6", "/home/test/vid/vid6.mkv", "/home/output/", "completeitem1", quality),
                new ConversionItem("SomeLibrary", "The FAILED", "Test #7", "/home/test/vid/vid7.mkv", "/home/output/", "completeitem2", quality)
                {
                    ErrorReason = "Task failed successfully. Reason: Not enough",
                    ErrorDetail = "This is a very long error message which contains all kinds of stack trace information and logs or whatever.\n" +
                        "NullProblemException: There is no issue.\n" +
                        "    at Volyar.Testing.TestCompletion()\n" +
                        "    at Volar.Testing.FakeStackTraceGenerator()\n" +
                        "    at System.Runtime.LoremIpsum()\n" +
                        "Running ffmpeg with arguments: -i something -a -b -c -d A bunch of arguments for ffmpeg which takes up a ridiculous amount of space.\n" +
                        "frame=  365 fps=0.0 q=-1.0 size=   22525kB time=00:00:15.27 bitrate=12077.9kbits/s speed=30.6x\n" +
                        "frame=  882 fps=882 q=-1.0 size=   42585kB time=00:00:36.80 bitrate=9479.1kbits/s speed=36.8x\n" +
                        "frame= 1434 fps=956 q=-1.0 size=   58536kB time=00:00:59.86 bitrate=8010.8kbits/s speed=39.9x\n" +
                        "frame= 1986 fps=993 q=-1.0 size=   74108kB time=00:01:22.82 bitrate=7329.9kbits/s speed=41.4x\n" +
                        "frame= 2445 fps=978 q=-1.0 size=   86742kB time=00:01:42.07 bitrate=6961.5kbits/s speed=40.8x\n" +
                        "frame= 2823 fps=941 q=-1.0 size=   96733kB time=00:01:57.79 bitrate=6727.3kbits/s speed=39.2x\n" +
                        "frame= 3261 fps=931 q=-1.0 size=  111247kB time=00:02:16.02 bitrate=6699.9kbits/s speed=38.8x\n" +
                        "frame= 3716 fps=928 q=-1.0 size=  129424kB time=00:02:35.01 bitrate=6839.5kbits/s speed=38.7x\n" +
                        "frame= 4215 fps=936 q=-1.0 size=  154043kB time=00:02:55.89 bitrate=7174.5kbits/s speed=39.1x\n" +
                        "frame= 4609 fps=921 q=-1.0 size=  169230kB time=00:03:12.19 bitrate=7213.3kbits/s speed=38.4x\n" +
                        "frame= 4986 fps=906 q=-1.0 size=  178219kB time=00:03:28.02 bitrate=7018.2kbits/s speed=37.8x\n" +
                        "frame= 5491 fps=915 q=-1.0 size=  194228kB time=00:03:49.11 bitrate=6944.7kbits/s speed=38.2x\n" +
                        "frame= 6044 fps=929 q=-1.0 size=  224348kB time=00:04:12.05 bitrate=7291.6kbits/s speed=38.8x\n" +
                        "frame= 6613 fps=944 q=-1.0 size=  259268kB time=00:04:35.89 bitrate=7698.2kbits/s speed=39.4x\n" +
                        "frame= 7244 fps=965 q=-1.0 size=  296714kB time=00:05:02.16 bitrate=8044.3kbits/s speed=40.3x\n" +
                        "frame= 7860 fps=982 q=-1.0 size=  328630kB time=00:05:27.84 bitrate=8211.7kbits/s speed=  41x\n" +
                        "frame= 8373 fps=985 q=-1.0 size=  350555kB time=00:05:49.25 bitrate=8222.6kbits/s speed=41.1x\n" +
                        "frame= 8851 fps=983 q=-1.0 size=  370039kB time=00:06:09.17 bitrate=8211.2kbits/s speed=  41x\n" +
                        "frame= 9305 fps=979 q=-1.0 size=  385474kB time=00:06:28.14 bitrate=8135.7kbits/s speed=40.8x\n" +
                        "frame= 9858 fps=985 q=-1.0 size=  419481kB time=00:06:51.24 bitrate=8356.0kbits/s speed=41.1x\n" +
                        "frame=10373 fps=987 q=-1.0 size=  443169kB time=00:07:12.74 bitrate=8389.2kbits/s speed=41.2x\n" +
                        "Error! This is the end of the log, good luck!"
                }
            };

            return new ObjectResult(Newtonsoft.Json.JsonConvert.SerializeObject(items));
        }

        private string GetStatus()
        {
            var queued = converter.ItemsQueued.Values;
            var converting = converter.ItemsProcessing.Values.Select(x => x.SourcePath).ToHashSet();

            var queueOnly = new List<IExportableConversionItem>();
            var convertingOnly = new List<IExportableConversionItem>();
            foreach (var item in queued)
            {
                if (converting.Contains(item.SourcePath))
                {
                    convertingOnly.Add(item);
                }
                else
                {
                    queueOnly.Add(item);
                }
            }

            var result = new Dictionary<string, object>
            {
                { "queued", queueOnly },
                { "processing", convertingOnly }
            };
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }


        [HttpPost("cancel")]
        public StatusCodeResult Cancel(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                converter.Cancel();
                libraryScanner.Cancel();
                log.LogInformation($"Cancelled entire queue.");
                return Ok();
            }
            else
            {
                if (converter.Cancel(name))
                {
                    log.LogInformation($"Cancelled {name}");
                    return Ok();
                }
                log.LogWarning($"Failed to cancel {name}");
                return NotFound();
            }
        }

        [HttpPost("pause")]
        public StatusCodeResult Pause()
        {
            converter.Pause();
            log.LogInformation($"Paused queue.");
            return Ok();
        }

        [HttpPost("resume")]
        public StatusCodeResult Resume()
        {
            converter.Resume();
            log.LogInformation($"Resumed queue.");
            return Ok();
        }
    }
}
