using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.OpenAPIService;
using Microsoft.OpenApi.Models;
using System.Threading.Tasks;

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

                OpenApiDocument source = await OpenApiService.GetGraphOpenApiDocument(graphVersion, forceRefresh);

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
    }
}
