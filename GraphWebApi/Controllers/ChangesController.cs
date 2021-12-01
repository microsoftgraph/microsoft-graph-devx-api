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
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Configuration;
using UtilityService;
using HttpGetAttribute = Microsoft.AspNetCore.Mvc.HttpGetAttribute;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

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
        [EnableQuery]
        [HttpGet]
        public async Task<IActionResult> GetChangesAsync([FromQuery] string requestUrl = null)
        {
            // Get the requested culture info.
            var cultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var cultureInfo = cultureFeature.RequestCulture.Culture;

            _telemetryClient?.TrackTrace($"Request to fetch changelog records for the requested culture info '{cultureInfo}'",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);
            // Fetch the changelog records
            var changeLog = await _changesStore.FetchChangeLogRecordsAsync(cultureInfo);

            // Filter the changelog records
            if (!changeLog.ChangeLogs.Any())
            {
                // No records
                return NoContent();
            }

            if (!string.IsNullOrEmpty(requestUrl))
            {
                //string graphVersion;
                //var urlSegments = requestUrl.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                //switch (urlSegments.FirstOrDefault())
                //{
                //    case ChangesServiceConstants.GraphVersion_Beta:
                //        requestUrl = requestUrl.Replace(ChangesServiceConstants.GraphVersion_Beta, string.Empty);
                //        graphVersion = ChangesServiceConstants.GraphVersion_Beta;
                //        break;
                //    default:
                //        requestUrl = requestUrl.Replace(ChangesServiceConstants.GraphVersion_V1, string.Empty);
                //        graphVersion = ChangesServiceConstants.GraphVersion_V1;
                //        break;
                //}

                //if (!requestUrl.StartsWith('/'))
                //{
                //    requestUrl = $"/{requestUrl}";
                //}

                (var url, var graphVersion) = _changesService.ExtractGraphVersionAndUrlValues(requestUrl);

                // Configs for fetching workload names from given requestUrl
                var graphProxyConfigs = new MicrosoftGraphProxyConfigs()
                {
                    GraphProxyBaseUrl = _configuration[ChangesServiceConstants.GraphProxyBaseUrlConfigPath],
                    GraphProxyRelativeUrl = _configuration[ChangesServiceConstants.GraphProxyRelativeUrlConfigPath],
                    GraphProxyAuthorization = _configuration[ChangesServiceConstants.GraphProxyAuthorization],
                    GraphVersion = graphVersion
                };

                var workloadServiceMappings = await _changesStore.FetchWorkloadServiceMappingsAsync();
                changeLog = await _changesService.FilterChangeLogRecordsByUrlAsync(requestUrl, changeLog, graphProxyConfigs, workloadServiceMappings, _httpClientUtility);
            }


            _changesTraceProperties.Add(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(ChangesController));
            _telemetryClient?.TrackTrace($"Fetched {changeLog.CurrentItems} changes",
                                            SeverityLevel.Information,
                                            _changesTraceProperties);
            return Ok(changeLog.ChangeLogs);
        }
    }
}
