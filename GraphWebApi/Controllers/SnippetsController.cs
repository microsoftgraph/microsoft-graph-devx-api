// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.IO;
using CodeSnippetsReflection;
using GraphWebApi.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using UtilityService;
using CodeSnippetsReflection.OData;
using CodeSnippetsReflection.OpenAPI;
using System;
using System.Diagnostics.CodeAnalysis;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [Route("api/graphexplorersnippets")]
    [Route("snippetgenerator")]
    [ApiController]
    [ExcludeFromCodeCoverage]
    public class SnippetsController : ControllerBase
    {
        private readonly ISnippetsGenerator _oDataSnippetGenerator;
		private readonly ISnippetsGenerator _openApiSnippetGenerator;
		private readonly Dictionary<string, string> _snippetsTraceProperties =
            new() { { UtilityConstants.TelemetryPropertyKey_Snippets, nameof(SnippetsController) } };
        private readonly TelemetryClient _telemetryClient;

        public SnippetsController(IODataSnippetsGenerator oDataSnippetGenerator, IOpenApiSnippetsGenerator openApiSnippetGenerator, TelemetryClient telemetryClient)
        {
            UtilityFunctions.CheckArgumentNull(telemetryClient, nameof(telemetryClient));
            UtilityFunctions.CheckArgumentNull(openApiSnippetGenerator, nameof(openApiSnippetGenerator));
            UtilityFunctions.CheckArgumentNull(oDataSnippetGenerator, nameof(oDataSnippetGenerator));
            _telemetryClient = telemetryClient;
            _oDataSnippetGenerator = oDataSnippetGenerator;
            _openApiSnippetGenerator = openApiSnippetGenerator;
        }

        //Default Service Page GET
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Get(string arg)
        {
            if(string.IsNullOrWhiteSpace(arg))
            {
                _telemetryClient?.TrackTrace("Fetching code snippet",
                                             SeverityLevel.Information,
                                             _snippetsTraceProperties);

                string result = "Graph Explorer Snippets Generator";
                return new OkObjectResult(new CodeSnippetResult { Code = "null", StatusCode = false, Message = result, Language = "Default C#" });
            }
            else
            {
                _telemetryClient?.TrackTrace($"Fetching snippet based on '{arg}'",
                                             SeverityLevel.Information,
                                             _snippetsTraceProperties);
                string result = "Graph Explorer Snippets Generator";
                return new OkObjectResult(new CodeSnippetResult { Code = "null", StatusCode = false, Message = result, Language = "Default C#" });
            }
        }

        //POST api/graphexplorersnippets
        [HttpPost]
        [Consumes("application/http")]
        public async Task<IActionResult> PostAsync(string lang = "c#", string generation = "")
        {
            // Default to openapi generation if supported and not explicitly requested for.
            if (string.IsNullOrEmpty(generation))
                generation = (OpenApiSnippetsGenerator.SupportedLanguages.Contains(lang)) ? "openapi" : "odata";
            
            Request.EnableBuffering();
            using var streamContent = new StreamContent(Request.Body);
            streamContent.Headers.Add("Content-Type", "application/http;msgtype=request");

            using HttpRequestMessage requestPayload = await streamContent.ReadAsHttpRequestMessageAsync().ConfigureAwait(false);

            _telemetryClient?.TrackTrace($"Processing the request payload: '{requestPayload}'",
                                            SeverityLevel.Information,
                                            _snippetsTraceProperties);

            var response = await GetSnippetGenerator(generation).ProcessPayloadRequestAsync(requestPayload, lang);

            _telemetryClient?.TrackTrace("Finished generating a code snippet",
                                            SeverityLevel.Information,
                                            _snippetsTraceProperties);

            return string.IsNullOrEmpty(response) ? NoContent() : new StringResult(response);
        }
        private ISnippetsGenerator GetSnippetGenerator(string generation) {
            return generation.ToLowerInvariant() switch {
                "odata" => _oDataSnippetGenerator,
                "openapi" => _openApiSnippetGenerator,
                _ => throw new ArgumentException($"{generation} is not a valid generation type")
            };
        }
    }

    class StringResult : IActionResult
    {
        private readonly string _value;

        public StringResult(string value)
        {
            this._value = value;
        }
        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.ContentType = "text/plain";
            var streamWriter = new StreamWriter(context.HttpContext.Response.Body);
            await streamWriter.WriteAsync(this._value);
            await streamWriter.DisposeAsync();
        }
    }
}
