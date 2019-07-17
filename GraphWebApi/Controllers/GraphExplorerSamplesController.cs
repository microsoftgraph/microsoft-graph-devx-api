using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphExplorerSamplesController : ControllerBase
    {
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Get()
        {
            return View();
        }

        //public async Task<IActionResult> PostAsync()
        //{

        //}
    }
}