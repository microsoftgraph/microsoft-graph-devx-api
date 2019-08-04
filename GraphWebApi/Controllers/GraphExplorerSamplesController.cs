using System;
using Microsoft.AspNetCore.Mvc;
using GraphExplorerSamplesService;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using GraphExplorerExtensions;

namespace GraphWebApi.Controllers
{

    [ApiController]
    public class GraphExplorerSamplesController : ControllerBase
    {
        private readonly IFileUtility _fileUtility;
        private readonly string _filePathSource;

        public GraphExplorerSamplesController(IFileUtility fileUtility, IConfiguration configuration)
        {
            _fileUtility = fileUtility;            
            _filePathSource = configuration["SampleQueriesFilePathName"]; // Gets the path of the JSON file
        }

        // Gets the list of all sample queries or queries for the searched category
        [Route("api/[controller]")]        
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> Get(string search)
        {
            try
            {
                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await GetSampleQueriesList();

                if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
                {
                    // List is empty, just return status code 204 - No Content
                    return NoContent();
                }

                if (string.IsNullOrEmpty(search))
                {
                    // No query string value provided, return entire list of sample queries
                    return Ok(sampleQueriesList); 
                }

                // Search by Category
                List<SampleQueryModel> sampleQueriesByCategory = sampleQueriesList.SampleQueries.FindAll(x => x.Category.ToLower() == search.ToLower());

                if (sampleQueriesByCategory == null || sampleQueriesByCategory.Count == 0)
                {
                    // Search parameter not found in list of sample queries
                    return NotFound();
                }

                // Success
                return Ok(sampleQueriesByCategory);
            }
            catch (Exception exception)
            {
                // Internal server error
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Gets a sample query from the list of sample queries by its id
        [Route("api/[controller]/{id}")]
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await GetSampleQueriesList();

                if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
                {
                    // List is empty, just return status code 204 - No Content
                    return NoContent();
                }

                // Search for sample query with the provided id
                SampleQueryModel sampleQueryById = sampleQueriesList.SampleQueries.Find(x => x.Id == Guid.Parse(id));

                if (sampleQueryById == null)
                {
                    // Sample query with the given id doesn't exist in the list of sample queries
                    return NotFound();
                }

                // Success
                return Ok(sampleQueryById);
            }
            catch (Exception exception)
            {
                // Internal server error
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Updates a sample query given its id value
        [Route("api/[controller]/{id}")]
        [Produces("application/json")]
        [HttpPut]
        public async Task<IActionResult> Put(string id, [FromBody]SampleQueryModel sampleQueryModel)
        {          
            try
            {
                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await GetSampleQueriesList();

                if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
                {
                    // List is empty; the sample query being searched is definitely not in an empty list
                    return NotFound();
                }

                // Update the provided sample query model into the list of sample queries
                SampleQueriesList updatedSampleQueriesList = SamplesService.UpdateSampleQueriesList(sampleQueriesList, sampleQueryModel, Guid.Parse(id));

                if (updatedSampleQueriesList == null)
                {
                    // Update failed; sample query model of provided id not found in the list of sample queries
                    return NotFound();
                }

                // Get the serialized JSON string of this sample query
                string updatedSampleQueriesJson = SamplesService.SerializeSampleQueriesList(updatedSampleQueriesList);

                // Format the string into a document-readable JSON-styled string
                updatedSampleQueriesJson = updatedSampleQueriesJson.FormatStringForJsonDocument();

                // Save the document-readable JSON-styled string to the source file
                await _fileUtility.WriteToFile(updatedSampleQueriesJson, _filePathSource);

                // Success; return the sample query model that was just updated
                return Ok(sampleQueryModel);
            }
            catch (Exception exception)
            {
                // Internal server error
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Adds a new sample query to the list of sample queries
        [Route("api/[controller]")]
        [Produces("application/json")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]SampleQueryModel sampleQueryModel)
        {                    
            try
            {
                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await GetSampleQueriesList();

                // Add the new sample query to the list of sample queries
                SampleQueriesList newSampleQueriesList = SamplesService.AddToSampleQueriesList(sampleQueriesList, ref sampleQueryModel);

                // Get the serialized JSON string of the sample query
                string newSampleQueriesJson = SamplesService.SerializeSampleQueriesList(newSampleQueriesList);

                // Format the string into a document-readable JSON-styled string
                newSampleQueriesJson = newSampleQueriesJson.FormatStringForJsonDocument();

                // Save the document-readable JSON-styled string to the source file
                await _fileUtility.WriteToFile(newSampleQueriesJson, _filePathSource);

                // Create the query Uri for the newly created sample query
                string newSampleQueryUri = string.Format("{0}://{1}{2}/{3}", Request.Scheme, Request.Host, Request.Path.Value, sampleQueryModel.Id.ToString());

                // Success; return the new sample query that was added along with its Uri
                return Created(newSampleQueryUri, sampleQueryModel);
            }
            catch (Exception exception)
            {
                // Internal server error
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Deletes a sample query of the provided id from the list of smaple queries
        [Route("api/[controller]/{id}")]
        [Produces("application/json")]
        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await GetSampleQueriesList();

                if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
                {
                    // List is empty; the sample query being searched is definitely not in an empty list
                    return NotFound();
                }

                // Remove the sample query with given id from the list of sample queries
                sampleQueriesList = SamplesService.RemoveSampleQuery(sampleQueriesList, Guid.Parse(id));

                if (sampleQueriesList == null)
                {
                    // Sample query with provided id not found
                    return NotFound();
                }

                // Get the serialized JSON string of the list of sample queries
                string newSampleQueriesJson = SamplesService.SerializeSampleQueriesList(sampleQueriesList);

                // Format the string into a document-readable JSON-styled string
                newSampleQueriesJson = newSampleQueriesJson.FormatStringForJsonDocument();

                // Save the document-readable JSON-styled string to the source file
                await _fileUtility.WriteToFile(newSampleQueriesJson, _filePathSource);
                                
                // Success
                return new JsonResult("Deleted successfully.") { StatusCode = StatusCodes.Status204NoContent};
            }
            catch (Exception exception)
            {
                // Internal server error
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        /// <summary>
        /// Gets the JSON file contents and returns a deserialized instance of a list of sample query objects from this.
        /// </summary>
        /// <returns>The deserialized instance of the list of sample queries.</returns>
        private async Task<SampleQueriesList> GetSampleQueriesList()
        {
            // Get the file contents from source
            string jsonFileContents = await _fileUtility.ReadFromFile(_filePathSource);

            if (string.IsNullOrEmpty(jsonFileContents))
            {
                /* File is empty; instantiate a new list of sample query 
                 * objects that will be used to add new sample queries*/
                return new SampleQueriesList(new List<SampleQueryModel>());
            }

            // Return the list of the sample queries from the file contents
            return SamplesService.GetSampleQueriesList(jsonFileContents);
        }
    }
}