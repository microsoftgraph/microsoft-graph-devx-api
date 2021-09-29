// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using OpenAPIService;
using OpenAPIService.Common;
using OpenAPIService.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UtilityService;
using Constants = OpenAPIService.Common.Constants;

namespace GraphWebApi.Controllers
{
    /// <summary>
    /// Controller that enables querying over an OpenAPI document
    /// </summary>
    [ApiController]
    [ExcludeFromCodeCoverage]
    public class OpenApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IOpenApiService _openApiService;

        public OpenApiController(IConfiguration configuration, IOpenApiService openApiService)
        {
            UtilityFunctions.CheckArgumentNull(openApiService, nameof(openApiService));
            UtilityFunctions.CheckArgumentNull(configuration, nameof(configuration));
            _configuration = configuration;
            _openApiService = openApiService;
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
            var styleOptions = new OpenApiStyleOptions(style, openApiVersion, graphVersion, format);

            var graphUri = GetVersionUri(styleOptions.GraphVersion);

            if (graphUri == null)
            {
                throw new InvalidOperationException($"Unsupported {nameof(graphVersion)} provided: '{graphVersion}'");
            }

            var source = await _openApiService.GetGraphOpenApiDocumentAsync(graphUri, forceRefresh);

            var predicate = _openApiService.CreatePredicate(operationIds: operationIds,
                                                            tags: tags,
                                                            url: url,
                                                            source: source,
                                                            graphVersion: styleOptions.GraphVersion,
                                                            forceRefresh: forceRefresh);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(source, title, styleOptions.GraphVersion, predicate);

            subsetOpenApiDocument = _openApiService.ApplyStyle(styleOptions.Style, subsetOpenApiDocument);

            var stream = _openApiService.SerializeOpenApiDocument(subsetOpenApiDocument, styleOptions);

            if (styleOptions.OpenApiFormat == "yaml")
            {
                return new FileStreamResult(stream, "text/yaml");
            }
            else
            {
                return new FileStreamResult(stream, "application/json");
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
                var styleOptions = new OpenApiStyleOptions(style, openApiVersion, graphVersion, format);

                var source = await _openApiService.ConvertCsdlToOpenApiAsync(Request.Body);

                var predicate = _openApiService.CreatePredicate(operationIds: operationIds,
                                                                     tags: tags,
                                                                     url: url,
                                                                     source: source,
                                                                     graphVersion: styleOptions.GraphVersion,
                                                                     forceRefresh: forceRefresh);

                var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(source, title, styleOptions.GraphVersion, predicate);

                subsetOpenApiDocument = _openApiService.ApplyStyle(styleOptions.Style, subsetOpenApiDocument);

                var stream = _openApiService.SerializeOpenApiDocument(subsetOpenApiDocument, styleOptions);

                if (styleOptions.OpenApiFormat == "yaml")
                {
                    return new FileStreamResult(stream, "text/yaml");
                }
                else
                {
                    return new FileStreamResult(stream, "application/json");
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
            var styleOptions = new OpenApiStyleOptions(style, openApiVersion, graphVersion, format);

            var graphUri = GetVersionUri(styleOptions.GraphVersion);

            if (graphUri == null)
            {
                throw new InvalidOperationException($"Unsupported {nameof(graphVersion)} provided: '{graphVersion}'");
            }

            var graphOpenApi = await _openApiService.GetGraphOpenApiDocumentAsync(graphUri, forceRefresh);
            await WriteIndex(Request.Scheme + "://" + Request.Host.Value, styleOptions.GraphVersion, styleOptions.OpenApiVersion, styleOptions.OpenApiFormat,
                graphOpenApi, Response.Body, styleOptions.Style);

            return new EmptyResult();
        }

        [Route("openapi/tree")]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string graphVersions = "*",
                                             [FromQuery] bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(graphVersions))
            {
                throw new InvalidOperationException($"{nameof(graphVersions)} parameter has an invalid value.");
            }

            HashSet<string> graphVersionsList = new();
            if (graphVersions == "*")
            {
                // Use both v1.0 and beta
                graphVersionsList.Add(Constants.OpenApiConstants.GraphVersion_V1);
                graphVersionsList.Add(Constants.OpenApiConstants.GraphVersion_Beta);
            }
            else
            {
                graphVersionsList.Add(graphVersions.ToLower());
            }

            var sources = new Dictionary<string, OpenApiDocument>();
            foreach (var graphVersion in graphVersionsList)
            {
                var graphUri = GetVersionUri(graphVersion);
                if (graphUri == null)
                {
                    throw new InvalidOperationException($"Unsupported {nameof(graphVersion)} provided: '{graphVersion}'");
                }

                sources.Add(graphVersion, await _openApiService.GetGraphOpenApiDocumentAsync(graphUri, forceRefresh));
            }

            var rootNode = _openApiService.CreateOpenApiUrlTreeNode(sources);
            using MemoryStream stream = new();
            _openApiService.ConvertOpenApiUrlTreeNodeToJson(rootNode, stream);
            return Content(Encoding.ASCII.GetString(stream.ToArray()), "application/json");
        }

        private static async Task WriteIndex(string baseUrl, string graphVersion, string openApiVersion, string format,
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
            return graphVersion.ToLower(CultureInfo.InvariantCulture) switch
            {
                "v1.0" => _configuration["GraphMetadata:V1.0"],
                "beta" => _configuration["GraphMetadata:Beta"],
                _ => null,
            };
        }
    }
}
