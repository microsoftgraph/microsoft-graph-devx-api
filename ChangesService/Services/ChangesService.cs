// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Common;
using ChangesService.Models;
using FileService.Common;
using FileService.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChangesService.Services
{
    /// <summary>
    /// Provides utility functions for transforming and filtering <see cref="ChangeLogList"/> and <see cref="ChangeLog"/> objects.
    /// </summary>
    public static class ChangesService
    {
        private static readonly IDictionary<string, string> UrlWorkloadDict = new Dictionary<string, string>();

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
        /// <returns>A <see cref="ChangeLogList"/> containing the list of filtered and/or paginated <see cref="ChangeLog"/> list.</returns>
        public static ChangeLogList FilterChangeLogList(ChangeLogList changeLogList,
            ChangeLogSearchOptions searchOptions, MicrosoftGraphProxyConfigs graphProxyConfigs)
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

            // Temp. var to hold cascading filtered data
            ChangeLogList tempChangeLogList = changeLogList;

            // Search the changelog list by workload
            ChangeLogList changeLogListByWorkload = new ChangeLogList();

            if (!string.IsNullOrEmpty(searchOptions.RequestUrl))
            {
                // Retrieve the workload name from the requestUrl
                var workload = RetrieveWorkloadNameFromRequestUrl(searchOptions, graphProxyConfigs).GetAwaiter().GetResult();

                // Search by the workload name
                changeLogListByWorkload.ChangeLogs = FilterChangeLogListByWorkload(tempChangeLogList,
                                                                      workload).ToList();

                tempChangeLogList = changeLogListByWorkload;
            }
            else if (!string.IsNullOrEmpty(searchOptions.Workload))
            {
                // Search by the workload name
                changeLogListByWorkload.ChangeLogs = FilterChangeLogListByWorkload(tempChangeLogList,
                                                                      searchOptions.Workload).ToList();

                tempChangeLogList = changeLogListByWorkload;
            }

            // Filter the search result by CreatedDate
            ChangeLogList changeLogListByDate = new ChangeLogList();

            if (searchOptions.StartDate != null && searchOptions.EndDate != null)
            {
                // Filter by start date and end date
                changeLogListByDate.ChangeLogs = tempChangeLogList.ChangeLogs
                                                 .Where(x => x.CreatedDateTime >= searchOptions.StartDate &&
                                                    x.CreatedDateTime <= searchOptions.EndDate)
                                                 .ToList();

                tempChangeLogList = changeLogListByDate;
            }
            else if (searchOptions.DaysRange > 0)
            {
                // Filter by the number of days provided, starting from current date
                DateTime startDate = DateTime.Today.AddDays(-searchOptions.DaysRange);

                changeLogListByDate.ChangeLogs = tempChangeLogList.ChangeLogs
                                                 .Where(x => x.CreatedDateTime >= startDate &&
                                                    x.CreatedDateTime <= DateTime.Today)
                                                 .ToList();

                tempChangeLogList = changeLogListByDate;
            }

            // Paginate the filtered result
            if (tempChangeLogList.ChangeLogs.Any() && searchOptions.PageLimit != null)
            {
                tempChangeLogList.PageLimit = searchOptions.PageLimit;

                if (searchOptions.Page == 1 || tempChangeLogList.TotalPages == 1)
                {
                    /* The first page of several pages or
                     * the first page of only one page
                     */

                    tempChangeLogList.Page = 1;
                    tempChangeLogList.ChangeLogs = tempChangeLogList.ChangeLogs
                                                    .Take(searchOptions.PageLimit.Value)
                                                    .ToList();
                }
                else if (searchOptions.Page < tempChangeLogList.TotalPages)
                {
                    // Any of the pages between first and last

                    tempChangeLogList.Page = searchOptions.Page;

                    // Skip the previous' pages data
                    int skipItems = (searchOptions.Page - 1) * searchOptions.PageLimit.Value;
                    tempChangeLogList.ChangeLogs = tempChangeLogList.ChangeLogs
                                                    .Skip(skipItems)
                                                    .Take(searchOptions.PageLimit.Value)
                                                    .ToList();
                }
                else
                {
                    /* The last page or any page specified
                     * greater than the total page count.
                     */

                    tempChangeLogList.Page = tempChangeLogList.TotalPages;

                    int lastItems = tempChangeLogList.ChangeLogs.Count() % searchOptions.PageLimit.Value;
                    tempChangeLogList.ChangeLogs = tempChangeLogList.ChangeLogs
                                                    .TakeLast(lastItems)
                                                    .ToList();
                }
            }

            return tempChangeLogList;
        }

        /// <summary>
        /// Filters a <see cref="ChangeLog"/> list by workload name.
        /// </summary>
        /// <param name="changeLogList">The <see cref="ChangeLogList"/> with the target
        /// <see cref="ChangeLog"/> list.</param>
        /// <param name="workloadName">Name of the target worload.</param>
        /// <returns>The <see cref="ChangeLog"/> list filtered by the provided workload name.</returns>
        private static IEnumerable<ChangeLog>FilterChangeLogListByWorkload(ChangeLogList changeLogList,
            string workloadName)
        {
            return changeLogList.ChangeLogs
                                .Where(x => x.WorkloadArea.Equals(workloadName,
                                        StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Retrieves the workload name for a given Microsoft Graph request url.
        /// </summary>
        /// <param name="searchOptions"><see cref="ChangeLogSearchOptions"/> containing the target request url
        /// and the relevant information required to call Microsoft Graph and retrieve the required workload name
        /// of the target request url.</param>
        /// <returns>The workload name for the target request url.</returns>
        private static async Task<string> RetrieveWorkloadNameFromRequestUrl(ChangeLogSearchOptions searchOptions,
            MicrosoftGraphProxyConfigs graphProxy)
        {
            // Pull out the workload name value if it was already cached
            if (UrlWorkloadDict.TryGetValue(searchOptions.RequestUrl, out string workloadValue))
            {
                return workloadValue;
            }

            var httpClientUtility = new HttpClientUtility(graphProxy.GraphProxyBaseUrl, new Dictionary<string, string>
            {
                // Authorization
                { FileServiceConstants.HttpRequest.Headers.Authorization.ToString(),
                    graphProxy.GraphProxyAuthorization },

                // Accept
                { FileServiceConstants.HttpRequest.Headers.Accept.ToString(),
                    FileServiceConstants.HttpRequest.ApplicationJsonMediaType },

                // User-Agent
                { FileServiceConstants.HttpRequest.Headers.UserAgent.ToString(),
                    FileServiceConstants.HttpRequest.DevxApiUserAgent }
            });

            var relativeProxyUrl = string.Format(graphProxy.GraphProxyRelativeUrl, graphProxy.GraphVersion,
                                    searchOptions.RequestUrl);

            // Fetch the request url workload info. content from Microsoft Graph
            string workloadInfo = await httpClientUtility.ReadFromFile(relativeProxyUrl);

            // Extract the workload name from the response content
            JToken workloadInfoToken = JObject.Parse(workloadInfo);
            var targetWorkloadId = (string)workloadInfoToken["TargetWorkloadId"];
            var workloadName = targetWorkloadId.Split('.').Last();

            // Cache this value
            UrlWorkloadDict.Add(searchOptions.RequestUrl, workloadName);

            return workloadName;
        }
    }
}
