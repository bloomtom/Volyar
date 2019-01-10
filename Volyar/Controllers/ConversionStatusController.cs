using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using VolyConverter.Conversion;

namespace Volyar.Controllers
{
    [Route("external/api/conversion")]
    public class ConversionStatusController : Controller
    {
        private readonly DQP.IDistinctQueueProcessor<IConversionItem> converter;
        private readonly ILogger<ConversionStatusController> log;

        public ConversionStatusController(DQP.IDistinctQueueProcessor<IConversionItem> converter, ILogger<ConversionStatusController> logger)
        {
            this.converter = converter;
            log = logger;
        }

        [HttpGet("statusui")]
        public IActionResult StatusView(long transactionId)
        {
            return View("Progress");
        }

        [HttpGet("status")]
        public IActionResult Status(long transactionId)
        {
            string json = GetStatus();
            return new ObjectResult(json);
        }

        [HttpGet("teststatus")]
        public IActionResult TestStatus(long transactionId)
        {
            var quality = DEnc.Quality.GenerateDefaultQualities(DEnc.DefaultQuality.medium, "fast").ToHashSet();

            var result = new Dictionary<string, object>
            {
                {
                    "queued",
                    new List<ExportableConversionItem>()
                    {
                        new ConversionItem("/home/test/vid/vid1.mkv", "/home/output/", "testitem", quality, 24, null, null),
                        new ConversionItem("/home/test/vid/vid2.mkv", "/home/output/", "testitem2", quality, 24, null, null)
                    }
                },
                {
                    "processing",
                    new List<ExportableConversionItem>()
                    {
                        new ConversionItem("/home/test/vid/vid3.mkv", "/home/output/", "processItem", quality, 24, null, null)
                        {
                            Progress = new List<DescribedProgress>()
                            {
                                new DescribedProgress("Encoding", 0.25)
                            }
                        },
                        new ConversionItem("/home/test/vid/vid4.mkv", "/home/output/", "processItem2", quality, 24, null, null)
                        {
                            Progress = new List<DescribedProgress>()
                            {
                                new DescribedProgress("Encoding", 1),
                                new DescribedProgress("DASHify", 1),
                                new DescribedProgress("Post Process", 1),
                                new DescribedProgress("Upload", 0.47)
                            }
                        },
                        new ConversionItem("/home/test/vid/vid5.mkv", "/home/output/", "processItem3", quality, 24, null, null)
                        {
                            Progress = new List<DescribedProgress>()
                            {
                                new DescribedProgress("Encoding", 1),
                                new DescribedProgress("DASHify", 1),
                                new DescribedProgress("Post Process", 1),
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
            return new ObjectResult(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        }

        private string GetStatus()
        {
            var queued = converter.ItemsQueued.Values.Select(x => ExportableConversionItem.Copy(x));
            var converting = converter.ItemsProcessing.Values.Select(x => x.SourcePath).ToHashSet();

            var queueOnly = new List<ExportableConversionItem>();
            var convertingOnly = new List<ExportableConversionItem>();
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
    }
}
