// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FileService.Extensions;
using GraphWebApi.Common;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PermissionsService.Interfaces;
using PermissionsService.Models;
using UtilityService;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [Route("api/graphexplorerpermissions")]
    [Route("permissions")]
    [ApiController]
    [ExcludeFromCodeCoverage]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionsStore _permissionsStore;
        private readonly Dictionary<string, string> _permissionsTraceProperties =
            new() { { UtilityConstants.TelemetryPropertyKey_Permissions, nameof(PermissionsController) } };
        private readonly TelemetryClient _telemetryClient;

        public PermissionsController(IPermissionsStore permissionsStore, TelemetryClient telemetryClient)
        {
            UtilityFunctions.CheckArgumentNull(telemetryClient, nameof(telemetryClient));
            UtilityFunctions.CheckArgumentNull(permissionsStore, nameof(permissionsStore));
            _telemetryClient = telemetryClient;
            _permissionsStore = permissionsStore;
        }

        // Gets the permissions scopes
        [HttpGet]
        [Produces("application/json")]
        public async Task<IActionResult> GetPermissionScopesAsync([FromQuery]ScopeType? scopeType = null,
                                                             [FromQuery]string requestUrl = null,
                                                             [FromQuery]string method = null,
                                                             [FromQuery]string org = null,
                                                             [FromQuery]string branchName = null,
                                                             [FromQuery]bool includeHidden = false,
                                                             [FromQuery]bool leastPrivilegeOnly = false)
        {
            if (!string.IsNullOrEmpty(requestUrl) && string.IsNullOrEmpty(method))
                return BadRequest("The HTTP method value cannot be null or empty.");

            string localeCode = GetPreferredLocaleLanguage(Request);

            var requests = requestUrl != null
                ? new List<RequestInfo> { new RequestInfo { RequestUrl = requestUrl, HttpMethod = method } }
                : null;

            PermissionResult result = await _permissionsStore.GetScopesAsync(
                                                                requests: requests,
                                                                locale: localeCode,
                                                                scopeType: scopeType,
                                                                includeHidden: includeHidden,
                                                                leastPrivilegeOnly: leastPrivilegeOnly,
                                                                org: org,
                                                                branchName: branchName);

            _permissionsTraceProperties.Add(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(PermissionsController));
            _telemetryClient?.TrackTrace($"Fetched {result?.Results?.Count ?? 0} permissions",
                                            SeverityLevel.Information,
                                            _permissionsTraceProperties);

            return result?.Results == null || result.Results.Count == 0 ? NotFound() : Ok(result.Results);
        }

        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> GetPermissionScopesAsync([FromBody] List<RequestInfo> requests,
                                                             [FromQuery] ScopeType? scopeType = null,
                                                             [FromQuery] string org = null,
                                                             [FromQuery] string branchName = null,
                                                             [FromQuery] bool leastPrivilegeOnly = true,
                                                             [FromQuery] bool includeHidden = false)
        {
            if (requests == null || requests.Count == 0)
                return BadRequest("Request URLs cannot be null or empty");

            string localeCode = GetPreferredLocaleLanguage(Request);

            PermissionResult result = await _permissionsStore.GetScopesAsync(
                                                                requests: requests,
                                                                locale: localeCode,
                                                                scopeType: scopeType,
                                                                includeHidden: includeHidden,
                                                                leastPrivilegeOnly: leastPrivilegeOnly,
                                                                org: org,
                                                                branchName: branchName);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        private string GetPreferredLocaleLanguage(HttpRequest request)
        {
            string localeCode = RequestHelper.GetPreferredLocaleLanguage(request) ?? Constants.DefaultLocale;
            _telemetryClient?.TrackTrace($"Request to fetch permissions for locale '{localeCode}'",
                                            SeverityLevel.Information,
                                            _permissionsTraceProperties);

            var supportedLocaleCode = LocalizationExtensions.GetSupportedLocaleVariant(localeCode);
            if (localeCode != supportedLocaleCode)
            {
                _telemetryClient?.TrackTrace($"Requested locale variant '{localeCode}' not supported; using '{supportedLocaleCode}'",
                                            SeverityLevel.Information,
                                            _permissionsTraceProperties);
                localeCode = supportedLocaleCode;
            }
            return localeCode;
        }
    }
}
