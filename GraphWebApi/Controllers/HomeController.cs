using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Reflection;

namespace GraphWebApi.Controllers
{
    [Route("")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            EmbeddedFileProvider embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
            Stream reader = embeddedProvider.GetFileInfo("wwwroot\\OpenApi.yaml").CreateReadStream();
            return new FileStreamResult(reader, "text/plain");
        }
    }
}
