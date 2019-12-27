using Microsoft.AspNetCore.Mvc;
using OpenAPIService;
using Microsoft.OpenApi.Models;
using System.Threading.Tasks;
using System.IO;
using GraphWebApi.Models;
using Microsoft.OpenApi.Services;

namespace GraphWebApi.Controllers
{
    /// <summary>
    /// Controller that enables querying over an OpenAPI document
    /// </summary>
    public class OpenApiController : ControllerBase
    {
        [Route("openapi")]
        [Route("$openapi")]
        [HttpGet]
        public async Task<IActionResult> Get(                                    
                                    [FromQuery]string operationIds = null,
                                    [FromQuery]string tags = null,
                                    [FromQuery]string url = null,
                                    [FromQuery]string openApiVersion = "2",
                                    [FromQuery]string title = "Partial Graph API",
                                    [FromQuery]OpenApiStyle style = OpenApiStyle.Plain,
                                    [FromQuery]string format = "yaml",
                                    [FromQuery]string graphVersion = "v1.0",
                                    [FromQuery]bool forceRefresh = false)
        {
            try
            {
                var predicate = OpenApiService.CreatePredicate(operationIds, tags, url, graphVersion, forceRefresh);

                if (predicate == null)
                {
                    return new BadRequestResult();
                }

                OpenApiDocument source = await OpenApiService.GetGraphOpenApiDocumentAsync(graphVersion, forceRefresh);

                var subsetOpenApiDocument = OpenApiService.CreateFilteredDocument(source, title, graphVersion, predicate);

                subsetOpenApiDocument = OpenApiService.ApplyStyle(style, subsetOpenApiDocument);

                var stream = OpenApiService.SerializeOpenApiDocument(subsetOpenApiDocument, openApiVersion, format);
                return new FileStreamResult(stream, "application/json");
            }
            catch
            {
                return new BadRequestResult();
            }            
        }

        [Route("openapi/operations")]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery]string graphVersion = "v1.0", 
                                             [FromQuery]bool forceRefresh = false)
        {
            try
            {
                var graphOpenApi = await OpenApiService.GetGraphOpenApiDocumentAsync(graphVersion, forceRefresh);
                WriteIndex(Request.Scheme + "://" + Request.Host.Value, graphVersion, graphOpenApi, Response.Body);

                return new EmptyResult();
            }
            catch 
            {
                return new BadRequestResult();
            }           
        }

        private void WriteIndex(string baseUrl, string graphVersion, OpenApiDocument graphOpenApi, Stream stream)
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
                sw.WriteLine($"<li>{item.Key.Name} [<a href='../../openapi?tags={target}'>OpenApi</a>]   [<a href='/swagger/index.html#url={target}'>Swagger UI</a>]</li>");
                sw.WriteLine("<ul>");
                foreach (var op in item.Value)
                {
                    sw.WriteLine($"<li>{op.OperationId}  [<a href='../../openapi?operationIds={op.OperationId}'>OpenAPI</a>]</li>");
                }
                sw.WriteLine("</ul>");
            }
            sw.WriteLine("</ul>");
            sw.Dispose();
        }
    }
}
