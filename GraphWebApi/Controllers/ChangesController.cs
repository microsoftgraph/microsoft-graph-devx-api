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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
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

        // Gets the changelog records
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

                // Get the requested culture info.
                var cultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
                var cultureInfo = cultureFeature.RequestCulture.Culture;

                // Fetch the changelog records
                var changeLog = await _changesStore.FetchChangeLogRecordsAsync(cultureInfo);

                // Filter the changelog records
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
                                    .FilterChangeLogRecords(changeLog, searchOptions, graphProxyConfigs, _httpClientUtility);
                }
                else
                {
                    // No records
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
