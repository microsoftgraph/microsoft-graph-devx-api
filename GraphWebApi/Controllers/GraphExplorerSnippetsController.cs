using System;
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

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [Route("snippetgenerator")]
    [ApiController]
    public class GraphExplorerSnippetsController : ControllerBase
    {
        private readonly ISnippetsGenerator _snippetGenerator;
        private readonly Dictionary<string, string> _snippetsTraceProperties =
            new() { { UtilityConstants.TelemetryPropertyKey_Snippets, "SnippetsController" } };
        private readonly TelemetryClient _telemetryClient;

        public GraphExplorerSnippetsController(ISnippetsGenerator snippetGenerator, TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
            _snippetGenerator = snippetGenerator;
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
        public async Task<IActionResult> PostAsync(string lang = "c#")
        {
            Request.EnableBuffering();
            using var streamContent = new StreamContent(Request.Body);
            streamContent.Headers.Add("Content-Type", "application/http;msgtype=request");

            try
            {
                using HttpRequestMessage requestPayload = await streamContent.ReadAsHttpRequestMessageAsync().ConfigureAwait(false);

                _telemetryClient?.TrackTrace($"Processing the request payload: '{requestPayload}'",
                                             SeverityLevel.Information,
                                             _snippetsTraceProperties);

                var response = _snippetGenerator.ProcessPayloadRequest(requestPayload, lang);

                _telemetryClient?.TrackTrace("Finished generating a code snippet",
                                             SeverityLevel.Information,
                                             _snippetsTraceProperties);

                return new StringResult(response);
            }
            catch (Exception e)
            {
                _telemetryClient?.TrackException(e,
                                        _snippetsTraceProperties);
                return new BadRequestObjectResult(e.Message);
            }
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
            using var streamWriter = new StreamWriter(context.HttpContext.Response.Body);
            await streamWriter.WriteAsync(this._value);
            await streamWriter.FlushAsync().ConfigureAwait(false);
        }
    }
}
