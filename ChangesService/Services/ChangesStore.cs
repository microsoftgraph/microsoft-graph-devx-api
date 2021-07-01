// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Common;
using ChangesService.Interfaces;
using ChangesService.Models;
using FileService.Common;
using FileService.Extensions;
using FileService.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using UtilityService;

namespace ChangesService.Services
{
    /// <summary>
    /// Provides <see cref="ChangeLogRecords"/> from cache or uri source.
    /// </summary>
    public class ChangesStore : IChangesStore
    {
        private readonly object _changesLock = new object();
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly IMemoryCache _changeLogCache;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, string> _changesTraceProperties =
                        new() { { UtilityConstants.TelemetryPropertyKey_Changes, nameof(ChangesStore) } };
        private readonly string _changeLogRelativeUrl;
        private readonly int _defaultRefreshTimeInHours;
        private readonly TelemetryClient _telemetryClient;
        private readonly IChangesService _changesService;

        public ChangesStore(IConfiguration configuration, IMemoryCache changeLogCache, IChangesService changesService,
                            IHttpClientUtility httpClientUtility, TelemetryClient telemetryClient = null)
        {
            _telemetryClient = telemetryClient;
            _changesService = changesService ?? throw new ArgumentNullException(nameof(changesService),
                $"{ ChangesServiceConstants.ValueNullError }: { nameof(changesService) }"); ;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration),
                $"{ ChangesServiceConstants.ValueNullError }: { nameof(configuration) }");
            _changeLogCache = changeLogCache ?? throw new ArgumentNullException(nameof(changeLogCache),
                $"{ ChangesServiceConstants.ValueNullError }: { nameof(changeLogCache) }");
            _httpClientUtility = httpClientUtility ?? throw new ArgumentNullException(nameof(httpClientUtility),
                $"{ChangesServiceConstants.ValueNullError}: { nameof(httpClientUtility) }");
            _changeLogRelativeUrl = configuration[ChangesServiceConstants.ChangelogRelativeUrlConfigPath]
                ?? throw new ArgumentNullException(nameof(ChangesServiceConstants.ChangelogRelativeUrlConfigPath), "Config path missing");
            _defaultRefreshTimeInHours = FileServiceHelper.GetFileCacheRefreshTime(configuration[ChangesServiceConstants.ChangelogRefreshTimeConfigPath]);
        }

        /// <summary>
        /// Fetches <see cref="ChangeLogRecords"/> from a uri source or an in-memory cache.
        /// </summary>
        /// <param name="cultureInfo">The culture of the localized file to be retrieved.</param>
        /// <returns><see cref="ChangeLogRecords"/> containing entries of <see cref="ChangeLog"/>.</returns>
        public async Task<ChangeLogRecords> FetchChangeLogRecordsAsync(CultureInfo cultureInfo)
        {
            var locale = cultureInfo.GetSupportedLocaleVariant().ToLower(); // lowercased in line with source files

            _telemetryClient?.TrackTrace($"Retrieving changelog records for locale '{locale}' from in-memory cache '{locale}'",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);

            // Fetch cached changelog records
            ChangeLogRecords changeLogRecords = await _changeLogCache.GetOrCreateAsync(locale, cacheEntry =>
            {
                _telemetryClient?.TrackTrace($"In-memory cache '{locale}' empty. " +
                                             $"Seeding changelog records for locale '{locale}' from Azure blob resource",
                                             SeverityLevel.Information,
                                             _changesTraceProperties);

                // Localized copy of changes is to be seeded by only one executing thread.
                lock (_changesLock)
                {
                    /* Check whether a previous thread already seeded an
                     * instance of the localized changelog records during the lock.
                     */
                    var lockedLocale = locale;
                    var seededChangeLogRecords = _changeLogCache.Get<ChangeLogRecords>(lockedLocale);
                    var sourceMsg = $"Return locale '{locale}' changelog records from in-memory cache";

                    if (seededChangeLogRecords != null)
                    {
                        _telemetryClient?.TrackTrace($"In-memory cache '{lockedLocale}' of changelog records " +
                                                     $"already seeded by a concurrently running thread",
                                                     SeverityLevel.Information,
                                                     _changesTraceProperties);
                        sourceMsg = $"Return changelog records for locale '{lockedLocale}' from in-memory cache '{lockedLocale}'";

                        // Already seeded by another thread
                        return Task.FromResult(seededChangeLogRecords);
                    }

                    // Set cache expiry
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);

                    // Construct the locale-specific relative uri
                    var relativeUrl = string.Format(_changeLogRelativeUrl, lockedLocale);

                    // Append to get the absolute uri
                    var requestUri = _configuration[ChangesServiceConstants.ChangelogBaseUrlConfigPath]
                                                        + relativeUrl;

                    // Construct the http request message
                    using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

                    // Get the file contents from source
                    var jsonFileContents = _httpClientUtility.ReadFromDocumentAsync(httpRequestMessage)
                                                .GetAwaiter().GetResult();

                    sourceMsg = $"Successfully seeded changelog records for locale '{lockedLocale}' from source";

                    _telemetryClient?.TrackTrace(sourceMsg,
                                                 SeverityLevel.Information,
                                                 _changesTraceProperties);

                    // Return the changelog records from the file contents
                    return Task.FromResult(_changesService.DeserializeChangeLogRecords(jsonFileContents));
                }
            });

            return changeLogRecords;
        }
    }
}
