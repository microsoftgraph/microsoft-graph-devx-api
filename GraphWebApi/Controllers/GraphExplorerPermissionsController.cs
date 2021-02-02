// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FileService.Interfaces;
using GraphExplorerPermissionsService;
using GraphExplorerPermissionsService.Interfaces;
using GraphExplorerPermissionsService.Models;
using GraphWebApi.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [Route("permissions")]
    [ApiController]
    public class GraphExplorerPermissionsController : ControllerBase
    {
        private readonly IPermissionsStore _permissionsStore;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientUtility _httpClientUtility;

        public GraphExplorerPermissionsController(IPermissionsStore permissionsStore, IConfiguration configuration, IHttpClientUtility httpClientUtility)
        {
            _permissionsStore = permissionsStore;
            _configuration = configuration;
            _httpClientUtility = httpClientUtility;
        }

        // Gets the permissions scopes
        [HttpGet]
        [Produces("application/json")]
        public async Task<IActionResult> GetPermissionScopes([FromQuery]string scopeType = "DelegatedWork",
                                                             [FromQuery]string requestUrl = null,
                                                             [FromQuery]string method = null,
                                                             [FromQuery] string org = null,
                                                             [FromQuery] string branchName = null)
        {
            try
            {
                string localeCode = RequestHelper.GetPreferredLocaleLanguage(Request);

                List<ScopeInformation> result = null;

                if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(branchName))
                {
                    var permissionsStore = new PermissionsStore(configuration: _configuration,
                        httpClientUtility: _httpClientUtility);

                    // Fetch permissions descriptions file from Github
                    result = await _permissionsStore.GetScopesAsync(scopeType: scopeType, locale: localeCode, requestUrl: requestUrl,
                        method: method, org: org, branchName: branchName);
                }
                else
                {
                    // Fetch the files from Azure Blob
                    result = await _permissionsStore.GetScopesAsync(scopeType: scopeType, locale: localeCode, requestUrl: requestUrl, method: method);
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
        }
    }
}