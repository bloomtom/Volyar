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
