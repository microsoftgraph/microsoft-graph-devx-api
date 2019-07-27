using System;
using Microsoft.AspNetCore.Mvc;
using GraphExplorerSamplesService;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphExplorerSamplesController : ControllerBase
    {
        private readonly IFileUtility _fileUtility;
        private readonly IConfiguration _configuration;

        public GraphExplorerSamplesController(IFileUtility fileUtility, IConfiguration configuration)
        {
            _fileUtility = fileUtility;
            _configuration = configuration;
        }

        // Gets the list of all Sample Queries
        [HttpGet]
        [Produces("application/json")]
        public async Task<IActionResult> Get()
        {
            try
            {
                // Get the file contents
                var jsonFileContents = await _fileUtility.ReadFromFile(_configuration["SampleQueriesFilePathName"]);

                // Get a list of the sample queries from the file contents
                var sampleQueriesList = SamplesService.GetSampleQueriesList(jsonFileContents);

                if (sampleQueriesList != null)
                {                    
                    return Ok(sampleQueriesList);
                }
                // List is empty, just return status code 204 - No Content
                return NoContent();
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }            
        }                
    }
}