// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Common;
using ChangesService.Interfaces;
using ChangesService.Models;
using FileService.Common;
using FileService.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UtilityService;

namespace ChangesService.Services
{
    /// <summary>
    /// Utility functions for transforming and filtering <see cref="ChangeLogRecords"/> and <see cref="ChangeLog"/> objects.
    /// </summary>
    public class ChangesService : IChangesService
    {
        // Field to hold key-value pairs of url and workload names
        private static readonly Dictionary<string, string> _urlWorkloadDict = new();
        private static readonly Dictionary<string, string> _changesTraceProperties =
                        new() { { UtilityConstants.TelemetryPropertyKey_Changes, nameof(ChangesService)} };
        private readonly TelemetryClient _telemetryClient;

        public ChangesService(TelemetryClient telemetryClient = null)
        {
            _telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Deserializes a <see cref="ChangeLogRecords"/> from a json string.
        /// </summary>
        /// <param name="jsonString">The json string to deserialize</param>
        /// <returns>The deserialized <see cref="ChangeLogRecords"/>.</returns>
        public ChangeLogRecords DeserializeChangeLogRecords(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                throw new ArgumentNullException(nameof(jsonString), ChangesServiceConstants.JsonStringNullOrEmpty);
            }

            ChangeLogRecords changeLogRecords = JsonConvert.DeserializeObject<ChangeLogRecords>(jsonString);

            return changeLogRecords;
        }

        /// <summary>
        /// Filters <see cref="ChangeLogRecods"/> by the specified filter options in the
        /// <see cref="ChangeLogSearchOptions"/>
        /// </summary>
        /// <param name="changeLogRecords">The <see cref="ChangeLogRecords"/> with the target
        /// <see cref="ChangeLog"/> entries.</param>
        /// <param name="searchOptions">The <see cref="ChangeLogSearchOptions"/> containing options for filtering
        /// and paginating the target <see cref="ChangeLog"/> entries.</param>
        /// <param name="graphProxyConfigs">Configuration settings for connecting to the Microsoft Graph Proxy.</param>
        /// <param name="httpClientUtility">Optional. An implementation instance of <see cref="IHttpClientUtility"/>.</param>
        /// <returns><see cref="ChangeLogRecords"/> containing the filtered and/or paginated
        /// <see cref="ChangeLog"/> entries.</returns>
        public ChangeLogRecords FilterChangeLogRecords(ChangeLogRecords changeLogRecords,
        ChangeLogSearchOptions searchOptions,
                                                              MicrosoftGraphProxyConfigs graphProxyConfigs,
                                                              IHttpClientUtility httpClientUtility = null)
        {
            _telemetryClient?.TrackTrace("Filtering changelog records",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);

            string filterType = null;

            UtilityFunctions.CheckArgumentNull(changeLogRecords, nameof(changeLogRecords));
            UtilityFunctions.CheckArgumentNull(searchOptions, nameof(searchOptions));
            UtilityFunctions.CheckArgumentNull(graphProxyConfigs, nameof(graphProxyConfigs));

            // Temp. var to hold cascading filtered results
            IEnumerable<ChangeLog> enumerableChangeLog = changeLogRecords.ChangeLogs;

            if (!string.IsNullOrEmpty(searchOptions.RequestUrl)) // filter by RequestUrl
            {
                filterType = $"'Request Url: {searchOptions.RequestUrl}'";

                // Retrieve the workload name from the requestUrl
                var workload = RetrieveWorkloadNameFromRequestUrl(searchOptions, graphProxyConfigs, httpClientUtility)
                                .GetAwaiter().GetResult();

                // Search by the retrieved workload name
                enumerableChangeLog = FilterChangeLogRecordsByWorkload(changeLogRecords,
                                                                      workload);
            }
            else if (!string.IsNullOrEmpty(searchOptions.Workload)) // filter by Workload
            {
                filterType = $"'Workload: {searchOptions.Workload}'";

                // Search by the provided workload name
                enumerableChangeLog = FilterChangeLogRecordsByWorkload(changeLogRecords,
                                                                      searchOptions.Workload);
            }

            if (searchOptions.StartDate != null && searchOptions.EndDate != null)
            {
                // Filter by StartDate & EndDate
                enumerableChangeLog = FilterChangeLogRecordsByDates(changeLogRecords, searchOptions.StartDate.Value, searchOptions.EndDate.Value);
            }
            else if (searchOptions.StartDate != null && searchOptions.DaysRange > 0)
            {
                // Filter by StartDate & DaysRange (lookahead: Given StartDate, look ahead {DaysRange} days)
                var endDate = searchOptions.StartDate.Value.AddDays(searchOptions.DaysRange);

                enumerableChangeLog = FilterChangeLogRecordsByDates(changeLogRecords, searchOptions.StartDate.Value, endDate);
            }
            else if (searchOptions.EndDate != null && searchOptions.DaysRange > 0)
            {
                // Filter by EndDate & DaysRange (lookbehind: Given EndDate, look back {DaysRange} days)
                var startDate = searchOptions.EndDate.Value.AddDays(-searchOptions.DaysRange);

                enumerableChangeLog = FilterChangeLogRecordsByDates(changeLogRecords, startDate, searchOptions.EndDate.Value);
            }
            else if (searchOptions.DaysRange > 0)
            {
                // Filter by the number of days provided, up to the current date (negative lookahead: Given DaysRange, go back {DaysRange} days and find the StartDate)
                var startDate = DateTime.Today.AddDays(-searchOptions.DaysRange);

                enumerableChangeLog = enumerableChangeLog
                                        .Where(x => x.CreatedDateTime >= startDate &&
                                            x.CreatedDateTime <= DateTime.Today);
            }

            // Create the new filtered result
            ChangeLogRecords filteredChangeLogRecords = new()
            {
                ChangeLogs = enumerableChangeLog.ToList()
            };

            _telemetryClient?.TrackTrace($"Completed filtering changelog records by '{filterType}'",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);

            return PaginateChangeLogRecords(filteredChangeLogRecords, searchOptions);
        }

