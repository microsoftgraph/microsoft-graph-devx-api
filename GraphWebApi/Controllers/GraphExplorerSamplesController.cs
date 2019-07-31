using System;
using Microsoft.AspNetCore.Mvc;
using GraphExplorerSamplesService;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        
        // Gets entire list of samples queries or search by category
        [HttpGet]
        [Produces("application/json")]
        public async Task<IActionResult> Get(string search)
        {
            try
            {
                // Get the file contents
                string jsonFileContents = await _fileUtility.ReadFromFile(_configuration["SampleQueriesFilePathName"]);

                // Get a list of the sample queries from the file contents
                SampleQueriesList sampleQueriesList = SamplesService.GetSampleQueriesList(jsonFileContents);

                if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
                {
                    // List is empty, just return status code 204 - No Content
                    return NoContent();
                }

                if (string.IsNullOrEmpty(search))
                {
                    // No query string values provided, return entire list of sample queries
                    return Ok(sampleQueriesList);
                }

                // Search by Category
                List<SampleQueryModel> sampleQueriesByCategory = sampleQueriesList.SampleQueries.FindAll(x => x.Category.ToLower() == search.ToLower());

                if (sampleQueriesByCategory == null || sampleQueriesByCategory.Count == 0)
                {
                    // Search parameter data not found in list of sample queries
                    return NotFound();                    
                }

                return Ok(sampleQueriesByCategory);
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}