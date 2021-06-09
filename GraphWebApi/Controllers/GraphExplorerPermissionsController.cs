// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphExplorerPermissionsService.Interfaces;
using GraphExplorerPermissionsService.Models;
using GraphWebApi.Common;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [Route("permissions")]
    [ApiController]
    public class GraphExplorerPermissionsController : ControllerBase
    {
        private readonly IPermissionsStore _permissionsStore;
        private readonly TelemetryClient _telemetry;
        private Dictionary<string, string> _permissionsTraceProperties = new Dictionary<string, string> { { "Permissions", "PermissionsController" } };

        public GraphExplorerPermissionsController(IPermissionsStore permissionsStore, TelemetryClient telemetry)
        {
            _permissionsStore = permissionsStore;
            _telemetry = telemetry;
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
            try
            {
                string localeCode = RequestHelper.GetPreferredLocaleLanguage(Request) ?? Constants.DefaultLocale;
                _telemetry?.TrackTrace($"Request to fetch permissions for locale '{localeCode}'",
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

                _permissionsTraceProperties.Add("Count", "PermissionsCount");
                _telemetry?.TrackTrace($"Fetched {result.Count} permissions",
                                      SeverityLevel.Information,
                                      _permissionsTraceProperties);

                return result == null ? NotFound() : Ok(result);
            }
            catch (ArgumentNullException argNullException)
            {
                _telemetry?.TrackException(argNullException,
                                          _permissionsTraceProperties);
                return new JsonResult(argNullException.Message) { StatusCode = StatusCodes.Status400BadRequest };
            }
            catch (Exception exception)
            {
                // Any 'InvalidOperationException' will also be caught here - these are classified as error 500

                _telemetry?.TrackException(exception,
                                          _permissionsTraceProperties);
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}