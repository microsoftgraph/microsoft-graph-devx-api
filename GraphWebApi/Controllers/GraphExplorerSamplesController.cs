// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using GraphExplorerSamplesService.Services;
using GraphExplorerSamplesService.Models;
using System.Security.Claims;
using System.Linq;
using GraphWebApi.Common;
using GraphExplorerSamplesService.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using UtilityService;

namespace GraphWebApi.Controllers
{
    [ApiController]
    public class GraphExplorerSamplesController : ControllerBase
    {
        private readonly ISamplesStore _samplesStore;
        private readonly TelemetryClient _telemetry;
        private readonly Dictionary<string, string> _samplesTraceProperties =
            new() { { UtilityConstants.TelemetryPropertyKey_Samples, "SamplesController" } };

        public GraphExplorerSamplesController(ISamplesStore samplesStore, TelemetryClient telemetry)
        {
            _samplesStore = samplesStore;
            _telemetry = telemetry;
        }

        // Gets the list of all sample queries
        [Route("api/[controller]")]
        [Route("samples")]
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> GetSampleQueriesListAsync(string search, string org, string branchName)
        {
            try
            {
                SampleQueriesList sampleQueriesList = await FetchSampleQueriesListAsync(org, branchName);

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
                    _telemetry?.TrackTrace($"Search value: '{search}' not found in: category, humanName or tip properties of sample queries",
                                          SeverityLevel.Error,
                                          _samplesTraceProperties);
                    return NotFound();
                }

                _samplesTraceProperties.Add(UtilityConstants.TelemetryPropertyKey_Count, "SamplesCount");
                _telemetry?.TrackTrace($"{filteredSampleQueries.Count} sample queries found from search value '{search}'",
                                      SeverityLevel.Information,
                                      _samplesTraceProperties);
                return Ok(filteredSampleQueries);
            }
            catch (Exception exception)
            {
                _telemetry?.TrackException(exception,
                                          _samplesTraceProperties);
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

       // Gets a sample query from the list of sample queries by its id
       [Route("api/[controller]/{id}")]
       [Route("samples/{id}")]
       [Produces("application/json")]
       [HttpGet]
        public async Task<IActionResult> GetSampleQueryByIdAsync(string id, string org, string branchName)
        {
            try
            {
                SampleQueriesList sampleQueriesList = await FetchSampleQueriesListAsync(org, branchName);

                if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
                {
                    return NoContent(); // list is empty, just return status code 204 - No Content
                }

                // Search for sample query with the provided id
                SampleQueryModel sampleQueryById = sampleQueriesList.SampleQueries.Find(x => x.Id == Guid.Parse(id));

                if (sampleQueryById == null)
                {
                    _telemetry?.TrackTrace($"Sample query with id: {id} doesn't exist in the list of sample queries",
                                          SeverityLevel.Error,
                                          _samplesTraceProperties);
                    return NotFound();
                }

                // Success; return the found sample query
                return Ok(sampleQueryById);
            }
            catch (Exception exception)
            {
                _telemetry?.TrackException(exception,
                                          _samplesTraceProperties);
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Updates a sample query given its id value
        [Route("api/[controller]/{id}")]
        [Route("samples/{id}")]
        [Produces("application/json")]
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateSampleQueryAsync(string id, [FromBody]SampleQueryModel sampleQueryModel)
        {
            try
            {
                // Get the list of policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();

                string categoryName = sampleQueryModel.Category;

                ClaimsIdentity identity = (ClaimsIdentity)User.Identity;
                IEnumerable<Claim> claims = identity.Claims;
                string userPrincipalName =
                    (claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnJwt, StringComparison.OrdinalIgnoreCase)) ??
                        claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnUriSchema, StringComparison.OrdinalIgnoreCase)))?.Value;

                // Check if authenticated user is authorized for this action
                bool isAuthorized = SamplesPolicyService.IsUserAuthorized(policies, userPrincipalName, categoryName, HttpMethods.Put);

                if (!isAuthorized)
                {
                    return new JsonResult(
                        $"{userPrincipalName} is not authorized to update the sample query. Category: '{categoryName}'")
                    { StatusCode = StatusCodes.Status403Forbidden };
                }

                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync("en-US");

                if (sampleQueriesList.SampleQueries.Count == 0)
                {
                    return NotFound(); // List is empty; the sample query being searched is definitely not in an empty list
                }

                // Check if the sample query model exists in the list of sample queries
                bool sampleQueryExists = sampleQueriesList.SampleQueries.Exists(x => x.Id == Guid.Parse(id));

                if (!sampleQueryExists)
                {
                    throw new InvalidOperationException($"No sample query found with id: {id}");
                }

                // Update the provided sample query model into the list of sample queries
                SampleQueriesList updatedSampleQueriesList = SamplesService.UpdateSampleQueriesList(sampleQueriesList, sampleQueryModel, Guid.Parse(id));

                // Get the serialized JSON string of this sample query
                string updatedSampleQueriesJson = SamplesService.SerializeSampleQueriesList(updatedSampleQueriesList);

                // Success; return the sample query model object that was just updated
                return Ok(sampleQueryModel);
            }
            catch (InvalidOperationException invalidOpsException)
            {
                _telemetry.TrackException(invalidOpsException,
                                         _samplesTraceProperties);
                // sample query with provided id not found
                return new JsonResult(invalidOpsException.Message) { StatusCode = StatusCodes.Status404NotFound };
            }
            catch (Exception exception)
            {
                _telemetry.TrackException(exception,
                                         _samplesTraceProperties);
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Adds a new sample query to the list of sample queries
        [Route("api/[controller]")]
        [Route("samples")]
        [Produces("application/json")]
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateSampleQueryAsync([FromBody]SampleQueryModel sampleQueryModel)
        {
            try
            {
                // Get the list of policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();

                string categoryName = sampleQueryModel.Category;

                ClaimsIdentity identity = (ClaimsIdentity)User.Identity;
                IEnumerable<Claim> claims = identity.Claims;
                string userPrincipalName =
                    (claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnJwt, StringComparison.OrdinalIgnoreCase)) ??
                        claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnUriSchema, StringComparison.OrdinalIgnoreCase)))?.Value;

