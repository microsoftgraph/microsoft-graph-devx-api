using System.IO;
using System.Threading.Tasks;
using GraphWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.OpenAPIService;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace GraphWebApi.Controllers
{
    [Route("list")]
    [ApiController]
    public class IndexController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get(string graphVersion = "v1.0", bool forceRefresh = false)
        {
            var graphOpenApi = await OpenApiService.GetGraphOpenApiDocument(graphVersion,forceRefresh);
            WriteIndex(this.Request.Scheme + "://"+ this.Request.Host.Value, graphVersion, graphOpenApi, Response.Body);
            
            return new EmptyResult();
        }

        private static void WriteIndex(string baseUrl, string graphVersion, OpenApiDocument graphOpenApi, Stream stream)
        {
            var sw = new StreamWriter(stream);
            
            var indexSearch = new OpenApiOperationIndex();
            var walker = new OpenApiWalker(indexSearch);

            walker.Walk(graphOpenApi);

            sw.AutoFlush = true;

            sw.WriteLine("<head>");
            sw.WriteLine("<link rel='stylesheet' href='./stylesheet.css' />");
            sw.WriteLine("</head>");
            sw.WriteLine("<h1># OpenAPI Operations for Microsoft Graph</h1>");
            sw.WriteLine("<b/>");
            sw.WriteLine("<ul>");
            foreach (var item in indexSearch.Index)
            {
                
                var target = $"{baseUrl}/openapi?tags={item.Key.Name}&openApiVersion=3&graphVersion={graphVersion}";
                sw.WriteLine($"<li>{item.Key.Name} [<a href='./openapi?tags={target}'>OpenApi</a>]   [<a href='/swagger/index.html#url={target}'>Swagger UI</a>]</li>");
                sw.WriteLine("<ul>");
                foreach (var op in item.Value)
                {
                    sw.WriteLine($"<li>{op.OperationId}  [<a href='./openapi?operationIds={op.OperationId}'>OpenAPI</a>]</li>");
                }
                sw.WriteLine("</ul>");
            }
            sw.WriteLine("</ul>");

        }
    }
}
