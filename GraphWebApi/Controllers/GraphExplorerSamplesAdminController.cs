using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphExplorerSamplesService.Interfaces;
using GraphExplorerSamplesService.Models;
using GraphExplorerSamplesService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphExplorerSamplesAdminController : ControllerBase
    {
        private readonly IFileUtility _fileUtility;
        private readonly string _policiesFilePathSource;

        public GraphExplorerSamplesAdminController(IFileUtility fileUtility, IConfiguration configuration)
        {
            _fileUtility = fileUtility;
            _policiesFilePathSource = configuration["SampleQueriesPoliciesFilePathName"]; // Sets the path of the policies file
        }

        // GET: api/GraphExplorerSamplesAdmin
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> GetSampleQueriesPoliciesListAsync(string search)
        {
            try
            {
                // Get the list of policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();

                if (string.IsNullOrEmpty(search))
                {
                    return Ok(policies);
                }

                List<CategoryPolicy> filteredPolicies = policies.CategoryPolicies.
                    FindAll(x => x.CategoryName.ToLower() == search.ToLower() ||
                    x.UserClaims.Exists(y => y.UserPrincipalName.ToLower() == search.ToLower()));
                
                if (filteredPolicies.Count == 0)
                {
                    // Search parameter not found in list of sample query policies
                    return NotFound();
                }

                // Success; return the found list of sample query policies from filtered search
                return Ok(filteredPolicies);
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
                
        // POST: api/GraphExplorerSamplesAdmin
        [HttpPost]
        public async Task<IActionResult> CreateUserClaimAsync([FromBody] CategoryPolicy categoryPolicy)
        {
            try
            {
                // Get the list of policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();

                // Add the new user claim in the given category policy
                SampleQueriesPolicies updatedPoliciesList = SamplesPolicyService.ModifyUserClaim(policies, categoryPolicy);
                                
                // Get the serialized JSON string of the sample query
                string updatedPoliciesJson = SamplesPolicyService.SerializeSampleQueriesPolicies(updatedPoliciesList);

                // Save the document-readable JSON-styled string to the source file
                await _fileUtility.WriteToFile(updatedPoliciesJson, _policiesFilePathSource);

                // Extract the first user claim from the given categoryPolicy; this is what was added
                UserClaim userClaim = categoryPolicy.UserClaims.First();

                // Create the query Uri for the newly created sample query
                string newUserClaimUri = string.Format("{0}://{1}{2}?search={3}&{4}", 
                    Request.Scheme, Request.Host, Request.Path.Value, userClaim.UserPrincipalName, categoryPolicy.CategoryName);

                // Success; return the new user claim that was added along with its Uri
                return Created(newUserClaimUri, categoryPolicy);
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // PUT: api/GraphExplorerSamplesAdmin/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        /// <summary>
        /// Gets the JSON file contents of the policies and returns a deserialized instance of a
        /// <see cref="SampleQueriesPolicies"/> from this.
        /// </summary>
        /// <returns></returns>
        private async Task<SampleQueriesPolicies> GetSampleQueriesPoliciesAsync()
        {
            // Get the file contents from source
            string jsonFileContents = await _fileUtility.ReadFromFile(_policiesFilePathSource);

            if (string.IsNullOrEmpty(jsonFileContents))
            {
                // Create default policies template
                SampleQueriesPolicies policies = SamplesPolicyService.CreateDefaultPoliciesTemplate();

                // Get the serialized JSON string of the list of policies
                string policiesJson = SamplesPolicyService.SerializeSampleQueriesPolicies(policies);

                // Save the document-readable JSON-styled string to the source file
                await _fileUtility.WriteToFile(policiesJson, _policiesFilePathSource);

                // Return the list of policies
                return policies;
            }

            // Return the list of policies
            return SamplesPolicyService.DeserializeSampleQueriesPolicies(jsonFileContents);
        }
    }
}
