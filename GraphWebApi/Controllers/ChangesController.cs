// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChangesService.Common;
using ChangesService.Interfaces;
using ChangesService.Models;
using FileService.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GraphWebApi.Controllers
{
    [ApiController]
    public class ChangesController : ControllerBase
    {
        private ChangesService.Services.ChangesService _changesService;
        private readonly IChangesStore _changesStore;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly TelemetryClient _telemetry;
        private readonly IDictionary<string, string> ChangesTraceProperties = new Dictionary<string, string> { { "Changes", "Fetch" } };


        public ChangesController(IChangesStore changesStore, IConfiguration configuration,
                                 IHttpClientUtility httpClientUtility, TelemetryClient telemetry)
        {
            _telemetry = telemetry;
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

                _telemetry?.TrackTrace($"Request to fetch changelog records for the requested culture info '{cultureInfo}'",
                                        SeverityLevel.Information,
                                        ChangesTraceProperties);

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

                    changeLog = _changesService
                                    .FilterChangeLogRecords(changeLog, searchOptions, graphProxyConfigs, _httpClientUtility);
                }
                else
                {
                    // No records
                    return NoContent();
                }

                if (!changeLog.ChangeLogs.Any())
                {
                    _telemetry?.TrackTrace($"Search options not found in: requestUrl, workload, daysRange, startDate, endDate properties of changelog records",
                                           SeverityLevel.Error,
                                           ChangesTraceProperties);

                    // Filtered items yielded no result
                    return NotFound();
                }

                _telemetry?.TrackTrace($"Fetched {changeLog.CurrentItems} changes",
                                         SeverityLevel.Information,
                                         ChangesTraceProperties);
                return Ok(changeLog);
            }
            catch (InvalidOperationException invalidOpsException)
            {
                _telemetry?.TrackException(invalidOpsException,
                                          ChangesTraceProperties);
                return new JsonResult(invalidOpsException.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
            catch (ArgumentException argException)
            {
                _telemetry?.TrackException(argException,
                                          ChangesTraceProperties);
                return new JsonResult(argException.Message) { StatusCode = StatusCodes.Status404NotFound };
            }
            catch (Exception exception)
            {
                _telemetry?.TrackException(exception,
                                          ChangesTraceProperties);
                return new JsonResult(exception.Message) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
