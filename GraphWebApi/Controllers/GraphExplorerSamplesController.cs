using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using GraphExplorerSamplesService.Services;
using GraphExplorerSamplesService.Models;
using GraphExplorerSamplesService.Interfaces;

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

        // Gets the list of all sample queries
        [Route("api/[controller]")]
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> GetSampleQueriesList(string search)
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
                    // No query string value provided; return entire list of sample queries
                    return Ok(sampleQueriesList); 
                }

                // Search sample queries
                List<SampleQueryModel> filteredSampleQueries = sampleQueriesList.SampleQueries.
                    FindAll(x => (x.Category != null && x.Category.ToLower().Contains(search.ToLower())) || 
                                 (x.HumanName != null && x.HumanName.ToLower().Contains(search.ToLower())) ||
                                 (x.Tip != null && x.Tip.ToLower().Contains(search.ToLower())));
                
                if (filteredSampleQueries.Count == 0)
                {
                    // Search parameter not found in list of sample queries
                    return NotFound();
                }

                // Success; return the found list of sample queries from filtered search
                return Ok(filteredSampleQueries);
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

       // Gets a sample query from the list of sample queries by its id
       [Route("api/[controller]/{id}")]
       [Produces("application/json")]
       [HttpGet]
        public async Task<IActionResult> GetSampleQueryById(string id)
        {
            try
            {
                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await GetSampleQueriesList();

                if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
                {
                    return NoContent(); // list is empty, just return status code 204 - No Content
                }

                // Search for sample query with the provided id
                SampleQueryModel sampleQueryById = sampleQueriesList.SampleQueries.Find(x => x.Id == Guid.Parse(id));

                if (sampleQueryById == null)
                {
                    return NotFound(); // sample query with the given id doesn't exist in the list of sample queries
                }

                // Success; return the found sample query
                return Ok(sampleQueryById);
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Updates a sample query given its id value
        [Route("api/[controller]/{id}")]
        [Produces("application/json")]
        [HttpPut]
        public async Task<IActionResult> UpdateSampleQuery(string id, [FromBody]SampleQueryModel sampleQueryModel)
        {          
            try
            {
                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await GetSampleQueriesList();

                if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
                {                    
                    return NotFound(); // List is empty; the sample query being searched is definitely not in an empty list
                }

                // Update the provided sample query model into the list of sample queries
                SampleQueriesList updatedSampleQueriesList = SamplesService.UpdateSampleQueriesList(sampleQueriesList, sampleQueryModel, Guid.Parse(id));

                if (updatedSampleQueriesList == null)
                {                    
                    return NotFound(); // Update failed; sample query model of provided id not found in the list of sample queries
                }

                // Get the serialized JSON string of this sample query
                string updatedSampleQueriesJson = SamplesService.SerializeSampleQueriesList(updatedSampleQueriesList);

                // Save the document-readable JSON-styled string to the source file
                await _fileUtility.WriteToFile(updatedSampleQueriesJson, _filePathSource);

                // Success; return the sample query model object that was just updated
                return Ok(sampleQueryModel);
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Adds a new sample query to the list of sample queries
        [Route("api/[controller]")]
        [Produces("application/json")]
        [HttpPost]
        public async Task<IActionResult> CreateSampleQuery([FromBody]SampleQueryModel sampleQueryModel)
        {                    
            try
            {
                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await GetSampleQueriesList();

                // Assign a new Id to the new sample query
                sampleQueryModel.Id = Guid.NewGuid();

                // Add the new sample query to the list of sample queries
                SampleQueriesList newSampleQueriesList = SamplesService.AddToSampleQueriesList(sampleQueriesList, sampleQueryModel);

                // Get the serialized JSON string of the sample query
                string newSampleQueriesJson = SamplesService.SerializeSampleQueriesList(newSampleQueriesList);

                // Save the document-readable JSON-styled string to the source file
                await _fileUtility.WriteToFile(newSampleQueriesJson, _filePathSource);

                // Create the query Uri for the newly created sample query
                string newSampleQueryUri = string.Format("{0}://{1}{2}/{3}", Request.Scheme, Request.Host, Request.Path.Value, sampleQueryModel.Id.ToString());

                // Success; return the new sample query that was added along with its Uri
                return Created(newSampleQueryUri, sampleQueryModel);
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Deletes a sample query of the provided id from the list of smaple queries
        [Route("api/[controller]/{id}")]
        [Produces("application/json")]
        [HttpDelete]
        public async Task<IActionResult> DeleteSampleQuery(string id)
        {
            try
            {
                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await GetSampleQueriesList();

                if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
                {                    
                    return NotFound(); // list is empty; the sample query being searched is definitely not in an empty list
                }

                // Remove the sample query with given id from the list of sample queries
                sampleQueriesList = SamplesService.RemoveSampleQuery(sampleQueriesList, Guid.Parse(id));

                if (sampleQueriesList == null)
                {                    
                    return NotFound(); // sample query with provided id not found
                }

                // Get the serialized JSON string of the list of sample queries
                string newSampleQueriesJson = SamplesService.SerializeSampleQueriesList(sampleQueriesList);

                // Save the document-readable JSON-styled string to the source file
                await _fileUtility.WriteToFile(newSampleQueriesJson, _filePathSource);
                                
                // Success; no content to return
                return new JsonResult("Deleted successfully.") { StatusCode = StatusCodes.Status204NoContent};
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        /// <summary>
        /// Gets the JSON file contents and returns a deserialized instance of a <see cref="SampleQueriesList"/> from this.
        /// </summary>
        /// <returns>The deserialized instance of a <see cref="SampleQueriesList"/>.</returns>
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
            return SamplesService.DeserializeSampleQueriesList(jsonFileContents);
        }
    }
}