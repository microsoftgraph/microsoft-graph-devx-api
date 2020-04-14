// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using GraphExplorerPermissionsService.Interfaces;
using GraphExplorerPermissionsService.Models;
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

        public GraphExplorerPermissionsController(IPermissionsStore permissionsStore)
        {
            _permissionsStore = permissionsStore;
        }

        // Gets the permission scopes and info for a request url
        [HttpGet]
        [Produces("application/json")]
        public IActionResult GetPermissionScopes([FromQuery]string requestUrl = null, [FromQuery]string method = "GET", [FromQuery]string scopeType = "DelegatedWork")
        {
            try
            {
                List<ScopeInformation> result = null;
                if (string.IsNullOrEmpty(requestUrl))
                    result = _permissionsStore.GetScopes();
                else
                    result = _permissionsStore.GetScopes(requestUrl, method, scopeType);


                if (result == null)
                {
                    return NotFound();
                }

                return Ok(result);
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
