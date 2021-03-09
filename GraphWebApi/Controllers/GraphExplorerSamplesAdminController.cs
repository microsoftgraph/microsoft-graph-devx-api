using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FileService.Interfaces;
using GraphExplorerSamplesService.Models;
using GraphExplorerSamplesService.Services;
using GraphWebApi.Common;
using GraphWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [Route("samplesadmin")]
    [ApiController]
    public class GraphExplorerSamplesAdminController : ControllerBase
    {
        private readonly IFileUtility _fileUtility;
        private readonly string _policiesFilePathSource;
        private readonly SamplesAdministrators _administrators;

        public GraphExplorerSamplesAdminController(IFileUtility fileUtility, IConfiguration configuration, IOptionsMonitor<SamplesAdministrators> administrators)
        {
            _fileUtility = fileUtility;
            _policiesFilePathSource = configuration["Samples:SampleQueriesPoliciesFilePathName"]; // sets the path of the policies file
            _administrators = administrators.CurrentValue; // sets the list of samples administrators
        }

        // Gets a list of all category policies, or a filtered list based on provided parameters
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> GetSampleQueriesPoliciesListAsync([FromQuery] string userPrincipalName, [FromQuery] string categoryName)
        {
            try
            {
                // Fetch the list of category policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();
                
                // This will hold the filtered list of category policies
                SampleQueriesPolicies filteredPolicies = new SampleQueriesPolicies();

                if (!string.IsNullOrEmpty(userPrincipalName) && !string.IsNullOrEmpty(categoryName))
                {
                    filteredPolicies.CategoryPolicies = GetUserPrincipalCategoryPolicies(policies, userPrincipalName, categoryName);
                }
                else if (!string.IsNullOrEmpty(userPrincipalName))
                {
                    filteredPolicies.CategoryPolicies = GetUserPrincipalCategoryPolicies(policies, userPrincipalName);
                }
                else if (!string.IsNullOrEmpty(categoryName))
                {
                    filteredPolicies.CategoryPolicies = policies.CategoryPolicies.FindAll(x => 
                        x.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    return Ok(policies);
                }
                
                if (filteredPolicies.CategoryPolicies == null || !filteredPolicies.CategoryPolicies.Any())
                {
                    // Search parameter value not found in list of category policies
                    return NotFound();
                }

                // Success; return the found list of category policies from filtered search
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
        [Authorize]
        public async Task<IActionResult> CreateUserClaimAsync([FromBody] CategoryPolicy categoryPolicy)
        {
            try
            {
                /* Validate whether authenticated user is samples administrator */

                ClaimsIdentity identity = (ClaimsIdentity)User.Identity;
                IEnumerable<Claim> claims = identity.Claims;
                string userPrincipalName =
                    (claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnJwt, StringComparison.OrdinalIgnoreCase)) ??
                        claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnUriSchema, StringComparison.OrdinalIgnoreCase)))?.Value;

                bool isAdmin = _administrators.Administrators.Contains(userPrincipalName);

                if (!isAdmin)
                {
                    return new JsonResult($"{userPrincipalName} is not authorized to create the user claim.")
                    { StatusCode = StatusCodes.Status403Forbidden };
                }

                // Get the list of policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();

                // Add the new user claim in the given category policy
                SampleQueriesPolicies updatedPoliciesList = SamplesPolicyService.ModifyUserClaim(policies, categoryPolicy);

                string updatedPoliciesJson = SamplesPolicyService.SerializeSampleQueriesPolicies(updatedPoliciesList);

                await _fileUtility.WriteToFile(updatedPoliciesJson, _policiesFilePathSource);

                // Extract the first user claim from the given categoryPolicy; this is what was added
                UserClaim userClaim = categoryPolicy.UserClaims.First();

                // Fetch the category policy with the newly created user claim
                categoryPolicy = updatedPoliciesList.CategoryPolicies.Find(x => x.CategoryName == categoryPolicy.CategoryName);

                // Create the query Uri for the newly created user claim
                string newUserClaimUri = string.Format("{0}://{1}{2}?userprincipalname={3}&categoryname={4}", 
                    Request.Scheme, Request.Host, Request.Path.Value, userClaim.UserPrincipalName, categoryPolicy.CategoryName);

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
        [Authorize]
        public async Task<IActionResult> UpdateUserClaimAsync([FromBody] CategoryPolicy categoryPolicy)
        {
            try
            {
                /* Validate whether authenticated user is samples administrator */

                ClaimsIdentity identity = (ClaimsIdentity)User.Identity;
                IEnumerable<Claim> claims = identity.Claims;
                string userPrincipalName =
                    (claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnJwt, StringComparison.OrdinalIgnoreCase)) ??
                        claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnUriSchema, StringComparison.OrdinalIgnoreCase)))?.Value;

                bool isAdmin = _administrators.Administrators.Contains(userPrincipalName);

                if (!isAdmin)
                {
                    return new JsonResult($"{userPrincipalName} is not authorized to update the user claim.")
                    { StatusCode = StatusCodes.Status403Forbidden };
                }

                // Get the list of policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();

                // Update the user claim in the given category policy
                SampleQueriesPolicies updatedPoliciesList = SamplesPolicyService.ModifyUserClaim(policies, categoryPolicy);

                string updatedPoliciesJson = SamplesPolicyService.SerializeSampleQueriesPolicies(updatedPoliciesList);

                await _fileUtility.WriteToFile(updatedPoliciesJson, _policiesFilePathSource);

                // Fetch the category policy with the updated user claim
                categoryPolicy = updatedPoliciesList.CategoryPolicies.Find(x => x.CategoryName == categoryPolicy.CategoryName);

                // Success; return the user claim in the category policy that was just updated
                return Ok(categoryPolicy);
            }
            catch (InvalidOperationException invalidOpsException)
            {
                //  Category policy provided doesn't exist
                return new JsonResult(invalidOpsException.Message) { StatusCode = StatusCodes.Status404NotFound };
            }
            catch(ArgumentNullException argNullException)
            {
                // Missing required parameter
                return new JsonResult(argNullException.Message) { StatusCode = StatusCodes.Status400BadRequest };
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
                
        // Removes a user claim from a category policy given a User Principal Name and a category policy name
        [Produces("application/json")]
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> RemoveUserClaim([FromQuery] string userPrincipalName, [FromQuery] string categoryName)
        {
            try
            {
                /* Validate whether authenticated user is samples administrator */

                ClaimsIdentity identity = (ClaimsIdentity)User.Identity;
                IEnumerable<Claim> claims = identity.Claims;
                string authenticatedUserPrincipalName =
                    (claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnJwt, StringComparison.OrdinalIgnoreCase)) ??
                        claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnUriSchema, StringComparison.OrdinalIgnoreCase)))?.Value;

                bool isAdmin = _administrators.Administrators.Contains(authenticatedUserPrincipalName);

                if (!isAdmin)
                {
                    return new JsonResult(
                       $"{userPrincipalName} is not authorized to remove the user claim.")
                    { StatusCode = StatusCodes.Status403Forbidden };
                }

                if (string.IsNullOrEmpty(userPrincipalName) || string.IsNullOrEmpty(categoryName))
                {
                    return new JsonResult($"Provide both the user principal name and category name in the search parameters. " +
                        $"e.g. /api/GraphExplorerSamplesAdmin?userprincipalname=john.doe@microsoft.com&categoryname=users")
                    { StatusCode = StatusCodes.Status400BadRequest };
                }

                // Get the list of policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();

                // Remove the user claim in the given category policy
                SampleQueriesPolicies updatedPoliciesList = SamplesPolicyService.RemoveUserClaim(policies, categoryName, userPrincipalName);

                string updatedPoliciesJson = SamplesPolicyService.SerializeSampleQueriesPolicies(updatedPoliciesList);

                await _fileUtility.WriteToFile(updatedPoliciesJson, _policiesFilePathSource);

                // Get the category policy that has just been updated
                CategoryPolicy categoryPolicy = updatedPoliciesList.CategoryPolicies.FirstOrDefault(x => 
                    x.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

                return Ok(categoryPolicy);
            }
            catch (InvalidOperationException invalidOpsException)
            {
                // One or more parameters values do not exist in the list of category policies
                return new JsonResult(invalidOpsException.Message) { StatusCode = StatusCodes.Status404NotFound };
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }            
        }

        /// <summary>
        /// Gets the JSON file contents of the policies and returns a deserialized instance of a <see cref="SampleQueriesPolicies"/> from this.
        /// </summary>
        /// <returns>A list of category policies.</returns>
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

                // Save the JSON string to the source file
                await _fileUtility.WriteToFile(policiesJson, _policiesFilePathSource);

                // Return the list of policies
                return policies;
            }

            // Return the list of policies
            return SamplesPolicyService.DeserializeSampleQueriesPolicies(jsonFileContents);
        }

        /// <summary>
        /// Gets the list of <see cref="CategoryPolicy"/> or a <see cref="CategoryPolicy"/> that a User Principal Name has claims in.
        /// </summary>
        /// <param name="policies">The list of <see cref="CategoryPolicy"/> to search in.</param>
        /// <param name="userPrincipalName">The target User Principal Name which to search for in the list of <see cref="CategoryPolicy"/>.</param>
        /// <param name="categoryName">The name of the target <see cref="CategoryPolicy"/> to be searched for in the list of <see cref="CategoryPolicy"/>. 
        /// If unspecified, all the category policies will be included in the search.</param>
        /// <returns>A list of filtered <see cref="CategoryPolicy"/> or the target <see cref="CategoryPolicy"/> with the claims for the specified User Principal Name.</returns>
        private static List<CategoryPolicy> GetUserPrincipalCategoryPolicies(SampleQueriesPolicies policies, string userPrincipalName, string categoryName = null)
        {
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            if (string.IsNullOrEmpty(categoryName))
            {
                // Search for all category policies that the specified user principal name has claims in
                categoryPolicies = policies.CategoryPolicies.FindAll(x => x.UserClaims.Exists(y => y.UserPrincipalName == userPrincipalName));
            }
            else
            {
                // Search for the category policy that the specified user principal name has claims in             
                CategoryPolicy categoryPolicy = policies.CategoryPolicies.Find
                 (x => x.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase) &&
                      x.UserClaims.Exists(y => y.UserPrincipalName.Equals(userPrincipalName, StringComparison.OrdinalIgnoreCase)));
 
                if (categoryPolicy == null)
                {
                    return null;
                }
                else
                {
                    categoryPolicies.Add(categoryPolicy);
                }
            }

            if (!categoryPolicies.Any())
            {
                return null;
            }

            List<CategoryPolicy> filteredCategoryPolicies = new List<CategoryPolicy>();

            // Filter each category policy to extract the specified user principal's claims
            foreach (CategoryPolicy categoryPolicy in categoryPolicies)
            {
                UserClaim userClaim = categoryPolicy.UserClaims.Find(x => x.UserPrincipalName.Equals(userPrincipalName, StringComparison.OrdinalIgnoreCase));

                CategoryPolicy userPrincipalPolicy = new CategoryPolicy
                {
                    CategoryName = categoryPolicy.CategoryName,
                    UserClaims = new List<UserClaim> { userClaim }
                };

                filteredCategoryPolicies.Add(userPrincipalPolicy);
            }

            return filteredCategoryPolicies;
        }
    }
}
