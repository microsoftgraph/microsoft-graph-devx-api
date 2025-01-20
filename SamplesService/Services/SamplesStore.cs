// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using AsyncKeyedLock;
using FileService.Common;
using FileService.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SamplesService.Interfaces;
using SamplesService.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UtilityService;

namespace SamplesService.Services
{
    /// <summary>
    /// Retrieves or adds localized sample queries to and from a memory cache or a remote source.
    /// </summary>
    public class SamplesStore : ISamplesStore
    {
        private static readonly AsyncKeyedLocker<string> _asyncKeyedLocker = new();
        private readonly IFileUtility _fileUtility;
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly IMemoryCache _samplesCache;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, string> SamplesTraceProperties =
            new() { { UtilityConstants.TelemetryPropertyKey_Samples, nameof(SamplesStore)} };
        private readonly string _sampleQueriesContainerName;
        private readonly string _sampleQueriesBlobName;
        private readonly int _defaultRefreshTimeInHours;
        private const string NullValueError = "Value cannot be null";
        private readonly TelemetryClient _telemetryClient;

        public SamplesStore(IConfiguration configuration, IHttpClientUtility httpClientUtility,
                            IFileUtility fileUtility, IMemoryCache samplesCache, TelemetryClient telemetryClient = null)
        {
            _telemetryClient = telemetryClient;
            _configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration), $"{ NullValueError }: { nameof(configuration) }");
            _httpClientUtility = httpClientUtility
                ?? throw new ArgumentNullException(nameof(httpClientUtility), $"{ NullValueError }: { nameof(httpClientUtility) }");
            _fileUtility = fileUtility
                ?? throw new ArgumentNullException(nameof(fileUtility), $"{ NullValueError }: { nameof(fileUtility) }");
            _samplesCache = samplesCache
                ?? throw new ArgumentNullException(nameof(samplesCache), $"{ NullValueError }: { nameof(samplesCache) }");
            _sampleQueriesContainerName = _configuration["BlobStorage:Containers:SampleQueries"];
            _sampleQueriesBlobName = _configuration["BlobStorage:Blobs:SampleQueries"];
            _defaultRefreshTimeInHours = FileServiceHelper.GetFileCacheRefreshTime(configuration["FileCacheRefreshTimeInHours:SampleQueries"]);
        }

        /// <summary>
        /// Fetches the sample queries from the cache or a JSON file and returns a deserialized instance of a
        /// <see cref="SampleQueriesList"/> from this.
        /// </summary>
        /// <param name="locale">The language code for the preferred localized file.</param>
        /// <returns>The deserialized instance of a <see cref="SampleQueriesList"/>.</returns>
        public async Task<SampleQueriesList> FetchSampleQueriesListAsync(string locale)
        {
            _telemetryClient?.TrackTrace($"Retrieving sample queries list for locale '{locale}' from in-memory cache '{locale}'",
                                         SeverityLevel.Information,
                                         SamplesTraceProperties);

            string sourceMsg = $"Return sample queries list for locale '{locale}' from in-memory cache '{locale}'";

            // making sure only a single thread at a time access the cache
            // when already seeded, lock will resolve fast and access the cache
            // when not seeded, lock will resolve slow for all other threads and seed the cache on the first thread
            using (await _asyncKeyedLocker.LockAsync("samples"))
            {
                // Fetch cached sample queries
                var sampleQueriesList = await _samplesCache.GetOrCreateAsync(locale, async cacheEntry =>
                {
                    _telemetryClient?.TrackTrace($"In-memory cache '{locale}' empty. " +
                                                $"Seeding sample queries list from Azure Blob resource",
                                                SeverityLevel.Information,
                                                SamplesTraceProperties);

                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);

                    // Fetch the requisite sample path source based on the locale
                    string queriesFilePathSource =
                        FileServiceHelper.GetLocalizedFilePathSource(_sampleQueriesContainerName, _sampleQueriesBlobName, locale);

                    // Get the file contents from source
                    string jsonFileContents = await _fileUtility.ReadFromFileAsync(queriesFilePathSource);

                    _telemetryClient?.TrackTrace($"Successfully seeded sample queries list for locale '{locale}' from Azure Blob resource",
                                                    SeverityLevel.Information,
                                                    SamplesTraceProperties);

                    /* Current business process only supports ordering of the English
                        translation of the sample queries.
                        */

                    sourceMsg = $"Return sample queries list for locale '{locale}' from Azure Blob resource";

                    return DeserializeSamplesList(jsonFileContents, locale);
                });

                _telemetryClient?.TrackTrace(sourceMsg,
                                            SeverityLevel.Information,
                                            SamplesTraceProperties);

                return sampleQueriesList;
            }
        }

        /// <summary>
        /// Fetches the sample query files from Github and returns a deserialized instance of a
        /// <see cref="SampleQueriesList"/> from this.
        /// </summary>
        /// <param name="locale">The language code for the preferred localized file.</param>
        /// <param name="org">The name of the organisation i.e microsoftgraph or a member's username in the case of a forked repo</param>
        /// <param name="branchName">The name of the branch</param>
        /// <returns>The deserialized instance of a <see cref="SampleQueriesList"/>.</returns>
        public async Task<SampleQueriesList> FetchSampleQueriesListAsync(string locale, string org, string branchName)
        {
            _telemetryClient?.TrackTrace($"Retrieving sample queries list for locale '{locale}' from GitHub repository.",
                                         SeverityLevel.Information,
                                         SamplesTraceProperties);

            string host = _configuration["BlobStorage:GithubHost"];
            string repo = _configuration["BlobStorage:RepoName"];

            // Fetch the requisite sample path source based on the locale
            string localizedFilePathSource = FileServiceHelper.GetLocalizedFilePathSource(_sampleQueriesContainerName, _sampleQueriesBlobName, locale);

            // Get the full file path from configuration and query param, then read from the file
            var queriesFilePathSource = string.Concat(host, org, repo, branchName, FileServiceConstants.DirectorySeparator, localizedFilePathSource);

            // Construct the http request message
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, queriesFilePathSource);

            string jsonFileContents = await _httpClientUtility.ReadFromDocumentAsync(httpRequestMessage);

            _telemetryClient?.TrackTrace($"Return sample queries list for locale '{locale}' from GitHub repository",
                                         SeverityLevel.Information,
                                         SamplesTraceProperties);

            return DeserializeSamplesList(jsonFileContents, locale);
        }

        /// <summary>
        /// Orders the English version of sample queries and deserializes the file contents
        /// </summary>
        /// <param name="fileContents">The json files to be deserialized.</param>
        /// <param name="locale">The language code for the preferred localized file.</param>
        /// <returns>The deserialized instance of a <see cref="SampleQueriesList"/>.</returns>
        private static SampleQueriesList DeserializeSamplesList(string fileContents, string locale)
        {
            /* Current business process only supports ordering of the English
               translation of the sample queries.
             */
            bool orderSamples = locale.Equals("en-us", StringComparison.OrdinalIgnoreCase);

            SampleQueriesList sampleQueriesList = SamplesService.DeserializeSampleQueriesList(fileContents, orderSamples);
            return sampleQueriesList;
        }
    }
}
