// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using GraphExplorerPermissionsService.Interfaces;
using GraphExplorerPermissionsService.Models;
using GraphWebApi.Common;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> GetPermissionScopes([FromQuery]string scopeType = "DelegatedWork",
                                                             [FromQuery]string requestUrl = null,
                                                             [FromQuery]string method = null,
                                                             [FromQuery]string org = null,
                                                             [FromQuery]string branchName = null)
        {
            string localeCode = RequestHelper.GetPreferredLocaleLanguage(Request) ?? Constants.DefaultLocale;
            _telemetryClient?.TrackTrace($"Request to fetch permissions for locale '{localeCode}'",
                                            SeverityLevel.Information,
                                            _permissionsTraceProperties);

            List<ScopeInformation> result = null;

            if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(branchName))
            {
                // Fetch permissions descriptions file from Github
                result = await _permissionsStore.GetScopesAsync(scopeType: scopeType,
                                                                locale: localeCode,
                                                                requestUrl: requestUrl,
                                                                method: method,
                                                                org: org,
                                                                branchName: branchName);
            }
            else
            {
                // Fetch the files from Azure Blob
                result = await _permissionsStore.GetScopesAsync(scopeType: scopeType,
                                                                locale: localeCode,
                                                                requestUrl: requestUrl,
                                                                method: method);
            }

            _permissionsTraceProperties.Add(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(PermissionsController));
            _telemetryClient?.TrackTrace($"Fetched {result?.Count ?? 0} permissions",
                                            SeverityLevel.Information,
                                            _permissionsTraceProperties);

            return result == null ? NotFound() : Ok(result);
        }
    }
}
