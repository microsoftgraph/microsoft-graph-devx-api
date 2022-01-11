// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using ChangesService.Common;
using ChangesService.Interfaces;
using ChangesService.Models;
using FileService.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UtilityService;

namespace GraphWebApi.Controllers
{
    [ApiController]
    [ExcludeFromCodeCoverage]
    public class ChangesController : ControllerBase
    {
        private readonly IChangesStore _changesStore;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly Dictionary<string, string> _changesTraceProperties =
            new() { { UtilityConstants.TelemetryPropertyKey_Changes, nameof(ChangesController) } };
        private readonly TelemetryClient _telemetryClient;
        private readonly IChangesService _changesService;

        public ChangesController(IChangesStore changesStore, IConfiguration configuration, IChangesService changesService,
                                 IHttpClientUtility httpClientUtility, TelemetryClient telemetryClient)
        {
            UtilityFunctions.CheckArgumentNull(telemetryClient, nameof(telemetryClient));
            UtilityFunctions.CheckArgumentNull(changesStore, nameof(changesStore));
            UtilityFunctions.CheckArgumentNull(configuration, nameof(configuration));
            UtilityFunctions.CheckArgumentNull(changesService, nameof(changesService));
            UtilityFunctions.CheckArgumentNull(httpClientUtility, nameof(httpClientUtility));
            _telemetryClient = telemetryClient;
            _changesStore = changesStore;
            _configuration = configuration;
            _httpClientUtility = httpClientUtility;
            _changesService = changesService;
        }

        // Gets the changelog records
        [Route("changes")]
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> GetChangesAsync(
                                         [FromQuery] string requestUrl,
                                         [FromQuery] string service,
                                         [FromQuery] double daysRange,
                                         [FromQuery] DateTime? startDate, // yyyy-MM-ddTHH:mm:ss
                                         [FromQuery] DateTime? endDate, // yyyy-MM-ddTHH:mm:ss
                                         [FromQuery] int skip, // Items to skip
                                         [FromQuery] int top = 500, // Max page items
                                         [FromQuery] string graphVersion = "v1.0")
        {
            // Options for searching, filtering and paging the changelog data
            var searchOptions = new ChangeLogSearchOptions(requestUrl: requestUrl.BaseUriPath(),
                                                           service: service,
                                                           daysRange: daysRange,
                                                           startDate: startDate,
                                                           endDate: endDate,
                                                           graphVersion: graphVersion)
            {
                Top = top,
                Skip = skip
            };

            // Get the requested culture info.
            var cultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var cultureInfo = cultureFeature.RequestCulture.Culture;

            _telemetryClient?.TrackTrace($"Request to fetch changelog records for the requested culture info '{cultureInfo}'",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);
            // Fetch the changelog records
            var changeLog = await _changesStore.FetchChangeLogRecordsAsync(cultureInfo);

            // Filter the changelog records
            if (changeLog.ChangeLogs.Any())
            {
                // Configs for fetching workload names from given requestUrl
                var graphProxyConfigs = new MicrosoftGraphProxyConfigs()
                {
                    GraphProxyBaseUrl = _configuration[ChangesServiceConstants.GraphProxyBaseUrlConfigPath],
                    GraphProxyRelativeUrl = _configuration[ChangesServiceConstants.GraphProxyRelativeUrlConfigPath],
                    GraphProxyAuthorization = _configuration[ChangesServiceConstants.GraphProxyAuthorization],
                    GraphVersion = graphVersion
                };

                var workloadServiceMappings = await _changesStore.FetchWorkloadServiceMappingsAsync();
                changeLog = _changesService.FilterChangeLogRecords(changeLog, searchOptions, graphProxyConfigs, workloadServiceMappings, _httpClientUtility);
            }
            else
            {
                // No records
                return NoContent();
            }

            if (!changeLog.ChangeLogs.Any())
            {
                _telemetryClient?.TrackTrace($"Search options not found in: requestUrl, workload, daysRange, startDate, endDate properties of changelog records",
                                                SeverityLevel.Error,
                                                _changesTraceProperties);
                // Filtered items yielded no result
                return NotFound();
            }
            _changesTraceProperties.Add(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(ChangesController));
            _telemetryClient?.TrackTrace($"Fetched {changeLog.CurrentItems} changes",
                                            SeverityLevel.Information,
                                            _changesTraceProperties);
            return Ok(changeLog);
        }
    }
}
