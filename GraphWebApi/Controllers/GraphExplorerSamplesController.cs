using System;
using Microsoft.AspNetCore.Mvc;
using GraphExplorerSamplesService;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphExplorerSamplesController : ControllerBase
    {
        private readonly ISamplesService _samplesService;
        private readonly IConfiguration _configuration;

        public GraphExplorerSamplesController(ISamplesService samplesService, IConfiguration configuration)
        {
            _samplesService = samplesService;
            _configuration = configuration;
        }

        [HttpGet]
        [Produces("application/json")]
        public IActionResult Get()
        {
            try
            {
                // Get the entire list of sample queries
                var listSamples = _samplesService.ReadFromJsonFile(_configuration["SampleQueriesFilePathName"]);
                if (listSamples != null)
                {                    
                    return Ok(listSamples);
                }
                // List is empty, just return status code 204 - No Content
                return NoContent();
            }
            catch (FileNotFoundException fileNotFoundException)
            {
                return new JsonResult(fileNotFoundException.Message + " : '" + fileNotFoundException.FileName + "'")
                { StatusCode = StatusCodes.Status404NotFound };
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }            
        }                
    }
}