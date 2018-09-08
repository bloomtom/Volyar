using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Volyar.Media.Conversion;

namespace Volyar.Controllers
{
    [Route("voly/external/ui")]
    public class FrontendController : Controller
    {
        private readonly ILogger<FrontendController> log;

        public FrontendController(ILogger<FrontendController> logger)
        {
            log = logger;
        }

        [HttpGet]
        public IActionResult Index(long transactionId)
        {
            return View();
        }
    }
}
