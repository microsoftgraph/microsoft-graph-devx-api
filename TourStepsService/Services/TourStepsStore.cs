// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UtilityService;
using TourStepsService.Interfaces;
using FileService.Common;
using FileService.Interfaces;
using TourStepsService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;


namespace TourStepsService.Services
{
    /// <summary>
    /// Retrieves or adds localized tour steps content to and from a memory cache or a remote source.
    /// </summary>
    public class TourStepsStore : ITourStepsStore
    {
        private readonly object _tourStepsLock = new();
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly IConfiguration _configuration;
        private const string NullValueError = "Value cannot be null";
        private readonly IFileUtility _fileUtility;
        private readonly string _tourStepsContainerName;
        private readonly string _tourStepsBlobName;
        private readonly IMemoryCache _tourStepsCache;
        private readonly int _defaultRefreshTimeInHours;
        private readonly TelemetryClient _telemetryClient;
        private readonly Dictionary<string, string> _tourStepsTraceProperties =
            new(){ { UtilityConstants.TelemetryPropertyKey_TourSteps, nameof(TourStepsStore)} };


        public TourStepsStore(IConfiguration configuration, IHttpClientUtility httpClientUtility, IFileUtility fileUtility,
            IMemoryCache tourStepsCache, TelemetryClient telemetryClient = null)
        {
            _httpClientUtility = httpClientUtility;
            _telemetryClient = telemetryClient;
            _configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration), $"{ NullValueError }: { nameof(configuration) }");
            _fileUtility = fileUtility
                ?? throw new ArgumentNullException(nameof(fileUtility), $"{ NullValueError }: { nameof(fileUtility) }");
            _tourStepsContainerName = _configuration["BlobStorage:Containers:TourSteps"];
            _tourStepsBlobName = _configuration["BlobStorage:Blobs:TourSteps"];
            _tourStepsCache = tourStepsCache
                ?? throw new ArgumentNullException(nameof(tourStepsCache), $"{ NullValueError }: { nameof(tourStepsCache) }");
            _defaultRefreshTimeInHours = FileServiceHelper.GetFileCacheRefreshTime(configuration["FileCacheRefreshTimeInHours:TourSteps"]);
        }

        /// <summary>
        /// Fetches the tour steps from the cache or a JSON file and returns a deserialized instance of a
        /// <see cref="TourStepsList"/> from this.
        /// </summary>
        /// <param name="locale">The language code for the preferred localized file.</param>
        /// <returns>The deserialized instance of a <see cref="TourStepsList"/>.</returns>

        public async Task<TourStepsList> FetchTourStepsListAsync(string locale)
        {
            _telemetryClient?.TrackTrace($"Retrieving tour steps list from locale '{locale}' from in-memory cache '{locale}'",
                                         SeverityLevel.Information,
                                         _tourStepsTraceProperties);
            string sourceMsg = $"Return tour steps list for locale '{locale}' from in-memory cache '{locale}'";

            // Localized copy of steps is to be seeded by only one executing thread.
            var tourStepsList = await _tourStepsCache.GetOrCreateAsync(locale, cacheEntry =>
            {
                lock (_tourStepsLock)
                {
                    /* Check whether a previous thread already seeded an
                     * instance of the localized steps during the lock.
                     */
                    string lockedLocale = locale;
                    var seededTourStepsList = _tourStepsCache?.Get<TourStepsList>(lockedLocale);

                    if (seededTourStepsList != null)
                    {
                        _telemetryClient?.TrackTrace($"In-memory cache '{lockedLocale}' of tour steps list " +
                                                    $"already seeded by a concurrently running thread",
                                                    SeverityLevel.Information,
                                                    _tourStepsTraceProperties);
                        sourceMsg = $"Return tour steps list for locale '{lockedLocale}' from in-memory cache '{lockedLocale}'";
                        return Task.FromResult(seededTourStepsList);
                    }

                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);

                    // Fetch the requisite steps path source based on the locale
                    var queriesFilePathSource =
                       FileServiceHelper.GetLocalizedFilePathSource(_tourStepsContainerName, _tourStepsBlobName, lockedLocale);

                    // Get the file contents from source
                    var jsonFileContents = _fileUtility.ReadFromFile(queriesFilePathSource).GetAwaiter().GetResult();

                    _telemetryClient?.TrackTrace($"Successfully seeded tour steps list for locale '{lockedLocale}' from Azure Blob resource",
                                                SeverityLevel.Information,
                                                _tourStepsTraceProperties);

                    return Task.FromResult(DeserializeTourStepsList(jsonFileContents));
                }
            });

            _telemetryClient?.TrackTrace(sourceMsg,
                                        SeverityLevel.Information,
                                        _tourStepsTraceProperties);
            return tourStepsList;
        }

        /// <summary>
        /// Fetches the tour steps files from Github and returns a deserialized instance of a
        /// <see cref="TourStepsList"/> from this.
        /// </summary>
        /// <param name="locale">The language code for the preferred localized file.</param>
        /// <param name="org">The name of the organisation i.e microsoftgraph or a member's username in the case of a forked repo</param>
        /// <param name="branchName">The name of the branch</param>
        /// <returns>The deserialized instance of a <see cref="TourStepsList"/>.</returns>

        public async Task<TourStepsList> FetchTourStepsListAsync(string locale, string org, string branchName)
        {
            _telemetryClient?.TrackTrace($"Retrieving tour steps list for locale '{locale}' from GitHub repository.",
                                        SeverityLevel.Information,
                                        _tourStepsTraceProperties);

            var host = _configuration["BlobStorage:GithubHost"];
            var repo = _configuration["BlobStorage:RepoName"];

            // Fetch the requisite sample path source based on the locale
            var localizedFilePathSource = FileServiceHelper.GetLocalizedFilePathSource(_tourStepsContainerName, _tourStepsBlobName, locale);
            var queriesFilePathSource = string.Concat(host, org, repo, branchName, FileServiceConstants.DirectorySeparator, localizedFilePathSource);


            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, queriesFilePathSource);
            var jsonFileContents = await _httpClientUtility.ReadFromDocumentAsync(httpRequestMessage);

            _telemetryClient?.TrackTrace($"Return tour steps list for locale '{locale}' from GitHub repository",
                                        SeverityLevel.Information,
                                        _tourStepsTraceProperties);

            return DeserializeTourStepsList(jsonFileContents);
        }

        /// <summary>
        /// returns deserialized tour steps
        /// </summary>
        /// <param name="fileContents">The json files to be deserialized.</param>
        /// <returns>The deserialized instance of a <see cref="TourStepsList"/>.</returns>
        private static TourStepsList DeserializeTourStepsList(string fileContents)
        {
            return TourStepsService.DeserializeTourStepsList(fileContents);
        }
    }
}
