// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using ChangesService.Common;
using ChangesService.Interfaces;
using ChangesService.Models;
using FileService.Interfaces;
using GraphWebApi.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GraphWebApi.Controllers
{
    [ApiController]
    public class ChangesController : ControllerBase
    {
        private readonly IChangesStore _changesStore;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientUtility _httpClientUtility;

        public ChangesController(IChangesStore changesStore, IConfiguration configuration, IHttpClientUtility httpClientUtility)
        {
            _changesStore = changesStore;
            _configuration = configuration;
            _httpClientUtility = httpClientUtility;
        }

        // Gets the list of changelog
        [Route("changes")]
        [Produces("application/json")]
        [HttpGet]
        public async Task<IActionResult> GetChangesAsync(
                                         [FromQuery] string requestUrl = null,
                                         [FromQuery] string workload = null,
                                         [FromQuery] double daysRange = 0,
                                         [FromQuery] DateTime? startDate = null, // yyyy-MM-ddTHH:mm:ss
                                         [FromQuery] DateTime? endDate = null, // yyyy-MM-ddTHH:mm:ss
                                         [FromQuery] int page = 1,
                                         [FromQuery] int? pageLimit = null,
                                         [FromQuery] string graphVersion = "v1.0")
        {
            try
            {
                // Options for searching, filtering and paging the changelog data
                var searchOptions = new ChangeLogSearchOptions(requestUrl: requestUrl,
                                                               workload: workload,
                                                               daysRange: daysRange,
                                                               startDate: startDate,
                                                               endDate: endDate)
                {
                    Page = page,
                    PageLimit = pageLimit
                };

                string locale = RequestHelper.GetPreferredLocaleLanguage(Request);

                // Fetch the changelog list
                var changeLog = await _changesStore.FetchChangeLogListAsync(locale);

                // Filter the changelog list
                if (changeLog.ChangeLogs.Any())
                {
                    // Configs for fetching workload names from given requestUrl
                    var graphProxyConfigs = new MicrosoftGraphProxyConfigs()
                    {
                        GraphProxyBaseUrl = _configuration[ChangesServiceConstants.GraphProxyBaseUrlConfigPath],
                        GraphProxyRelativeUrl = _configuration[ChangesServiceConstants.GraphProxyRelativeUrlConfigPath],
                        GraphProxyAuthorization = _configuration[ChangesServiceConstants.GraphProxyAuthorization],
                        GraphVersion = graphVersion
                    };

                    changeLog = ChangesService.Services.ChangesService
                                    .FilterChangeLogList(changeLog, searchOptions, graphProxyConfigs, _httpClientUtility);
                }
                else
                {
                    // Source list is empty
                    return NoContent();
                }

                if (!changeLog.ChangeLogs.Any())
                {
                    // Filtered items yielded no result
                    return NotFound();
                }

                return Ok(changeLog);
            }
            catch (InvalidOperationException ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status400BadRequest };
            }
            catch (ArgumentException ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status404NotFound };
            }
            catch (Exception ex)
            {
                return new JsonResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
