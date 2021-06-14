// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Common;
using FileService.Interfaces;
using GraphExplorerSamplesService.Interfaces;
using GraphExplorerSamplesService.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UtilityService;

namespace GraphExplorerSamplesService.Services
{
    /// <summary>
    /// Retrieves or adds localized sample queries to and from a memory cache or a remote source.
    /// </summary>
    public class SamplesStore : ISamplesStore
    {
        private readonly object _samplesLock = new object();
        private readonly IFileUtility _fileUtility;
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly IMemoryCache _samplesCache;
        private readonly IConfiguration _configuration;
        private readonly TelemetryClient _telemetry;
        private readonly Dictionary<string, string> SamplesTraceProperties =
            new() { { UtilityConstants.TelemetryPropertyKey_Samples, "SamplesStore" } };
        private readonly string _sampleQueriesContainerName;
        private readonly string _sampleQueriesBlobName;
        private readonly int _defaultRefreshTimeInHours;
        private const string NullValueError = "Value cannot be null";

        public SamplesStore(IConfiguration configuration, IHttpClientUtility httpClientUtility,
                            IFileUtility fileUtility, IMemoryCache samplesCache,
                            TelemetryClient telemetry = null)
        {
            _telemetry = telemetry;
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
            _telemetry?.TrackTrace($"Retrieving sample queries list for locale '{locale}' from in-memory cache '{locale}'",
                                   SeverityLevel.Information,
                                   SamplesTraceProperties);

            string sourceMsg = $"Return sample queries list for locale '{locale}' from in-memory cache '{locale}'";

            // Fetch cached sample queries
            SampleQueriesList sampleQueriesList = await _samplesCache.GetOrCreateAsync(locale, cacheEntry =>
            {
                _telemetry?.TrackTrace($"In-memory cache '{locale}' empty. " +
                                       $"Seeding sample queries list from Azure Blob resource",
                                       SeverityLevel.Information,
                                       SamplesTraceProperties);

                // Localized copy of samples is to be seeded by only one executing thread.
                lock (_samplesLock)
                {
                    /* Check whether a previous thread already seeded an
                     * instance of the localized samples during the lock.
                     */
                    var lockedLocale = locale;
                    var seededSampleQueriesList = _samplesCache?.Get<SampleQueriesList>(lockedLocale);

                    if (seededSampleQueriesList != null)
                    {
                        _telemetry?.TrackTrace($"In-memory cache '{lockedLocale}' of sample queries list " +
                                               $"already seeded by a concurrently running thread",
                                               SeverityLevel.Information,
                                               SamplesTraceProperties);
                        sourceMsg = $"Return sample queries list for locale '{lockedLocale}' from in-memory cache '{lockedLocale}'";

                        return Task.FromResult(seededSampleQueriesList);
                    }

                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);

                    // Fetch the requisite sample path source based on the locale
                    string queriesFilePathSource =
                       FileServiceHelper.GetLocalizedFilePathSource(_sampleQueriesContainerName, _sampleQueriesBlobName, lockedLocale);

                    // Get the file contents from source
                    string jsonFileContents = _fileUtility.ReadFromFile(queriesFilePathSource).GetAwaiter().GetResult();

                    _telemetry?.TrackTrace($"Successfully seeded sample queries list for locale '{lockedLocale}' from Azure Blob resource",
                                           SeverityLevel.Information,
                                           SamplesTraceProperties);

                    /* Current business process only supports ordering of the English
                       translation of the sample queries.
                     */
                    bool orderSamples = lockedLocale.Equals("en-us", StringComparison.OrdinalIgnoreCase);

                    sourceMsg = $"Return sample queries list for locale '{lockedLocale}' from Azure Blob resource";

                    return Task.FromResult(DeserializeSamplesList(jsonFileContents, locale));
                }
            });

            _telemetry?.TrackTrace(sourceMsg,
                                   SeverityLevel.Information,
                                   SamplesTraceProperties);

            return sampleQueriesList;
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
            _telemetry?.TrackTrace($"Retrieving sample queries list for locale '{locale}' from GitHub repository.",
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

            _telemetry?.TrackTrace($"Return sample queries list for locale '{locale}' from GitHub repository",
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
        private SampleQueriesList DeserializeSamplesList(string fileContents, string locale)
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
