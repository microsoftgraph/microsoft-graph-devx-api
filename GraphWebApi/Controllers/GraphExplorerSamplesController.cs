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
        public async Task<IActionResult> Get(string id, string category)
        {
            if (id != null && category != null)
            {
                return BadRequest(); // Only one search parameter is allowed
            }

            try
            {
                // Get the file contents
                var jsonFileContents = await _fileUtility.ReadFromFile(_configuration["SampleQueriesFilePathName"]);

                // Get a list of the sample queries from the file contents
                var sampleQueriesList = SamplesService.GetSampleQueriesList(jsonFileContents);

                if (sampleQueriesList != null)
                {
                    if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(category))
                    {
                        return Ok(sampleQueriesList); // No query string values provided, return entire list of sample queries
                    }
                    else if (id != null)
                    {
                        // Search by Id
                        var sampleQueriesById = sampleQueriesList.SampleQueries.FindAll(x => x.Id == Guid.Parse(id));
                        if (sampleQueriesById != null)
                        {
                            return Ok(sampleQueriesById);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                    else
                    {
                        // Search by Category
                        var sampleQueriesByCategory = sampleQueriesList.SampleQueries.FindAll(x => x.Category.ToLower() == category.ToLower());
                        if (sampleQueriesByCategory != null)
                        {
                            return Ok(sampleQueriesByCategory);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
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