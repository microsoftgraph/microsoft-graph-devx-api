// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Common;
using ChangesService.Models;
using FileService.Common;
using FileService.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChangesService.Services
{
    /// <summary>
    /// Utility functions for transforming and filtering <see cref="ChangeLogList"/> and <see cref="ChangeLog"/> objects.
    /// </summary>
    public static class ChangesService
    {
        // Field to hold key-value pairs of url and workload names
        private static readonly Dictionary<string, string> _urlWorkloadDict = new();

        /// <summary>
        /// Deserializes a <see cref="ChangeLogList"/> from a json string.
        /// </summary>
        /// <param name="jsonString">The json string to deserialize</param>
        /// <returns>The deserialized <see cref="ChangeLogList"/>.</returns>
        public static ChangeLogList DeserializeChangeLogList(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                throw new ArgumentNullException(nameof(jsonString), ChangesServiceConstants.JsonStringNullOrEmpty);
            }

            ChangeLogList changeLogList = JsonConvert.DeserializeObject<ChangeLogList>(jsonString);

            return changeLogList;
        }

        /// <summary>
        /// Filters a <see cref="ChangeLog"/> list by the specified filter options in the
        /// <see cref="ChangeLogSearchOptions"/>
        /// </summary>
        /// <param name="changeLogList">The <see cref="ChangeLogList"/> with the target
        /// <see cref="ChangeLog"/> list.</param>
        /// <param name="searchOptions">The <see cref="ChangeLogSearchOptions"/> containing options for filtering
        /// and paginating the target <see cref="ChangeLog"/> list.</param>
        /// <param name="graphProxyConfigs">Configuration settings for connecting to the Microsoft Graph Proxy.</param>
        /// <param name="httpClientUtility">Optional. An implementation instance of <see cref="IHttpClientUtility"/>.</param>
        /// <returns>A <see cref="ChangeLogList"/> containing the list of filtered and/or paginated
        /// <see cref="ChangeLog"/> list.</returns>
        public static ChangeLogList FilterChangeLogList(ChangeLogList changeLogList,
                                                        ChangeLogSearchOptions searchOptions,
                                                        MicrosoftGraphProxyConfigs graphProxyConfigs,
                                                        IHttpClientUtility httpClientUtility = null)
        {
            if (changeLogList == null)
            {
                throw new ArgumentNullException(nameof(changeLogList), ChangesServiceConstants.ValueNullError);
            }

            if (searchOptions == null)
            {
                throw new ArgumentNullException(nameof(searchOptions), ChangesServiceConstants.ValueNullError);
            }

            if (graphProxyConfigs == null)
            {
                throw new ArgumentNullException(nameof(graphProxyConfigs), ChangesServiceConstants.ValueNullError);
            }

            // Temp. var to hold cascading filtered results
            IEnumerable<ChangeLog> enumerableChangeLog = changeLogList.ChangeLogs;

            if (!string.IsNullOrEmpty(searchOptions.RequestUrl)) // filter by RequestUrl
            {
                // Retrieve the workload name from the requestUrl
                var workload = RetrieveWorkloadNameFromRequestUrl(searchOptions, graphProxyConfigs, httpClientUtility)
                                .GetAwaiter().GetResult();

                // Search by the retrieved workload name
                enumerableChangeLog = FilterChangeLogListByWorkload(changeLogList,
                                                                      workload);
            }
            else if (!string.IsNullOrEmpty(searchOptions.Workload)) // filter by Workload
            {
                // Search by the provided workload name
                enumerableChangeLog = FilterChangeLogListByWorkload(changeLogList,
                                                                      searchOptions.Workload);
            }

            if (searchOptions.StartDate != null && searchOptions.EndDate != null)
            {
                // Filter by StartDate & EndDate
                enumerableChangeLog = FilterChangeLogListByDates(changeLogList, searchOptions.StartDate.Value, searchOptions.EndDate.Value);
            }
            else if (searchOptions.StartDate != null && searchOptions.DaysRange > 0)
            {
                var endDate = searchOptions.StartDate.Value.AddDays(searchOptions.DaysRange);

                // Filter by StartDate & DaysRange
                enumerableChangeLog = FilterChangeLogListByDates(changeLogList, searchOptions.StartDate.Value, endDate);
            }
            else if (searchOptions.EndDate != null && searchOptions.DaysRange > 0)
            {
                var startDate = searchOptions.EndDate.Value.AddDays(searchOptions.DaysRange);

                // Filter by EndDate & DaysRange
                enumerableChangeLog = FilterChangeLogListByDates(changeLogList, startDate, searchOptions.EndDate.Value);
            }
            else if (searchOptions.DaysRange > 0)
            {
                // Filter by the number of days provided, up to the current date
                var startDate = DateTime.Today.AddDays(-searchOptions.DaysRange);

                enumerableChangeLog = enumerableChangeLog
                                        .Where(x => x.CreatedDateTime >= startDate &&
                                            x.CreatedDateTime <= DateTime.Today);
            }

            // Create the new filtered result
            ChangeLogList filteredChangeLogList = new()
            {
                ChangeLogs = enumerableChangeLog.ToList()
            };

            // Paginate the filtered result
            if (filteredChangeLogList.ChangeLogs.Any() && searchOptions.PageLimit != null)
            {
                filteredChangeLogList.PageLimit = searchOptions.PageLimit;

                if (searchOptions.Page == 1 || filteredChangeLogList.TotalPages == 1)
                {
                    /* The first page of several pages or
                     * the first page of only one page
                     */

                    filteredChangeLogList.Page = 1;
                    enumerableChangeLog = enumerableChangeLog.Take(searchOptions.PageLimit.Value);
                }
                else if (searchOptions.Page < filteredChangeLogList.TotalPages)
                {
                    // Any of the pages between first page and last page

                    filteredChangeLogList.Page = searchOptions.Page;

                    // Skip the previous' pages data
                    int skipItems = (searchOptions.Page - 1) * searchOptions.PageLimit.Value;
                    enumerableChangeLog = enumerableChangeLog
                                            .Skip(skipItems)
                                            .Take(searchOptions.PageLimit.Value);
                }
                else
                {
                    /* The last page or any page specified
                     * greater than the total page count.
                     */

                    filteredChangeLogList.Page = filteredChangeLogList.TotalPages;

                    int lastItems = filteredChangeLogList.ChangeLogs.Count % searchOptions.PageLimit.Value;
                    enumerableChangeLog = enumerableChangeLog.TakeLast(lastItems);
                }

                // Update with the paginated result
                filteredChangeLogList.ChangeLogs = enumerableChangeLog.ToList();
            }

            return filteredChangeLogList;
        }

        /// <summary>
        /// Filters a <see cref="ChangeLog"/> list by workload name.
        /// </summary>
        /// <param name="changeLogList">The <see cref="ChangeLogList"/> with the target
        /// <see cref="ChangeLog"/> list.</param>
        /// <param name="workloadName">Name of the target worload.</param>
        /// <returns>The <see cref="ChangeLog"/> list filtered by the provided workload name.</returns>
        private static IEnumerable<ChangeLog> FilterChangeLogListByWorkload(ChangeLogList changeLogList, string workloadName)
        {
            return changeLogList.ChangeLogs
                                .Where(x => x.WorkloadArea.Equals(workloadName,
                                        StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Filters a <see cref="ChangeLog"/> list by workload name
        /// </summary>
        /// <param name="changeLogList">The <see cref="ChangeLogList"/> with the target
        /// <see cref="ChangeLog"/> list.</param>
        /// <param name="startDate">The start date for the changelog data.</param>
        /// <param name="endDate">The end date for the changelog data.</param>
        /// <returns>The <see cref="ChangeLog"/> list filtered by the provided dates.</returns>
        private static IEnumerable<ChangeLog> FilterChangeLogListByDates(ChangeLogList changeLogList, DateTime startDate, DateTime endDate)
        {
            return changeLogList.ChangeLogs
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
        private static async Task<string> RetrieveWorkloadNameFromRequestUrl(ChangeLogSearchOptions searchOptions,
                                                                             MicrosoftGraphProxyConfigs graphProxy,
                                                                             IHttpClientUtility httpClientUtility)
        {
            // Pull out the workload name value if it was already cached
            if (_urlWorkloadDict.TryGetValue(searchOptions.RequestUrl, out string workloadValue))
            {
                return workloadValue;
            }

            if (graphProxy == null)
            {
                throw new ArgumentNullException(nameof(graphProxy), ChangesServiceConstants.ValueNullError);
            }

            if (httpClientUtility == null)
            {
                throw new ArgumentNullException(nameof(httpClientUtility), ChangesServiceConstants.ValueNullError);
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
            string workloadInfo = await httpClientUtility.ReadFromDocumentAsync(httpRequestMessage);

            // Extract the workload name from the response content
            JToken workloadInfoToken = JObject.Parse(workloadInfo);
            var targetWorkloadId = (string)workloadInfoToken["TargetWorkloadId"];
            var workloadName = targetWorkloadId.Split('.').Last();

            // Cache the retrieved workload name
            _urlWorkloadDict.Add(searchOptions.RequestUrl, workloadName);

            return workloadName;
        }
    }
}
