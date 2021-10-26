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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UtilityService;

namespace ChangesService.Services
{
    /// <summary>
    /// Provides changelog documents from cache or uri source.
    /// </summary>
    public class ChangesStore : IChangesStore
    {
        private readonly object _changesLock = new();
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly IFileUtility _fileUtility;
        private readonly IMemoryCache _changeLogCache;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, string> _changesTraceProperties =
                        new() { { UtilityConstants.TelemetryPropertyKey_Changes, nameof(ChangesStore) } };
        private readonly string _changeLogRelativeUrl;
        private readonly int _defaultRefreshTimeInHours;
        private readonly TelemetryClient _telemetryClient;
        private readonly IChangesService _changesService;
        private readonly string _workloadMappingContainerName;
        private readonly string _workloadMappingBlobName;
        private const string WorkloadMappingContainerConfig = "BlobStorage:Containers:Changelog";
        private const string WorkloadMappingBlobConfig = "BlobStorage:Blobs:WorkloadMapping";

        public ChangesStore(IConfiguration configuration, IMemoryCache changeLogCache, IChangesService changesService,
                            IHttpClientUtility httpClientUtility, IFileUtility fileUtility = null,
                            TelemetryClient telemetryClient = null)
        {
            _telemetryClient = telemetryClient;
            _changesService = changesService ?? throw new ArgumentNullException(nameof(changesService),
                $"{ ChangesServiceConstants.ValueNullError }: { nameof(changesService) }");
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration),
                $"{ ChangesServiceConstants.ValueNullError }: { nameof(configuration) }");
            _changeLogCache = changeLogCache ?? throw new ArgumentNullException(nameof(changeLogCache),
                $"{ ChangesServiceConstants.ValueNullError }: { nameof(changeLogCache) }");
            _httpClientUtility = httpClientUtility ?? throw new ArgumentNullException(nameof(httpClientUtility),
                $"{ChangesServiceConstants.ValueNullError}: { nameof(httpClientUtility) }");
            _fileUtility = fileUtility
                ?? throw new ArgumentNullException(nameof(fileUtility), $"{ ChangesServiceConstants.ValueNullError }: { nameof(fileUtility) }");
            _changeLogRelativeUrl = configuration[ChangesServiceConstants.ChangelogRelativeUrlConfigPath]
                ?? throw new ArgumentNullException(nameof(ChangesServiceConstants.ChangelogRelativeUrlConfigPath), "Config path missing");
            _workloadMappingContainerName = configuration[WorkloadMappingContainerConfig]
                ?? throw new ArgumentNullException(nameof(WorkloadMappingContainerConfig), $"Config path missing: { WorkloadMappingContainerConfig }");
            _workloadMappingBlobName = configuration[WorkloadMappingBlobConfig]
                ?? throw new ArgumentNullException(nameof(WorkloadMappingBlobConfig), $"Config path missing: { WorkloadMappingBlobConfig }");
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
                    _telemetryClient?.TrackTrace(sourceMsg,
                                                 SeverityLevel.Information,
                                                 _changesTraceProperties);

                    if (seededChangeLogRecords != null)
                    {
                        _telemetryClient?.TrackTrace($"In-memory cache '{lockedLocale}' of changelog records " +
                                                     $"already seeded by a concurrently running thread",
                                                     SeverityLevel.Information,
                                                     _changesTraceProperties);
                        sourceMsg = $"Return changelog records for locale '{lockedLocale}' from in-memory cache '{lockedLocale}'";
                        _telemetryClient?.TrackTrace(sourceMsg,
                                                    SeverityLevel.Information,
                                                    _changesTraceProperties);
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

        /// <summary>
        /// Gets or creates workload service mappings.
        /// </summary>
        /// <returns>The localized instance of permissions descriptions.</returns>
        public async Task<Dictionary<string, string>> FetchWorkloadServiceMappingsAsync()
        {
            _telemetryClient?.TrackTrace($"Retrieving workload service mappings from in-memory cache 'WorkloadServiceMappings'",
                                         SeverityLevel.Information,
                                         _changesTraceProperties);

            var workloadServiceMappings = await _changeLogCache.GetOrCreateAsync("WorkloadServiceMappings", cacheEntry =>
            {
                _telemetryClient?.TrackTrace($"In-memory cache 'WorkloadServiceMappings' empty. " +
                                             $"Fetching the workload-mapping.json file from Azure blob resource",
                                             SeverityLevel.Information,
                                             _changesTraceProperties);

                // Ensure workload-mapping.json file is fetched by only one executing thread.
                lock (_changesLock)
                {
                    /* Check whether a previous thread already fetched a
                     * copy of the file during the lock.
                     */
                    var seededWorkloadServiceMappings = _changeLogCache.Get<Dictionary<string, string>>("WorkloadServiceMappings");
                    var sourceMsg = $"Return workload mapping from in-memory cache 'WorkloadServiceMappings'";

                    if (seededWorkloadServiceMappings == null)
                    {
                        string relativeSourcePath = FileServiceHelper.GetLocalizedFilePathSource(_workloadMappingContainerName, _workloadMappingBlobName);

                        // Get file contents from source
                        string sourceJson = _fileUtility.ReadFromFile(relativeSourcePath).GetAwaiter().GetResult();
                        _telemetryClient?.TrackTrace($"Successfully fetched workload-mapping.json file from Azure blob resource",
                                                     SeverityLevel.Information,
                                                     _changesTraceProperties);

                        seededWorkloadServiceMappings = CreateWorkloadServiceMappings(sourceJson);
                        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);
                        sourceMsg = $"Return workload-mapping.json file from Azure blob resource";
                    }
                    else
                    {
                        _telemetryClient?.TrackTrace($"In-memory cache 'WorkloadServiceMappings' " +
                                                     $"already seeded by a concurrently running thread",
                                                     SeverityLevel.Information,
                                                     _changesTraceProperties);
                    }

                    _telemetryClient?.TrackTrace(sourceMsg,
                                                 SeverityLevel.Information,
                                                 _changesTraceProperties);

                    return Task.FromResult(seededWorkloadServiceMappings);
                }
            });

            return workloadServiceMappings;
        }

        /// <summary>
        /// Creates the workload service mappings dictionary.
        /// </summary>
        /// <param name="sourceJson">The JSON string of the workload-mappings file.</param>
        /// <returns>A dictionary of workload service mappings.</returns>
        private static Dictionary<string, string> CreateWorkloadServiceMappings(string sourceJson)
        {
            var workloadMappings = JsonConvert.DeserializeObject<JObject>(sourceJson)
                     .Value<JObject>("workloadMappings")
                     .ToObject<Dictionary<string, JObject>>();

            var workloadServiceMappings = new Dictionary<string, string>();
            foreach (var token in workloadMappings)
            {
                var workloadIds = token.Value.Properties()
                    .Where(x => x.Name.Equals("workloads", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(x => x.Value).OfType<JObject>()
                    .Properties().Where(x => x.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Value)
                    .Values<string>().Distinct().ToList();

                foreach (var id in workloadIds)
                {
                    workloadServiceMappings.TryAdd(id, token.Key);
                }
            }

            return workloadServiceMappings;
        }
    }
}
