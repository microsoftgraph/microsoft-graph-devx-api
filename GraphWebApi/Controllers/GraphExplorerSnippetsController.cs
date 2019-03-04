using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CodeSnippetsReflection;
using System.Net.Http;
using GraphWebApi.Models;
using Microsoft.Extensions.Options;
using System.IO;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphExplorerSnippetsController : ControllerBase
    {
        private readonly ISnippetsGenerator _snippetGenerator;

        public GraphExplorerSnippetsController(ISnippetsGenerator snippetGenerator)
        {
            _snippetGenerator = snippetGenerator;
        }

        //Default Service Page GET 
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Get(string arg)
        {
            if (arg != String.Empty && arg != null)
            {
                string result = "Graph Explorer Snippets Generator";
                return new OkObjectResult(new CodeSnippetResult { Code = "null", StatusCode = false, Message = result, Language = "Default C#" });
            }
            else
            {
                string result = "Graph Explorer Snippets Generator";
                return new OkObjectResult(new CodeSnippetResult { Code = "null", StatusCode = false, Message = result, Language = "Default C#" });
            }
        }

        //POST api/graphexplorersnippets
        [HttpPost]
        [Consumes("text/message")]
        public async Task<IActionResult> PostAsync(string lang)
        {
            var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);

            var tempRequest = new HttpRequestMessage
            {
                Content = new ByteArrayContent(memoryStream.ToArray()),
            };

            // this header as 
            tempRequest.Content.Headers.Add("Content-Type", "application/http;msgtype=request");

            try
            {
                using (HttpRequestMessage requestPayload = await tempRequest.Content.ReadAsHttpRequestMessageAsync().ConfigureAwait(false))
                {
                    var response = _snippetGenerator.ProcessPayloadRequest(requestPayload, lang);
                    return new OkObjectResult(response);
                }
            }
            catch (Exception e)
            {
                //TODO handle this more explicitly. This is most likely a parsing error caused by malformed HTTP data
                return new BadRequestObjectResult(e);
            }
        }
    }
}