        /// <summary>
        /// Paginates <see cref="ChangeLogRecords"/>.
        /// </summary>
        /// <param name="changeLogRecords">The <see cref="ChangeLogRecords"/> with the target
        /// <see cref="ChangeLog"/> entries to be paginated.</param>
        /// <param name="searchOptions">The <see cref="ChangeLogSearchOptions"/> containing options for filtering
        /// and paginating the target <see cref="ChangeLog"/> entries.</param>
        /// <returns>The paginated <see cref="ChangeLogRecords"/>.</returns>
        private static ChangeLogRecords PaginateChangeLogRecords(ChangeLogRecords changeLogRecords,
                                                                 ChangeLogSearchOptions searchOptions)
        {
            if (changeLogRecords.ChangeLogs.Any() && searchOptions.PageLimit != null)
            {
                IEnumerable<ChangeLog> enumerableChangeLogs;
                changeLogRecords.PageLimit = searchOptions.PageLimit;

                if (searchOptions.Page == 1 || changeLogRecords.TotalPages == 1)
                {
                    /* The first page of several pages or
                     * the first page of only one page
                     */

                    changeLogRecords.Page = 1;
                    enumerableChangeLogs = changeLogRecords.ChangeLogs
                                                            .Take(searchOptions.PageLimit.Value);
                }
                else if (searchOptions.Page < changeLogRecords.TotalPages)
                {
                    // Any of the pages between first page and last page

                    changeLogRecords.Page = searchOptions.Page;

                    // Skip the previous' pages data
                    int skipItems = (searchOptions.Page - 1) * searchOptions.PageLimit.Value;
                    enumerableChangeLogs = changeLogRecords.ChangeLogs
                                                            .Skip(skipItems)
                                                            .Take(searchOptions.PageLimit.Value);
                }
                else
                {
                    /* The last page or any page specified
                     * greater than the total page count.
                     */

                    changeLogRecords.Page = changeLogRecords.TotalPages;

                    int lastItems = changeLogRecords.ChangeLogs.Count() % searchOptions.PageLimit.Value;
                    enumerableChangeLogs = changeLogRecords.ChangeLogs
                                                            .TakeLast(lastItems);
                }

                changeLogRecords.ChangeLogs = enumerableChangeLogs.ToList();
            }

            return changeLogRecords;
        }