                // Check if authenticated user is authorized for this action
                bool isAuthorized = SamplesPolicyService.IsUserAuthorized(policies, userPrincipalName, categoryName, HttpMethods.Post);

                if(!isAuthorized)
                {
                    return new JsonResult(
                        $"{userPrincipalName} is not authorized to create the sample query. Category: '{categoryName}'")
                        { StatusCode = StatusCodes.Status403Forbidden };
                }

                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync("en-US");

                // Assign a new Id to the new sample query
                sampleQueryModel.Id = Guid.NewGuid();

                // Add the new sample query to the list of sample queries
                SampleQueriesList newSampleQueriesList = SamplesService.AddToSampleQueriesList(sampleQueriesList, sampleQueryModel);

                // Get the serialized JSON string of the sample query
                string newSampleQueriesJson = SamplesService.SerializeSampleQueriesList(newSampleQueriesList);

                // Disabled functionality
                // await _fileUtility.WriteToFile(updatedSampleQueriesJson, _queriesFilePathSource);

                // Create the query Uri for the newly created sample query
                string newSampleQueryUri = string.Format("{0}://{1}{2}/{3}", Request.Scheme, Request.Host, Request.Path.Value, sampleQueryModel.Id.ToString());

                // Success; return the new sample query that was added along with its Uri
                return Created(newSampleQueryUri, sampleQueryModel);
            }
            catch (Exception exception)
            {
                _telemetry.TrackException(exception,
                                         _samplesTraceProperties);
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        // Deletes a sample query of the provided id from the list of sample queries
        [Route("api/[controller]/{id}")]
        [Route("samples/{id}")]
        [Produces("application/json")]
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteSampleQueryAsync(string id)
        {
            try
            {
                // Get the list of sample queries
                SampleQueriesList sampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync("en-US");

                // Get the list of policies
                SampleQueriesPolicies policies = await GetSampleQueriesPoliciesAsync();

                // Check if the sample query model exists in the list of sample queries
                bool sampleQueryExists = sampleQueriesList.SampleQueries.Exists(x => x.Id == Guid.Parse(id));

                if (!sampleQueryExists)
                {
                    throw new InvalidOperationException($"No sample query found with id: {id}");
                }

                string categoryName = sampleQueriesList.SampleQueries.Find(x => x.Id == Guid.Parse(id)).Category;

                ClaimsIdentity identity = (ClaimsIdentity)User.Identity;
                IEnumerable<Claim> claims = identity.Claims;
                string userPrincipalName =
                   (claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnJwt, StringComparison.OrdinalIgnoreCase)) ??
                        claims?.FirstOrDefault(x => x.Type.Equals(Constants.ClaimTypes.UpnUriSchema, StringComparison.OrdinalIgnoreCase)))?.Value;

                // Check if authenticated user is authorized for this action
                bool isAuthorized = SamplesPolicyService.IsUserAuthorized(policies, userPrincipalName, categoryName, HttpMethods.Delete);

                if (!isAuthorized)
                {
                    return new JsonResult(
                        $"{userPrincipalName} is not authorized to delete the sample query. Category: '{categoryName}'")
                    { StatusCode = StatusCodes.Status403Forbidden };
                }

                if (sampleQueriesList.SampleQueries.Count == 0)
                {
                    return NotFound(); // list is empty; the sample query being searched is definitely not in an empty list
                }

                // Remove the sample query with given id from the list of sample queries
                sampleQueriesList = SamplesService.RemoveSampleQuery(sampleQueriesList, Guid.Parse(id));

                // Get the serialized JSON string of the list of sample queries
                string newSampleQueriesJson = SamplesService.SerializeSampleQueriesList(sampleQueriesList);

                // Disabled functionality
                // await _fileUtility.WriteToFile(updatedSampleQueriesJson, _queriesFilePathSource);

                // Success; no content to return
                return new JsonResult("Deleted successfully.") { StatusCode = StatusCodes.Status204NoContent};
            }
            catch (InvalidOperationException invalidOpsException)
            {
                _telemetry.TrackException(invalidOpsException,
                                        _samplesTraceProperties);
                // Sample query with provided id not found
                return new JsonResult(invalidOpsException.Message) { StatusCode = StatusCodes.Status404NotFound };
            }
            catch (Exception exception)
            {
                _telemetry.TrackException(exception,
                                          _samplesTraceProperties);
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        /// <summary>
        /// Fetches a list of Sample Queries from Github or blob storage
        /// </summary>
        /// <param name="org">The name of the organisation i.e microsoftgraph or a member's username in the case of a forked repo.</param>
        /// <param name="branchName">The name of the branch.</param>
        /// <returns>A list of Sample Queries.</returns>
        private async Task<SampleQueriesList> FetchSampleQueriesListAsync(string org, string branchName)
        {
            string locale = RequestHelper.GetPreferredLocaleLanguage(Request);
            _telemetry?.TrackTrace($"Request to fetch samples for locale '{locale}'",
                                  SeverityLevel.Information,
                                  _samplesTraceProperties);

            SampleQueriesList sampleQueriesList;
            if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(branchName))
            {
                sampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync(locale, org, branchName);
            }
            else
            {
                sampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync(locale);
            }

            _samplesTraceProperties.Add("Count", "SamplesCount");
            _telemetry?.TrackTrace($"Fetched {sampleQueriesList?.SampleQueries.Count} samples",
                                  SeverityLevel.Information,
                                  _samplesTraceProperties);

            return sampleQueriesList;
        }

        /// <summary>
        /// Gets the JSON file contents of the policies and returns a deserialized instance of a
        /// <see cref="SampleQueriesPolicies"/> from this.
        /// </summary>
        /// <returns></returns>
        private Task<SampleQueriesPolicies> GetSampleQueriesPoliciesAsync()
        {
            throw new NotImplementedException();
        }
    }
}