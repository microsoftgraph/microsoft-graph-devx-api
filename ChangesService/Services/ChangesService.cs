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
        /// Filters <see cref="ChangeLogRecods"/> by a given request url.
        /// </summary>
        /// <param name="requestUrl">The target url to filter the <see cref="ChangeLogRecods"/> by.</param>
        /// <param name="changeLogRecords">The <see cref="ChangeLogRecords"/> with the target
        /// <see cref="ChangeLog"/> entries.</param>
        /// <param name="graphProxyConfigs">Configuration settings for connecting to the Microsoft Graph Proxy.</param>
        /// <param name="workloadServiceMappings">Workload service mappings dictionary.</param>
        /// <param name="httpClientUtility">An implementation instance of <see cref="IHttpClientUtility"/>.</param>
        /// <returns><see cref="ChangeLogRecords"/> containing the filtered and/or paginated
        /// <see cref="ChangeLog"/> entries.</returns>
        public async Task<ChangeLogRecords> FilterChangeLogRecordsByUrlAsync(string requestUrl,
                                                                             ChangeLogRecords changeLogRecords,
                                                                             MicrosoftGraphProxyConfigs graphProxyConfigs,
                                                                             Dictionary<string, string> workloadServiceMappings,
                                                                             IHttpClientUtility httpClientUtility)
        {
            _telemetryClient?.TrackTrace($"Filtering changelog records by request url: {requestUrl}",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);

            UtilityFunctions.CheckArgumentNullOrEmpty(requestUrl, nameof(requestUrl));
            UtilityFunctions.CheckArgumentNull(changeLogRecords, nameof(changeLogRecords));
            UtilityFunctions.CheckArgumentNull(graphProxyConfigs, nameof(graphProxyConfigs));
            UtilityFunctions.CheckArgumentNull(workloadServiceMappings, nameof(workloadServiceMappings));

            var enumerableChangeLog = changeLogRecords.ChangeLogs;

            // Retrieve the service name from the requestUrl
            var serviceName = await RetrieveServiceNameFromUrlAsync(requestUrl, graphProxyConfigs, workloadServiceMappings, httpClientUtility);

            // Search by the retrieved service name
            enumerableChangeLog = changeLogRecords.ChangeLogs.Where(x => x.WorkloadArea.Equals(serviceName,
                StringComparison.OrdinalIgnoreCase) && x.Version.Equals(graphProxyConfigs.GraphVersion, StringComparison.OrdinalIgnoreCase));

            // Search for url segment values in the ChangeList Target property value
            var urlSegments = requestUrl.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
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

            // Create the new filtered result
            ChangeLogRecords filteredChangeLogRecords = new()
            {
                ChangeLogs = enumerableChangeLog.ToList()
            };

            _telemetryClient?.TrackTrace($"Finished filtering changelog records by request url: {requestUrl}",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);

            return filteredChangeLogRecords;
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
        private async Task<string> RetrieveServiceNameFromUrlAsync(string requestUrl,
                                                                     MicrosoftGraphProxyConfigs graphProxy,
                                                                     Dictionary<string, string> workloadServiceMappings,
                                                                     IHttpClientUtility httpClientUtility)
        {
            _telemetryClient?.TrackTrace($"Retrieving service name for url: {requestUrl}",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);

            // Pull out the service name value if it was already cached
            if (_urlServiceNameDict.TryGetValue(requestUrl, out var serviceName))
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
                                    requestUrl);

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
                _urlServiceNameDict.Add(requestUrl, serviceName);

                _telemetryClient?.TrackTrace($"Finished retrieving service name for request url '{requestUrl}'. " +
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

        public (string GraphVersion, string RequestUrl) ExtractGraphVersionAndUrlValues(string requestUrl)
        {
            UtilityFunctions.CheckArgumentNullOrEmpty(requestUrl, nameof(requestUrl));

            string graphVersion;
            var urlSegments = requestUrl.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            switch (urlSegments.FirstOrDefault())
            {
                case ChangesServiceConstants.GraphVersion_Beta:
                    requestUrl = requestUrl.Replace(ChangesServiceConstants.GraphVersion_Beta, string.Empty);
                    graphVersion = ChangesServiceConstants.GraphVersion_Beta;
                    break;
                default:
                    requestUrl = requestUrl.Replace(ChangesServiceConstants.GraphVersion_V1, string.Empty);
                    graphVersion = ChangesServiceConstants.GraphVersion_V1;
                    break;
            }

            if (!requestUrl.StartsWith('/'))
            {
                requestUrl = $"/{requestUrl}";
            }

            return (requestUrl, graphVersion);
        }
    }
}
