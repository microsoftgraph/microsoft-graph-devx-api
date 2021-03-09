// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GraphExplorerPermissionsService.Interfaces;
using GraphExplorerPermissionsService.Models;
using GraphWebApi.Common;
using GraphWebApi.Telemetry.Interfaces;
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
        private readonly ITelemetryHelper _telemetryHelper;

        public GraphExplorerPermissionsController(IPermissionsStore permissionsStore, ITelemetryHelper telemetryHelper)
        {
            _permissionsStore = permissionsStore;
            _telemetryHelper = telemetryHelper;
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
            Stopwatch stopwatch = _telemetryHelper.BeginRequest();

            try
            {
                string localeCode = RequestHelper.GetPreferredLocaleLanguage(Request);

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

                return result == null ? NotFound() : (IActionResult)Ok(result);
            }
            catch (InvalidOperationException invalidOpsException)
            {
                return new JsonResult(invalidOpsException.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
            catch (ArgumentNullException argNullException)
            {
                return new JsonResult(argNullException.Message) { StatusCode = StatusCodes.Status400BadRequest };
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
            finally
            {
                var eventName = "GET_PERMISSION_SCOPES_EVENT";

                // send the event to AppInsights
                _telemetryHelper.EndRequest(stopwatch, HttpContext, eventName);
            }
        }
    }
}