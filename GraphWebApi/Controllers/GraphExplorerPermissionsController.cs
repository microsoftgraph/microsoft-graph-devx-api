using System;
using GraphExplorerPermissionsService.Interfaces;
using GraphWebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GraphWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphExplorerPermissionsController : ControllerBase
    {
        private readonly IPermissionsStore _permissionsStore;

        public GraphExplorerPermissionsController(IPermissionsStore permissionsStore)
        {
            _permissionsStore = permissionsStore;
        }

        // Gets the permission scopes for a request url
        [HttpGet]
        [Produces("application/json")]
        public IActionResult GetPermissionScopes([FromQuery] ScopeRequest scopeRequest)
        {
            try
            {
                string[] result = null;

                result = _permissionsStore.GetScopes(scopeRequest.RequestUrl, scopeRequest.HttpVerb, scopeRequest.ScopeType);

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
                return new JsonResult(argNullException.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
            catch (Exception exception)
            {
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }       
    }
}
