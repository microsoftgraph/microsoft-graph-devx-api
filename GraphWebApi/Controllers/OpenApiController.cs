// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphWebApi.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using OpenAPIService;
using OpenAPIService.Common;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace GraphWebApi.Controllers
{
    /// <summary>
    /// Controller that enables querying over an OpenAPI document
    /// </summary>
    [ApiController]
    public class OpenApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly TelemetryClient _telemetry;

        public OpenApiController(IConfiguration configuration,
                                 TelemetryClient telemetry)
        {
            _configuration = configuration;
            _telemetry = telemetry;
            OpenApiService.TelemetryClient = telemetry;
        }

        [Route("openapi")]
        [Route("$openapi")]
        [HttpGet]
        public async Task<IActionResult> Get(
                                    [FromQuery]string operationIds = null,
                                    [FromQuery]string tags = null,
                                    [FromQuery]string url = null,
                                    [FromQuery]string openApiVersion = null,
                                    [FromQuery]string title = "Partial Graph API",
                                    [FromQuery]OpenApiStyle style = OpenApiStyle.Plain,
                                    [FromQuery]string format = null,
                                    [FromQuery]string graphVersion = null,
                                    [FromQuery]bool forceRefresh = false)
        {
            try
            {
                OpenApiStyleOptions styleOptions = new OpenApiStyleOptions(style, openApiVersion, graphVersion, format);

                string graphUri = GetVersionUri(styleOptions.GraphVersion);

                if (graphUri == null)
                {
                    throw new InvalidOperationException($"Unsupported {nameof(graphVersion)} provided: '{graphVersion}'");
                }

                OpenApiDocument source = await OpenApiService.GetGraphOpenApiDocumentAsync(graphUri, forceRefresh);

                var predicate = await OpenApiService.CreatePredicate(operationIds, tags, url, source, forceRefresh);

                var subsetOpenApiDocument = OpenApiService.CreateFilteredDocument(source, title, styleOptions.GraphVersion, predicate);

                subsetOpenApiDocument = OpenApiService.ApplyStyle(styleOptions.Style, subsetOpenApiDocument);

                var stream = OpenApiService.SerializeOpenApiDocument(subsetOpenApiDocument, styleOptions);

                if (styleOptions.OpenApiFormat == "yaml")
                {
                    return new FileStreamResult(stream, "text/yaml");
                }
                else
                {
                    return new FileStreamResult(stream, "application/json");
                }
            }
            catch (InvalidOperationException ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status400BadRequest };
            }
            catch (ArgumentException ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status404NotFound };
            }
            catch (Exception ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        [Route("openapi")]
        [HttpPost]
        public async Task<IActionResult> Post(
                            [FromQuery] string operationIds = null,
                            [FromQuery] string tags = null,
                            [FromQuery] string url = null,
                            [FromQuery] string openApiVersion = null,
                            [FromQuery] string title = "Partial Graph API",
                            [FromQuery] OpenApiStyle style = OpenApiStyle.Plain,
                            [FromQuery] string format = null,
                            [FromQuery] string graphVersion = null,
                            [FromQuery] bool forceRefresh = false)
        {
            try
            {
                OpenApiStyleOptions styleOptions = new OpenApiStyleOptions(style, openApiVersion, graphVersion, format);

                string graphUri = GetVersionUri(styleOptions.GraphVersion);

                if (graphUri == null)
                {
                    throw new InvalidOperationException($"Unsupported {nameof(graphVersion)} provided: '{graphVersion}'");
                }

                OpenApiDocument source = await OpenApiService.ConvertCsdlToOpenApiAsync(Request.Body);

                var predicate = await OpenApiService.CreatePredicate(operationIds, tags, url, source, forceRefresh);

                var subsetOpenApiDocument = OpenApiService.CreateFilteredDocument(source, title, styleOptions.GraphVersion, predicate);

                subsetOpenApiDocument = OpenApiService.ApplyStyle(styleOptions.Style, subsetOpenApiDocument);

                var stream = OpenApiService.SerializeOpenApiDocument(subsetOpenApiDocument, styleOptions);

                if (styleOptions.OpenApiFormat == "yaml")
                {
                    return new FileStreamResult(stream, "text/yaml");
                }
                else
                {
                    return new FileStreamResult(stream, "application/json");
                }
            }
            catch (InvalidOperationException ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status400BadRequest };
            }
            catch (ArgumentException ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status404NotFound };
            }
            catch (Exception ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        [Route("openapi/operations")]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery]string graphVersion = null,
                                             [FromQuery]string openApiVersion = null,
                                             [FromQuery]OpenApiStyle style = OpenApiStyle.Plain,
                                             [FromQuery]string format = null,
                                             [FromQuery]bool forceRefresh = false)
        {
            try
            {
                OpenApiStyleOptions styleOptions = new OpenApiStyleOptions(style, openApiVersion, graphVersion, format);

                string graphUri = GetVersionUri(styleOptions.GraphVersion);

                if (graphUri == null)
                {
                    throw new InvalidOperationException($"Unsupported {nameof(graphVersion)} provided: '{graphVersion}'");
                }

                var graphOpenApi = await OpenApiService.GetGraphOpenApiDocumentAsync(graphUri, forceRefresh);
                await WriteIndex(Request.Scheme + "://" + Request.Host.Value, styleOptions.GraphVersion, styleOptions.OpenApiVersion, styleOptions.OpenApiFormat,
                    graphOpenApi, Response.Body, styleOptions.Style);

                return new EmptyResult();
            }
            catch (InvalidOperationException ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status400BadRequest };
            }
            catch (ArgumentException ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status404NotFound };
            }
            catch (Exception ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        private async Task WriteIndex(string baseUrl, string graphVersion, string openApiVersion, string format,
                                OpenApiDocument graphOpenApi, Stream stream, OpenApiStyle style)

        {
            using var sw = new StreamWriter(stream);
            var indexSearch = new OpenApiOperationIndex();
            var walker = new OpenApiWalker(indexSearch);

            walker.Walk(graphOpenApi);

            await sw.WriteAsync("<head>" + Environment.NewLine +
                                "<link rel='stylesheet' href='./stylesheet.css' />" + Environment.NewLine +
                                "</head>" + Environment.NewLine +
                                "<h1># OpenAPI Operations for Microsoft Graph</h1>" + Environment.NewLine +
                                "<b/>" + Environment.NewLine +
                                "<ul>" + Environment.NewLine);

            foreach (var item in indexSearch.Index)
            {
                var target = $"{baseUrl}/openapi?tags={item.Key.Name}&openApiVersion={openApiVersion}&graphVersion={graphVersion}&format={format}&style={style}";
                await sw.WriteAsync($"<li>{item.Key.Name} [<a href='{target}'>OpenApi</a>]   [<a href='/swagger/index.html#url={target}'>Swagger UI</a>]</li>{Environment.NewLine}<ul>{Environment.NewLine}");
                foreach (var op in item.Value)
                {
                    await sw.WriteLineAsync($"<li>{op.OperationId}  [<a href='../../openapi?operationIds={op.OperationId}&openApiVersion={openApiVersion}&graphVersion={graphVersion}" +
                        $"&format={format}&style={style}'>OpenAPI</a>]</li>");
                }
                await sw.WriteLineAsync("</ul>");
            }
            await sw.WriteLineAsync("</ul>");
            await sw.FlushAsync();
        }

        private string GetVersionUri(string graphVersion)
        {
            switch (graphVersion.ToLower(CultureInfo.InvariantCulture))
            {
                case "v1.0":
                    return _configuration["GraphMetadata:V1.0"];
                case "beta":
                    return _configuration["GraphMetadata:Beta"];
                default:
                    return null;
            }
        }
    }
}