        /// <summary>
        /// Filters <see cref="ChangeLogRecords"/> by workload name.
        /// </summary>
        /// <param name="changeLogRecords">The <see cref="ChangeLogRecords"/> with the target
        /// <see cref="ChangeLog"/> entries.</param>
        /// <param name="workloadName">Name of the target worload.</param>
        /// <returns>The <see cref="ChangeLog"/> entries filtered by the provided workload name.</returns>
        private static IEnumerable<ChangeLog> FilterChangeLogRecordsByWorkload(ChangeLogRecords changeLogRecords, string workloadName)
        {
            return changeLogRecords.ChangeLogs
                                .Where(x => x.WorkloadArea.Equals(workloadName,
                                        StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Filters <see cref="ChangeLogRecords"/> by workload name
        /// </summary>
        /// <param name="changeLogRecords">The <see cref="ChangeLogRecords"/> with the target
        /// <see cref="ChangeLog"/> entries.</param>
        /// <param name="startDate">The start date for the changelog data.</param>
        /// <param name="endDate">The end date for the changelog data.</param>
        /// <returns>The <see cref="ChangeLog"/> entries filtered by the provided dates.</returns>
        private static IEnumerable<ChangeLog> FilterChangeLogRecordsByDates(ChangeLogRecords changeLogRecords, DateTime startDate, DateTime endDate)
        {
            return changeLogRecords.ChangeLogs
                                .Where(x => x.CreatedDateTime >= startDate &&
                                    x.CreatedDateTime <= endDate);
        }

        /// <summary>
        /// Retrieves the workload name for a given Microsoft Graph request url.
        /// </summary>
        /// <param name="searchOptions"><see cref="ChangeLogSearchOptions"/> containing the target request url
        /// and the relevant information required to call Microsoft Graph and retrieve the required workload name
        /// of the target request url.</param>
        /// <param name="graphProxy">Configuration settings for connecting to the Microsoft Graph Proxy.</param>
        /// <param name="httpClientUtility">An implementation instance of <see cref="IFileUtility"/>.</param>
        /// <returns>The workload name for the target request url.</returns>
        private async Task<string> RetrieveWorkloadNameFromRequestUrl(ChangeLogSearchOptions searchOptions,
                                                                             MicrosoftGraphProxyConfigs graphProxy,
                                                                             IHttpClientUtility httpClientUtility)
        {
            _telemetryClient?.TrackTrace($"Retrieving workload name for url '{searchOptions.RequestUrl}'",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);

            // Pull out the workload name value if it was already cached
            if (_urlWorkloadDict.TryGetValue(searchOptions.RequestUrl, out string workloadValue))
            {
                return workloadValue;
            }

            UtilityFunctions.CheckArgumentNull(graphProxy, nameof(graphProxy));
            UtilityFunctions.CheckArgumentNull(httpClientUtility, nameof(httpClientUtility));

            // The proxy url helps fetch data from Microsoft Graph anonymously
            var relativeProxyUrl = string.Format(graphProxy.GraphProxyRelativeUrl, graphProxy.GraphVersion,
                                    searchOptions.RequestUrl);

            // Get the absolute uri
            var requestUri = graphProxy.GraphProxyBaseUrl + relativeProxyUrl;

            // Construct the http request message
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

            // Add the request headers
            httpRequestMessage.Headers.Add(FileServiceConstants.HttpRequest.Headers.Authorization.ToString(),
                        graphProxy.GraphProxyAuthorization); // Authorization
            httpRequestMessage.Headers.Add(FileServiceConstants.HttpRequest.Headers.Accept.ToString(),
                    FileServiceConstants.HttpRequest.ApplicationJsonMediaType); // Accept
            httpRequestMessage.Headers.Add(FileServiceConstants.HttpRequest.Headers.UserAgent.ToString(),
                FileServiceConstants.HttpRequest.DevxApiUserAgent); // User Agent

            // Fetch the request url workload info. content from Microsoft Graph
            string workloadInfo = await httpClientUtility.ReadFromDocumentAsync(httpRequestMessage);

            // Extract the workload name from the response content
            JToken workloadInfoToken = JObject.Parse(workloadInfo);
            var targetWorkloadId = (string)workloadInfoToken["TargetWorkloadId"];
            var workloadName = targetWorkloadId.Split('.').Last();

            // Cache the retrieved workload name
            _urlWorkloadDict.Add(searchOptions.RequestUrl, workloadName);

            _telemetryClient?.TrackTrace($"Finished retrieving workload name for url '{searchOptions.RequestUrl}'. " +
                                         $"Retrieved workload name: {workloadName}",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);

            return workloadName;
            // NB: No test coverage for this currently; requires a service call to the Graph proxy url
        }
    }
}
