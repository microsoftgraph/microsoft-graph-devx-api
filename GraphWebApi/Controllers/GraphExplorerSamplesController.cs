using System;
using Microsoft.AspNetCore.Mvc;
using GraphExplorerSamplesService;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            _filePathSource = configuration["SampleQueriesFilePathName"];
        }

        [Route("api/[controller]")]
        [HttpGet]
        [Produces("application/json")]
        public async Task<IActionResult> Get(string search)
        {
            try
            {
                // Get the list of the sample queries
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

                return sampleQueriesByCategory != null ? Ok(sampleQueriesByCategory) : (IActionResult)NotFound();
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        [Route("api/[controller]/{id}")]
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                // Get the list of the sample queries
                SampleQueriesList sampleQueriesList = await GetSampleQueriesList();

                if (sampleQueriesList == null)
                {
                    // List is empty, just return status code 204 - No Content
                    return NoContent();
                }

                // Search for given id
                SampleQueryModel sampleQueryById = sampleQueriesList.SampleQueries.Find(x => x.Id == Guid.Parse(id));

                return sampleQueryById != null ? Ok(sampleQueryById) : (IActionResult)NotFound();
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        [Route("api/[controller]/{id}")]
        [Produces("application/json")]
        [HttpPut]
        public async Task<IActionResult> Put(string id, [FromBody]SampleQueryModel sampleQueryModel)
        {          
            try
            {
                // Get the list of the sample queries
                SampleQueriesList sampleQueriesList = await GetSampleQueriesList();

                if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
                {
                    // List is empty, the sample query model is definitely not in the list
                    return NotFound();
                }

                SampleQueriesList updatedSampleQueriesList = SamplesService.UpdateSampleQueriesList(sampleQueriesList, sampleQueryModel, Guid.Parse(id));

                if (updatedSampleQueriesList == null)
                {
                    // Sample query model not in the list
                    return NotFound();
                }

                // Get the serialized JSON string
                string updatedSampleQueriesJson = SamplesService.SerializeSampleQueriesList(updatedSampleQueriesList);

                // Save JSON string to source file
                await _fileUtility.WriteToFile(updatedSampleQueriesJson, _filePathSource);

                // Return the sample query model that was updated
                return Ok(sampleQueryModel);

            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        [Route("api/[controller]")]
        [Produces("application/json")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]SampleQueryModel sampleQueryModel)
        {                    
            try
            {
                /* Create and instantiate a new instance of SampleQueriesList for holding the list of sample queries
                 * in case we get a null value when attempting to get the list of sample queries from a file source*/
                SampleQueriesList sampleQueriesList = new SampleQueriesList();

                // Get the list of the sample queries
                sampleQueriesList = await GetSampleQueriesList();

                // Add the new sample to the samples queries list
                SampleQueriesList newSampleQueriesList = SamplesService.AddToSampleQueriesList(sampleQueriesList, ref sampleQueryModel);

                // Get the serialized JSON string
                string newSampleQueriesJson = SamplesService.SerializeSampleQueriesList(newSampleQueriesList);

                // Save the JSON string to the source file
                await _fileUtility.WriteToFile(newSampleQueriesJson, _filePathSource);

                // Create the new sample query Uri
                string newSampleQueryUri = string.Format("{0}://{1}{2}/{3}", Request.Scheme, Request.Host, Request.Path.Value, sampleQueryModel.Id.ToString());

                // Return the sample query that was updated along with its Uri
                return Created(newSampleQueryUri, sampleQueryModel);
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        /// <summary>
        /// Gets the JSON file contents and gets a deserialized instance of a list of samples queries from this.
        /// </summary>
        /// <returns>The deserialized instance of the list of samples queries.</returns>
        private async Task<SampleQueriesList> GetSampleQueriesList()
        {
            // Get the file contents from source
            string jsonFileContents = await _fileUtility.ReadFromFile(_filePathSource);

            // Return the list of the sample queries from the file contents
            return SamplesService.GetSampleQueriesList(jsonFileContents);
        }
    }
}