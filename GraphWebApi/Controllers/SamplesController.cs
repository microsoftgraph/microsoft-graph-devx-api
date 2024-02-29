// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using GraphWebApi.Common;
using Microsoft.ApplicationInsights.DataContracts;
using UtilityService;
using Microsoft.ApplicationInsights;
using System.Diagnostics.CodeAnalysis;
using SamplesService.Interfaces;
using SamplesService.Models;
using FileService.Extensions;
using System.Reflection.Metadata;

namespace GraphWebApi.Controllers
{
    [ApiController]
    [ExcludeFromCodeCoverage]
    public class SamplesController : ControllerBase
    {
        private readonly ISamplesStore _samplesStore;
        private readonly Dictionary<string, string> _samplesTraceProperties =
            new() { { UtilityConstants.TelemetryPropertyKey_Samples, nameof(SamplesController)} };
        private readonly TelemetryClient _telemetryClient;

        public SamplesController(ISamplesStore samplesStore, TelemetryClient telemetryClient)
        {
            UtilityFunctions.CheckArgumentNull(telemetryClient, nameof(telemetryClient));
            UtilityFunctions.CheckArgumentNull(samplesStore, nameof(samplesStore));
            _telemetryClient = telemetryClient;
            _samplesStore = samplesStore;
        }

        // Gets the list of all sample queries
        [Route("api/[controller]")]
        [Route("api/graphexplorersamples")]
        [Route("samples")]
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> GetSampleQueriesListAsync(string search, string org, string branchName)
        {
            SampleQueriesList sampleQueriesList = await FetchSampleQueriesListAsync(org, branchName);
            Validate(sampleQueriesList);

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
                _telemetryClient?.TrackTrace($"Search value: '{search}' not found in: category, humanName or tip properties of sample queries",
                                                SeverityLevel.Error,
                                                _samplesTraceProperties);
                return NotFound();
            }

            _samplesTraceProperties.Add(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(SamplesController));
            _telemetryClient?.TrackTrace($"{filteredSampleQueries?.Count ?? 0} sample queries found from search value '{search}'",
                                            SeverityLevel.Information,
                                            _samplesTraceProperties);

            return Ok(filteredSampleQueries);
        }

       // Gets a sample query from the list of sample queries by its id
       [Route("api/[controller]/{id}")]
       [Route("api/graphexplorersamples/{id}")]
       [Route("samples/{id}")]
       [Produces("application/json")]
       [HttpGet]
        public async Task<IActionResult> GetSampleQueryByIdAsync(string id, string org, string branchName)
        {
            SampleQueriesList sampleQueriesList = await FetchSampleQueriesListAsync(org, branchName);
            Validate(sampleQueriesList);

            if (!string.IsNullOrEmpty(id))
            {
                // Search for sample query with the provided id
                var guidId = Guid.Parse(id);
                SampleQueryModel sampleQueryById = sampleQueriesList.SampleQueries.Find(x => x.Id == guidId);

                if (sampleQueryById == null)
                {
                    _telemetryClient?.TrackTrace($"Sample query with id: {id} doesn't exist in the list of sample queries",
                                                    SeverityLevel.Error,
                                                    _samplesTraceProperties);
                    return NotFound();
                }

                // Success; return the found sample query
                return Ok(sampleQueryById);
            }

            return Ok(sampleQueriesList);
        }

        /// <summary>
        /// Fetches a list of Sample Queries from Github or blob storage
        /// </summary>
        /// <param name="org">The name of the organisation i.e microsoftgraph or a member's username in the case of a forked repo.</param>
        /// <param name="branchName">The name of the branch.</param>
        /// <returns>A list of Sample Queries.</returns>
        private async Task<SampleQueriesList> FetchSampleQueriesListAsync(string org, string branchName)
        {
            string locale = RequestHelper.GetPreferredLocaleLanguage(Request) ?? Constants.DefaultLocale;
            locale = LocalizationExtensions.GetSupportedLocaleVariant(locale);

            var supportedLocale = LocalizationExtensions.GetSupportedLocaleVariant(locale);
            if (locale != supportedLocale)
            {
                _telemetryClient?.TrackTrace($"Requested locale variant '{locale}' not supported; using '{supportedLocale}'",
                                            SeverityLevel.Information,
                                            _samplesTraceProperties);
                locale = supportedLocale;
            }

            _telemetryClient?.TrackTrace($"Request to fetch samples for locale '{locale}'",
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

            _samplesTraceProperties.Add(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(SamplesController));
            _telemetryClient?.TrackTrace($"Fetched {sampleQueriesList?.SampleQueries?.Count ?? 0} samples",
                                         SeverityLevel.Information,
                                         _samplesTraceProperties);

            return sampleQueriesList;
        }

        /// <summary>
        /// Checks whether the SampleQueriesList is empty and returns status code 204.
        /// </summary>
        /// <param name="sampleQueriesList"></param>
        /// <returns>Status code response.</returns>
        private IActionResult Validate(SampleQueriesList sampleQueriesList)
        {
            if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
            {
                return NoContent(); // list is empty, just return status code 204 - No Content
            }

            return Ok(sampleQueriesList);
        }
    }
}
