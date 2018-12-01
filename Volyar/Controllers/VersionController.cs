using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Volyar.Controllers
{
    [Route("voly/external/api/version")]
    public class VersionController : Controller
    {
        [HttpGet]
        public IActionResult Version()
        {
            return Json(Program.version);
        }
    }
}
