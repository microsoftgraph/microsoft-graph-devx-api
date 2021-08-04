// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using KnownIssuesService.Interfaces;
using KnownIssuesService.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UtilityService;

namespace GraphWebApi.Controllers
{
    [ApiController]
    public class KnownIssuesController : ControllerBase
    {
        private readonly IKnownIssuesService _knownIssuesService;
        private readonly TelemetryClient _telemetryClient;
        private readonly Dictionary<string, string> _knownIssuesTraceProperties =
            new() { { UtilityConstants.TelemetryPropertyKey_KnownIssues, nameof(KnownIssuesController) } };

        public KnownIssuesController(IKnownIssuesService knownIssuesService, TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
            UtilityFunctions.CheckArgumentNull(knownIssuesService, nameof(knownIssuesService));
            _knownIssuesService = knownIssuesService;
        }

        // GET: api/<KnownIssuesController>
        [Route("api/[controller]")]
        [Route("knownissues")]
        [HttpGet]
        public async Task<IActionResult> GetKnownIssues()
        {
            try
            {
                _telemetryClient?.TrackTrace("Request to query the list of known issues",
                                             SeverityLevel.Information,
                                             _knownIssuesTraceProperties);

                List<KnownIssue> result = await _knownIssuesService.QueryBugsAsync();
                return result == null ? NotFound() : Ok(result);
            }
            catch(Exception ex)
            {
                _telemetryClient?.TrackException(ex, _knownIssuesTraceProperties);
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };                
            }
        }
    }
}
