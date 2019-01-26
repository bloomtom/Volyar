using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Volyar.Controllers
{
    [Route("external/ui")]
    public class FrontendController : Controller
    {
        private readonly ILogger<FrontendController> log;

        public FrontendController(ILogger<FrontendController> logger)
        {
            log = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            log.LogInformation($"UI view requested.");
            return View();
        }
    }
}
