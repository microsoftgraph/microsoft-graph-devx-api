using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CodeSnippetsReflection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Text;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphExplorerSnippetsController : ControllerBase
    {
        // GET api/values/GET,/me/
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Get(string arg)
        {
            if (arg != String.Empty && arg != null)
            {
                SnippetsGeneratorCSharp code = new SnippetsGeneratorCSharp();
                string snippet = code.GenerateCode(arg);

                return new OkObjectResult(new CodeSnippetResult { Code = snippet, StatusCode = true, Message = "Success" , Language="C#"});
            }
            else
            {
                string result = "No Results! The Service expects atleast a HTTP Method and Graph Resource." + Environment.NewLine +
                        "You can also add OData parameters which are optional." + Environment.NewLine + "An example of a paramater expect would be [GET,/me/events]";

                return new OkObjectResult(new CodeSnippetResult { Code = "", StatusCode = false, Message = result, Language="C#" });
            }
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }

    public class CodeSnippetResult
    {
        public string Code { get; set; }
        public bool StatusCode { get; set; }
        public string Message { get; set; }
        public string Language { get; set; }
    }
}
