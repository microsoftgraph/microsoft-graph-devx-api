// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Common;
using ChangesService.Interfaces;
using ChangesService.Models;
using FileService.Common;
using FileService.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChangesService.Services
{
    /// <summary>
    /// Provides a <see cref="ChangeLog"/> list from cache or uri source.
    /// </summary>
    public class ChangesStore : IChangesStore
    {
        private readonly object _changesLock = new object();
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly IMemoryCache _changeLogCache;
        private readonly IConfiguration _configuration;
        private readonly string _changeLogRelativeUrl;
        private readonly int _defaultRefreshTimeInHours;

        public ChangesStore(IConfiguration configuration, IMemoryCache changeLogCache, IHttpClientUtility httpClientUtility)
        {
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
        /// Fetches a <see cref="ChangeLog"/> list from a uri source or an in-memory cache.
        /// </summary>
        /// <param name="locale">The language code for the preferred localized file.</param>
        /// <returns>A <see cref="ChangeLogList"/> containing the list of <see cref="ChangeLog"/>.</returns>
        public async Task<ChangeLogList> FetchChangeLogListAsync(string locale)
        {
            if (string.IsNullOrEmpty(locale) || locale.Equals("en-gb", StringComparison.OrdinalIgnoreCase))
            {
                // Default file locale is en-us
                // en-gb is not supported and should default to en-us
                locale = "en-us";
            }

            locale = locale.ToLowerInvariant(); // for uniformity; uri path is all lower-cased

            // Fetch cached changelog list
            ChangeLogList changeLogList = await _changeLogCache.GetOrCreateAsync(locale, cacheEntry =>
            {
                lock (_changesLock)
                {
                    /* Check whether a previous thread already seeded an
                     * instance of the localized changelog list during the lock.
                     */
                    var lockedLocale = locale;
                    var seededChangeLogList = _changeLogCache.Get<ChangeLogList>(lockedLocale);

                    if (seededChangeLogList != null)
                    {
                        // Already seeded by another thread
                        return Task.FromResult(seededChangeLogList);
                    }

                    // Set cache expiry
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);

                    // Construct the locale-specific relative uri
                    string relativeUrl = string.Format(_changeLogRelativeUrl, locale);

                    // Append to get the absolute uri
                    string requestUri = _configuration[ChangesServiceConstants.ChangelogBaseUrlConfigPath]
                                                        + relativeUrl;

                    // Construct the http request message
                    using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

                    // Get the file contents from source
                    string jsonFileContents = _httpClientUtility.ReadFromDocument(httpRequestMessage)
                                                .GetAwaiter().GetResult();

                    // Return the changelog list from the file contents
                    return Task.FromResult(ChangesService.DeserializeChangeLogList(jsonFileContents));
                }
            });

            return changeLogList;
        }
    }
}
