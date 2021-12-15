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
using static ChangesService.Models.ChangeLog;

namespace ChangesService.Services
{
    /// <summary>
    /// Utility functions for transforming and filtering <see cref="ChangeLogRecords"/> and <see cref="ChangeLog"/> objects.
    /// </summary>
    public class ChangesService : IChangesService
    {
        // Field to hold key-value pairs of url and workload names
        private static readonly Dictionary<string, string> _urlServiceNameDict = new();
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
        /// <param name="searchOptions">The <see cref="ChangeLogSearchOptions"/> containing options for slicing
        /// the target <see cref="ChangeLog"/> entries.</param>
        /// <param name="graphProxyConfigs">Configuration settings for connecting to the Microsoft Graph Proxy.</param>
        /// <param name="workloadServiceMappings">Workload service mappings dictionary.</param>
        /// <param name="httpClientUtility">Optional. An implementation instance of <see cref="IHttpClientUtility"/>.</param>
        /// <returns><see cref="ChangeLogRecords"/> containing the filtered and/or paginated
        /// <see cref="ChangeLog"/> entries.</returns>
        public ChangeLogRecords FilterChangeLogRecords(ChangeLogRecords changeLogRecords,
                                                       ChangeLogSearchOptions searchOptions,
                                                       MicrosoftGraphProxyConfigs graphProxyConfigs,
                                                       Dictionary<string, string> workloadServiceMappings,
                                                       IHttpClientUtility httpClientUtility = null)
        {
            _telemetryClient?.TrackTrace("Filtering changelog records",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);

            string filterType = null;

            UtilityFunctions.CheckArgumentNull(changeLogRecords, nameof(changeLogRecords));
            UtilityFunctions.CheckArgumentNull(searchOptions, nameof(searchOptions));
            UtilityFunctions.CheckArgumentNull(graphProxyConfigs, nameof(graphProxyConfigs));
            UtilityFunctions.CheckArgumentNull(workloadServiceMappings, nameof(workloadServiceMappings));

            // Temp. var to hold cascading filtered results
            IEnumerable<ChangeLog> enumerableChangeLog = changeLogRecords.ChangeLogs;

            if (!string.IsNullOrEmpty(searchOptions.RequestUrl)) // filter by RequestUrl
            {
                filterType = $"'Request Url: {searchOptions.RequestUrl}'";

                // Retrieve the service name from the requestUrl
                var serviceName = RetrieveServiceNameFromRequestUrl(searchOptions, graphProxyConfigs, workloadServiceMappings, httpClientUtility)
                                .GetAwaiter().GetResult();

                // Search by the retrieved workload name
                enumerableChangeLog = FilterChangeLogRecordsByServiceName(changeLogRecords, serviceName, searchOptions.GraphVersion);

                // Search for url segment values in the ChangeList Target property value
                var urlSegments = searchOptions.RequestUrl.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
                var changeLogList = enumerableChangeLog.ToList();
                changeLogList.ForEach(changeLog =>
                {
                    var changeList = new List<Change>();
                    urlSegments.ForEach(segment =>
                    {
                        changeList.AddRange(changeLog.ChangeList
                            .Where(x => x.Target.ToLowerInvariant().Split(',', StringSplitOptions.RemoveEmptyEntries) // ChangeList Target property values are comma separated
                            .Contains(segment.ToLowerInvariant())));
                    });

                    changeLog.ChangeList = changeList;
                });

                enumerableChangeLog = changeLogList.Where(x => x.ChangeList?.Any() ?? false);
            }
            else if (!string.IsNullOrEmpty(searchOptions.Service)) // filter by service
            {
                filterType = $"'Service: {searchOptions.Service}'";

                // Search by the provided service name
                enumerableChangeLog = FilterChangeLogRecordsByServiceName(changeLogRecords, searchOptions.Service, searchOptions.GraphVersion);
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

            return ApplyChangeLogQueryOptions(filteredChangeLogRecords, searchOptions);
        }

        /// <summary>
        /// Applies <see cref="ChangeLogQueryOptions"/> to slice the <see cref="ChangeLogRecords"/>.
        /// </summary>
        /// <param name="changeLogRecords">The <see cref="ChangeLogRecords"/> with the target
        /// <see cref="ChangeLog"/> entries to be sliced.</param>
        /// <param name="searchOptions">The <see cref="ChangeLogSearchOptions"/> containing options for slicing
        /// the target <see cref="ChangeLog"/> entries.</param>
        /// <returns>The sliced <see cref="ChangeLogRecords"/>.</returns>
        private static ChangeLogRecords ApplyChangeLogQueryOptions(ChangeLogRecords changeLogRecords,
                                                                   ChangeLogSearchOptions searchOptions)
        {
            if (changeLogRecords.ChangeLogs.Any())
            {
                IEnumerable<ChangeLog> enumerableChangeLogs = null;

                if (searchOptions.Top != null && searchOptions.Skip > 0)
                {
                    changeLogRecords.Skip = searchOptions.Skip;
                    changeLogRecords.Top = searchOptions.Top;
                    enumerableChangeLogs = changeLogRecords.ChangeLogs
                                                           .Skip(searchOptions.Skip)
                                                           .Take(searchOptions.Top.Value);
                }
                else if (searchOptions.Top != null)
                {
                    changeLogRecords.Top = searchOptions.Top;
                    enumerableChangeLogs = changeLogRecords.ChangeLogs
                                                           .Take(searchOptions.Top.Value);
                }
                else if (searchOptions.Skip > 0)
                {
                    changeLogRecords.Skip = searchOptions.Skip;
                    enumerableChangeLogs = changeLogRecords.ChangeLogs
                                                           .Skip(searchOptions.Skip);
                }

                if (enumerableChangeLogs != null)
                {
                    changeLogRecords.ChangeLogs = enumerableChangeLogs.ToList();
                }
            }

            return changeLogRecords;
        }

        /// <summary>
        /// Filters <see cref="ChangeLogRecords"/> by the service name.
        /// </summary>
        /// <param name="changeLogRecords">The <see cref="ChangeLogRecords"/> with the target
        /// <see cref="ChangeLog"/> entries.</param>
        /// <param name="serviceName">Name of the target worload.</param>
        /// <returns>The <see cref="ChangeLog"/> entries filtered by the provided workload name.</returns>
        private static IEnumerable<ChangeLog> FilterChangeLogRecordsByServiceName(ChangeLogRecords changeLogRecords, string serviceName, string version)
        {
            return changeLogRecords.ChangeLogs
                                .Where(x => x.WorkloadArea.Equals(serviceName,
                                        StringComparison.OrdinalIgnoreCase) &&
                                        x.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
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
        /// Retrieves the service name for a given Microsoft Graph request url.
        /// </summary>
        /// <param name="searchOptions"><see cref="ChangeLogSearchOptions"/> containing the target request url
        /// and the relevant information required to call Microsoft Graph and retrieve the required workload name
        /// of the target request url.</param>
        /// <param name="graphProxy">Configuration settings for connecting to the Microsoft Graph Proxy.</param>
        /// <param name="workloadServiceMappings">Workload service mappings dictionary.</param>
        /// <param name="httpClientUtility">An implementation instance of <see cref="IFileUtility"/>.</param>
        /// <returns>The service name of the target request url.</returns>
        private async Task<string> RetrieveServiceNameFromRequestUrl(ChangeLogSearchOptions searchOptions,
                                                                     MicrosoftGraphProxyConfigs graphProxy,
                                                                     Dictionary<string, string> workloadServiceMappings,
                                                                     IHttpClientUtility httpClientUtility)
        {
            _telemetryClient?.TrackTrace($"Retrieving service name for url '{searchOptions.RequestUrl}'",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);

            // Pull out the service name value if it was already cached
            if (_urlServiceNameDict.TryGetValue(searchOptions.RequestUrl, out var serviceName))
            {
                return serviceName;
            }

            UtilityFunctions.CheckArgumentNull(graphProxy, nameof(graphProxy));
            UtilityFunctions.CheckArgumentNull(httpClientUtility, nameof(httpClientUtility));

            // Fetch the Graph Proxy Url
            using var graphProxyRequestMessage = new HttpRequestMessage(HttpMethod.Get, graphProxy.GraphProxyRequestUrl);
            string graphProxyBaseUrl = await httpClientUtility.ReadFromDocumentAsync(graphProxyRequestMessage);

            if (!string.IsNullOrEmpty(graphProxyBaseUrl))
            {
                graphProxy.GraphProxyBaseUrl = graphProxyBaseUrl.Trim('"');
            }

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
            var workloadInfo = await httpClientUtility.ReadFromDocumentAsync(httpRequestMessage);

            if (workloadInfo.Contains("\"error\""))
            {
                throw new InvalidOperationException(workloadInfo);
            }

            // Extract the workload name from the response content
            var workloadInfoToken = JObject.Parse(workloadInfo);
            var targetWorkloadId = (string)workloadInfoToken["TargetWorkloadId"];

            // Retrieve the service name using the returned TargetWorkloadId
            workloadServiceMappings.TryGetValue(targetWorkloadId, out serviceName);

            if (!string.IsNullOrEmpty(serviceName))
            {
                // Cache the retrieved service name against the base url
                _urlServiceNameDict.Add(searchOptions.RequestUrl, serviceName);

                _telemetryClient?.TrackTrace($"Finished retrieving service name for request url '{searchOptions.RequestUrl}'. " +
                                             $"TargetWorkloadId: {serviceName}" +
                                             $"Corresponding service name: {serviceName}",
                                             SeverityLevel.Information,
                                             _changesTraceProperties);

                return serviceName;
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Service name not found for the WorkloadId: {targetWorkloadId}");
            }
        }
    }
}
