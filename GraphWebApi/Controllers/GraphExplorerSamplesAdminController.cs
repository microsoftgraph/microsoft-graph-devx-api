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
            _policiesFilePathSource = configuration["SampleQueriesPoliciesFilePathName"]; // sets the path of the policies file
        }

        // Gets a list of all category policies, or a filtered list based on search parameters
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> GetSampleQueriesPoliciesListAsync([FromQuery] string userPrincipalName, [FromQuery] string categoryName)
        {
            try
            {
                // Fetch the list of category policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();
                
                // This will hold the filtered set of category policies
                SampleQueriesPolicies filteredPolicies = new SampleQueriesPolicies();

                if (!string.IsNullOrEmpty(userPrincipalName) && !string.IsNullOrEmpty(categoryName))
                {
                    filteredPolicies.CategoryPolicies =  GetUserPrincipalCategoryPolicies(policies, userPrincipalName, categoryName);
                }
                else if (!string.IsNullOrEmpty(userPrincipalName))
                {
                    filteredPolicies.CategoryPolicies = GetUserPrincipalCategoryPolicies(policies, userPrincipalName);
                }
                else if (!string.IsNullOrEmpty(categoryName))
                {
                    // Find all the user claims for the specified category
                    filteredPolicies.CategoryPolicies = policies.CategoryPolicies.FindAll(x => x.CategoryName.ToLower() == categoryName.ToLower());
                }
                else
                {
                    return Ok(policies);
                }
                
                if (filteredPolicies.CategoryPolicies == null || !filteredPolicies.CategoryPolicies.Any())
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

        // Adds a user claim into a category policy
        [Produces("application/json")]
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
                string newUserClaimUri = string.Format("{0}://{1}{2}?userprincipalname={3}&categoryname={4}", 
                    Request.Scheme, Request.Host, Request.Path.Value, userClaim.UserPrincipalName, categoryPolicy.CategoryName);

                // Success; return the new user claim in the category policy that was added along with its Uri
                return Created(newUserClaimUri, categoryPolicy);
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Updates a user claim in a category policy
        [Produces("application/json")]
        [HttpPut]
        public async Task<IActionResult> UpdateUserClaimAsync([FromBody] CategoryPolicy categoryPolicy)
        {
            try
            {
                // Get the list of policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();

                // Update the user claim in the given category policy
                SampleQueriesPolicies updatedPoliciesList = SamplesPolicyService.ModifyUserClaim(policies, categoryPolicy);

                // Get the serialized JSON string of the sample query
                string updatedPoliciesJson = SamplesPolicyService.SerializeSampleQueriesPolicies(updatedPoliciesList);

                // Save the document-readable JSON-styled string to the source file
                await _fileUtility.WriteToFile(updatedPoliciesJson, _policiesFilePathSource);

                // Success; return the user claim in the category policy that was just updated
                return Ok(categoryPolicy);
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Removes a user claim from a category policy given a User Principal Name and a category policy name
        [Produces("application/json")]
        [HttpDelete]
        public async Task<IActionResult> RemoveUserClaim([FromQuery] string userPrincipalName, [FromQuery] string categoryName)
        {
            try
            {
                if (string.IsNullOrEmpty(userPrincipalName) || string.IsNullOrEmpty(categoryName))
                {
                    return new JsonResult($"Provide both the user principal name and category policy name in the search parameter. " +
                        $"e.g. /api/GraphExplorerSamplesAdmin?search=xyz@microsoft.com&Users")
                    { StatusCode = StatusCodes.Status400BadRequest };
                }

                // Get the list of policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();

                // Remove the user claim in the given category policy
                SampleQueriesPolicies updatedPoliciesList = SamplesPolicyService.RemoveUserClaim(policies, categoryName, userPrincipalName);

                // Get the serialized JSON string of the sample query
                string updatedPoliciesJson = SamplesPolicyService.SerializeSampleQueriesPolicies(updatedPoliciesList);

                // Save the document-readable JSON-styled string to the source file
                await _fileUtility.WriteToFile(updatedPoliciesJson, _policiesFilePathSource);

                // Get the category policy that has just been updated
                CategoryPolicy categoryPolicy = updatedPoliciesList.CategoryPolicies.FirstOrDefault(x => x.CategoryName == categoryName);

                return Ok(categoryPolicy);
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }            
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

        /// <summary>
        /// Gets the list of <see cref="CategoryPolicy"/> that a User Principal Name has claims in.
        /// </summary>
        /// <param name="policies">The list of category policies to search in.</param>
        /// <param name="userPrincipalName">The User Principal Name which to search for in the category policies.</param>
        /// <returns>A list of filtered <see cref="CategoryPolicy"/> for the specified User Principal Name.</returns>
        private static List<CategoryPolicy> GetUserPrincipalCategoryPolicies(SampleQueriesPolicies policies, string userPrincipalName)
        {
            // Find all category policies that the specified user principal has claims in
            List<CategoryPolicy> userPrincipalPolicies = policies.CategoryPolicies.FindAll(x => x.UserClaims.Exists(y => y.UserPrincipalName == userPrincipalName));

            if (!userPrincipalPolicies.Any())
            {
                return null;
            }

            List<CategoryPolicy> filteredCategoryPolicies = new List<CategoryPolicy>();

            // Filter each category policy to extract the specified user principal's claims
            foreach (CategoryPolicy categoryPolicy in userPrincipalPolicies)
            {
                UserClaim userClaim = categoryPolicy.UserClaims.Find(x => x.UserPrincipalName == userPrincipalName);

                CategoryPolicy userPrincipalPolicy = new CategoryPolicy
                {
                    CategoryName = categoryPolicy.CategoryName,
                    UserClaims = new List<UserClaim> { userClaim }
                };

                filteredCategoryPolicies.Add(userPrincipalPolicy);
            }

            return filteredCategoryPolicies;
        }

        /// <summary>
        /// Gets the <see cref="UserClaim"/> for a given <see cref="CategoryPolicy"/>. 
        /// </summary>
        /// <param name="policies">The list of category policies to search in.</param>
        /// <param name="userPrincipalName">The User Principal Name which to search for in the category policies.</param>
        /// <param name="categoryName">The name of the category policy to be searched for in the list of <see cref="CategoryPolicy"/>.</param>
        /// <returns>The list of <see cref="CategoryPolicy"/> with the searched for category and the <see cref="UserClaim"/> for the specified User Principal Name.</returns>
        private static List<CategoryPolicy> GetUserPrincipalCategoryPolicies(SampleQueriesPolicies policies, string userPrincipalName, string categoryName)
        {
            // Find the category policy that the specified user principal has claims in
            CategoryPolicy categoryPolicy = policies.CategoryPolicies.Find
               (x => x.CategoryName.ToLower() == categoryName.ToLower() && x.UserClaims.Exists(y => y.UserPrincipalName.ToLower() == userPrincipalName.ToLower()));

            if (categoryPolicy == null)
            {
                return null;
            }

            UserClaim userClaim = categoryPolicy.UserClaims.Find(x => x.UserPrincipalName.ToLower() == userPrincipalName.ToLower());
            categoryPolicy.UserClaims = new List<UserClaim> {userClaim };
            List<CategoryPolicy> filteredCategoryPolicies = new List<CategoryPolicy> { categoryPolicy };

            return filteredCategoryPolicies;
        }
    }
}
