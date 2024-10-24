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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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
            ArgumentNullException.ThrowIfNull(openApiService, nameof(openApiService));
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            _configuration = configuration;
            _openApiService = openApiService;
        }

        [Route("openapi")]
        [Route("$openapi")]
        [HttpGet]
        public async Task<IActionResult> GetAsync(
                                    [FromQuery] string operationIds = null,
                                    [FromQuery] string tags = null,
                                    [FromQuery] string url = null,
                                    [FromQuery] string openApiVersion = null,
                                    [FromQuery] string title = "Partial Graph API",
                                    [FromQuery] OpenApiStyle style = OpenApiStyle.Plain,
                                    [FromQuery] string format = null,
                                    [FromQuery] string graphVersion = null,
                                    [FromQuery] bool includeRequestBody = false,
                                    [FromQuery] bool forceRefresh = false,
                                    [FromQuery] bool singularizeOperationIds = false,
                                    [FromQuery] string fileName = null)
        {
            var styleOptions = new OpenApiStyleOptions(style, openApiVersion, graphVersion, format);

            var graphUri = GetVersionUri(styleOptions.GraphVersion);

            if (string.IsNullOrEmpty(graphUri))
            {
                throw new InvalidOperationException($"Unsupported {nameof(graphVersion)} provided: '{graphVersion}'");
            }

            var source = await _openApiService.GetGraphOpenApiDocumentAsync(graphUri, style, forceRefresh, fileName);
            return CreateSubsetOpenApiDocument(operationIds, tags, url, source, title, styleOptions, forceRefresh, includeRequestBody, singularizeOperationIds);
        }

        [Route("openapi/operations")]
        [HttpGet]
        public async Task<IActionResult> GetAsync([FromQuery] string graphVersion = null,
                                             [FromQuery] string openApiVersion = null,
                                             [FromQuery] OpenApiStyle style = OpenApiStyle.Plain,
                                             [FromQuery] string format = null,
                                             [FromQuery] bool forceRefresh = false,
                                             [FromQuery] bool singularizeOperationIds = false,
                                             [FromQuery] string fileName = null)
        {
            var styleOptions = new OpenApiStyleOptions(style, openApiVersion, graphVersion, format);

            var graphUri = GetVersionUri(styleOptions.GraphVersion);

            if (string.IsNullOrEmpty(graphUri))
            {
                throw new InvalidOperationException($"Unsupported {nameof(graphVersion)} provided: '{graphVersion}'");
            }

            var graphOpenApi = await _openApiService.GetGraphOpenApiDocumentAsync(graphUri, style, forceRefresh, fileName);
            await WriteIndexAsync(Request.Scheme + "://" + Request.Host.Value, styleOptions.GraphVersion, styleOptions.OpenApiVersion, styleOptions.OpenApiFormat,
                graphOpenApi, Response.Body, styleOptions.Style, singularizeOperationIds, fileName);

            return new EmptyResult();
        }

        [Route("openapi/tree")]
        [HttpGet]
        public async Task GetAsync([FromQuery] string graphVersions = "*",
                                             [FromQuery] bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(graphVersions))
            {
                throw new InvalidOperationException($"{nameof(graphVersions)} parameter has an invalid value.");
            }

            HashSet<string> graphVersionsList = new();
            if ("*".Equals(graphVersions, StringComparison.OrdinalIgnoreCase))
            {
                // Use both v1.0 and beta
                graphVersionsList.Add(Constants.OpenApiConstants.GraphVersion_V1);
                graphVersionsList.Add(Constants.OpenApiConstants.GraphVersion_Beta);
            }
            else
            {
                graphVersionsList.Add(graphVersions.ToLower());
            }

            var sources = new ConcurrentDictionary<string, OpenApiDocument>();
            foreach (var graphVersion in graphVersionsList)
            {
                var graphUri = GetVersionUri(graphVersion);
                if (string.IsNullOrEmpty(graphUri))
                {
                    throw new InvalidOperationException($"Unsupported {nameof(graphVersion)} provided: '{graphVersion}'");
                }

                sources.TryAdd(graphVersion, await _openApiService.GetGraphOpenApiDocumentAsync(graphUri, OpenApiStyle.Plain, forceRefresh));
            }

            var rootNode = _openApiService.CreateOpenApiUrlTreeNode(sources);

            Response.ContentType = "application/json";
            Response.StatusCode = 200;
            await Response.StartAsync();

            var writer = new Utf8JsonWriter(Response.BodyWriter, new JsonWriterOptions() { Indented = false });
            OpenApiService.ConvertOpenApiUrlTreeNodeToJson(writer, rootNode);
            await writer.FlushAsync();
            await Response.CompleteAsync();
        }

        private FileStreamResult CreateSubsetOpenApiDocument(string operationIds, string tags,
                                                             string url, OpenApiDocument source,
                                                             string title, OpenApiStyleOptions styleOptions,
                                                             bool forceRefresh, bool includeRequestBody,
                                                             bool singularizeOperationIds)
        {
            var predicate = _openApiService.CreatePredicate(operationIds, tags, url, source, styleOptions.GraphVersion, forceRefresh);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(source, title, styleOptions.GraphVersion, predicate);

            subsetOpenApiDocument = _openApiService.ApplyStyle(styleOptions.Style, subsetOpenApiDocument, includeRequestBody, singularizeOperationIds);

            var stream = _openApiService.SerializeOpenApiDocument(subsetOpenApiDocument, styleOptions);

            if ("yaml".Equals(styleOptions.OpenApiFormat, StringComparison.OrdinalIgnoreCase))
            {
                return new FileStreamResult(stream, "text/yaml");
            }
            else
            {
                return new FileStreamResult(stream, "application/json");
            }
        }

        private static async Task WriteIndexAsync(
            string baseUrl,
            string graphVersion,
            string openApiVersion,
            string format,
            OpenApiDocument graphOpenApi,
            Stream stream,
            OpenApiStyle style,
            bool singularizeOperationIds,
            string fileName = null)

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

            string fileNameParam = !string.IsNullOrEmpty(fileName) ? "&fileName=" + fileName : null;

            foreach (var item in indexSearch.Index)
            {
                var target = $"{baseUrl}/openapi?tags={item.Key.Name}&openApiVersion={openApiVersion}&graphVersion={graphVersion}&format={format}&style={style}&singularizeOperationIds={singularizeOperationIds}{fileNameParam}";
                await sw.WriteAsync($"<li>{item.Key.Name} [<a href='{target}'>OpenApi</a>]   [<a href='/swagger/index.html#url={target}'>Swagger UI</a>]</li>{Environment.NewLine}<ul>{Environment.NewLine}");
                foreach (var op in item.Value)
                {
                    await sw.WriteLineAsync($"<li>{op.OperationId}  [<a href='../../openapi?operationIds={op.OperationId}&openApiVersion={openApiVersion}&graphVersion={graphVersion}" +
                        $"&format={format}&style={style}&singularizeOperationIds={singularizeOperationIds}{fileNameParam}'>OpenAPI</a>]</li>");
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
