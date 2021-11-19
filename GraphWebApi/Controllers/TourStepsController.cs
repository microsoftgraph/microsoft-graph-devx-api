// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using GraphWebApi.Common;
using Microsoft.ApplicationInsights.DataContracts;
using UtilityService;
using TourStepsService.Interfaces;
using TourStepsService.Models;
using Microsoft.ApplicationInsights;

namespace GraphWebApi.Controllers
{
    [ApiController]
    public class TourStepsController : ControllerBase
    {
        private readonly ITourStepsStore _tourStepsStore;
        private readonly Dictionary<string, string> _tourStepsTraceProperties =
            new() { { UtilityConstants.TelemetryPropertyKey_TourSteps, nameof(TourStepsController)} };
        private readonly TelemetryClient _telemetryClient;
        public TourStepsController(ITourStepsStore tourStepsStore, TelemetryClient telemetryClient)
        {
            UtilityFunctions.CheckArgumentNull(tourStepsStore, nameof(tourStepsStore));
            UtilityFunctions.CheckArgumentNull(telemetryClient, nameof(telemetryClient));
            _tourStepsStore = tourStepsStore;
            _telemetryClient = telemetryClient;
        }

        // Gets list of all steps
        [Route("toursteps")]
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> GetTourStepsListAsync(string org = default, string branchName = default)
        {
            var tourStepsList = await FetchTourStepsListAsync(org, branchName);

            if (tourStepsList == null || !tourStepsList.TourSteps.Any())
            {
                return NoContent();
            }
            return Ok(tourStepsList);
        }

        private async Task<TourStepsList> FetchTourStepsListAsync(string org, string branchName)
        {
            string locale = RequestHelper.GetPreferredLocaleLanguage(Request) ?? Constants.DefaultLocale;
            _telemetryClient?.TrackTrace($"Request to fetch steps for locale '{locale}'",
                                        SeverityLevel.Information,
                                        _tourStepsTraceProperties);

            TourStepsList tourStepsList;

            if(!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(branchName))
            {
                tourStepsList = await _tourStepsStore.FetchTourStepsListAsync(locale, org, branchName);
            }
            else
            {
                tourStepsList = await _tourStepsStore.FetchTourStepsListAsync(locale);
            }

            _tourStepsTraceProperties.Add(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(TourStepsController));
            _telemetryClient?.TrackTrace($"Fetched {tourStepsList?.TourSteps?.Count ?? 0} steps",
                                         SeverityLevel.Information,
                                         _tourStepsTraceProperties);
            return tourStepsList;
        }
    }
}
