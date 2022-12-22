// -------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Common;
using FileService.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PermissionsService.Interfaces;
using PermissionsService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UriMatchingService;
using UtilityService;

namespace PermissionsService
{
    public class PermissionsStore : IPermissionsStore
    {
        
        private readonly IMemoryCache _cache;
        private readonly IFileUtility _fileUtility;
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly IConfiguration _configuration;
        private readonly TelemetryClient _telemetryClient;
        private readonly Dictionary<string, string> _permissionsTraceProperties =
            new() { { UtilityConstants.TelemetryPropertyKey_Permissions, nameof(PermissionsStore) } };
        private readonly Dictionary<string, string> _permissionsTracePropertiesWithSanitizeIgnore =
            new() { { UtilityConstants.TelemetryPropertyKey_Permissions, nameof(PermissionsStore) },
                    { UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(PermissionsStore) } };
        private readonly string _permissionsContainerName;
        private readonly List<string> _permissionsBlobNames;
        private readonly string _scopesInformation;
        private readonly int _defaultRefreshTimeInHours; // life span of the in-memory cache
        private const string DefaultLocale = "en-US"; // default locale language
        private readonly object _permissionsLock = new();
        private readonly object _scopesLock = new();
        private const string Delegated = "Delegated";
        private const string Application = "Application";
        private const string CacheRefreshTimeConfig = "FileCacheRefreshTimeInHours:Permissions";
        private const string ScopesInfoBlobConfig = "BlobStorage:Blobs:Permissions:Descriptions";
        private const string PermissionsNamesBlobConfig = "BlobStorage:Blobs:Permissions:Names";
        private const string PermissionsContainerBlobConfig = "BlobStorage:Containers:Permissions";
        private const string NullValueError = "Value cannot be null";


        private class PermissionsDataInfo
        {
            public UriTemplateMatcher UriTemplateMatcher
            {
                get; set;
            }
            public IDictionary<int, object> ScopesListTable 
            {
                get;set;
            }

        }
        public PermissionsStore(IConfiguration configuration, IHttpClientUtility httpClientUtility,
                                IFileUtility fileUtility, IMemoryCache permissionsCache, TelemetryClient telemetryClient = null)
        {
            _telemetryClient = telemetryClient;
            _configuration = configuration
               ?? throw new ArgumentNullException(nameof(configuration), $"{NullValueError}: {nameof(configuration)}");
            _cache = permissionsCache
                ?? throw new ArgumentNullException(nameof(permissionsCache), $"{NullValueError}: {nameof(permissionsCache)}");
            _httpClientUtility = httpClientUtility
                ?? throw new ArgumentNullException(nameof(httpClientUtility), $"{NullValueError}: {nameof(httpClientUtility)}");
            _fileUtility = fileUtility
                ?? throw new ArgumentNullException(nameof(fileUtility), $"{NullValueError}: {nameof(fileUtility)}");
            _permissionsContainerName = configuration[PermissionsContainerBlobConfig]
                ?? throw new ArgumentNullException(nameof(PermissionsContainerBlobConfig), $"Config path missing: {PermissionsContainerBlobConfig}");
            _permissionsBlobNames = configuration.GetSection(PermissionsNamesBlobConfig).Get<List<string>>()
                ?? throw new ArgumentNullException(nameof(PermissionsNamesBlobConfig), $"Config path missing: {PermissionsNamesBlobConfig}");
            _scopesInformation = configuration[ScopesInfoBlobConfig]
                ?? throw new ArgumentNullException(nameof(ScopesInfoBlobConfig), $"Config path missing: {ScopesInfoBlobConfig}");
            _defaultRefreshTimeInHours = FileServiceHelper.GetFileCacheRefreshTime(configuration[CacheRefreshTimeConfig]);

        }

        private PermissionsDataInfo PermissionsData
        {
            get
            {
                if (!_cache.TryGetValue<PermissionsDataInfo>("PermissionsData", out var permissionsData))
                {
                    lock (_permissionsLock)
                    {
                        permissionsData = LoadPermissionsData();
                        _cache.Set("PermissionsData", permissionsData, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours)
                        });
                    }
                }
                
                return permissionsData;
            }
         }

        /// <summary>
        /// Populates the template table with the request urls and the scopes table with the permission scopes.
        /// </summary>
        private PermissionsDataInfo LoadPermissionsData()
        {
            var urlTemplateMatcher = new UriTemplateMatcher();
            var scopesListTable = new Dictionary<int, object>();

            HashSet<string> uniqueRequestUrlsTable = new HashSet<string>();
            int count = 0;

            foreach (string permissionFilePath in _permissionsBlobNames)
            {
                _telemetryClient?.TrackTrace($"Seeding permissions table from file source '{permissionFilePath}'",
                                             SeverityLevel.Information,
                                             _permissionsTracePropertiesWithSanitizeIgnore);

                string relativePermissionPath = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, permissionFilePath);
                string jsonString = _fileUtility.ReadFromFile(relativePermissionPath).GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(jsonString))
                {
                    JObject permissionsObject = JObject.Parse(jsonString);

                    if (permissionsObject.Count < 1)
                    {
                        throw new InvalidOperationException("The permissions data sources cannot be empty." +
                            "Check the source file or check whether the file path is properly set. File path: " +
                            $"{relativePermissionPath}");
                    }

                    JToken apiPermissions = permissionsObject.First.First;

                    foreach (JProperty property in apiPermissions.OfType<JProperty>())
                    {
                        // Remove any '(...)' from the request url and set to lowercase for uniformity
                        string requestUrl = property.Name
                                                    .RemoveParentheses()
                                                    .ToLower();

                        if (uniqueRequestUrlsTable.Add(requestUrl))
                        {
                            count++;

                            // Add the request url
                            urlTemplateMatcher.Add(count.ToString(), requestUrl);

                            // Add the permission scopes
                            scopesListTable.Add(count, property.Value);
                        }
                    }
                }
            }

            _telemetryClient?.TrackTrace("Finished seeding permissions tables",
                                         SeverityLevel.Information,
                                         _permissionsTraceProperties);
            return new PermissionsDataInfo()
            {
                UriTemplateMatcher = urlTemplateMatcher,
                ScopesListTable = scopesListTable
            };
        }

        /// <summary>
        /// Gets or creates the localized permissions descriptions from the cache.
        /// </summary>
        /// <param name="locale">The locale of the permissions decriptions file.</param>
        /// <returns>The localized instance of permissions descriptions.</returns>
        private async Task<IDictionary<string, IDictionary<string, ScopeInformation>>> GetOrCreatePermissionsDescriptionsAsync(string locale = DefaultLocale)
        {
            _telemetryClient?.TrackTrace($"Retrieving permissions for locale '{locale}' from in-memory cache 'ScopesInfoList_{locale}'",
                                         SeverityLevel.Information,
                                         _permissionsTraceProperties);

            var scopesInformationDictionary = await _cache.GetOrCreateAsync($"ScopesInfoList_{locale}", cacheEntry =>
            {
                _telemetryClient?.TrackTrace($"In-memory cache 'ScopesInfoList_{locale}' empty. " +
                                             $"Seeding permissions for locale '{locale}' from Azure blob resource",
                                             SeverityLevel.Information,
                                             _permissionsTraceProperties);

                /* Localized copy of permissions descriptions
                   is to be seeded by only one executing thread.
                */
                lock (_scopesLock)
                {
                    /* Check whether a previous thread already seeded an
                     * instance of the localized permissions descriptions
                     * during the lock.
                     */
                    var seededScopesInfoDictionary = _cache.Get<IDictionary<string, IDictionary<string, ScopeInformation>>>($"ScopesInfoList_{locale}");
                    var sourceMsg = $"Return locale '{locale}' permissions from in-memory cache 'ScopesInfoList_{locale}'";

                    if (seededScopesInfoDictionary == null)
                    {
                        string relativeScopesInfoPath = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, _scopesInformation, locale);

                        // Get file contents from source
                        string scopesInfoJson = _fileUtility.ReadFromFile(relativeScopesInfoPath).GetAwaiter().GetResult();
                        _telemetryClient?.TrackTrace($"Successfully seeded permissions for locale '{locale}' from Azure blob resource, file length: {scopesInfoJson.Length}",
                                                     SeverityLevel.Information,
                                                     _permissionsTraceProperties);

                        seededScopesInfoDictionary = CreateScopesInformationTables(scopesInfoJson);
                        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);
                        sourceMsg = $"Return locale '{locale}' permissions from Azure blob resource";
                    }
                    else
                    {
                        _telemetryClient?.TrackTrace($"In-memory cache 'ScopesInfoList_{locale}' of permissions " +
                                                     $"already seeded by a concurrently running thread",
                                                     SeverityLevel.Information,
                                                     _permissionsTraceProperties);
                    }

                    _telemetryClient?.TrackTrace(sourceMsg,
                                                 SeverityLevel.Information,
                                                 _permissionsTraceProperties);

                    return Task.FromResult(seededScopesInfoDictionary);
                }
            });

            return scopesInformationDictionary;
        }

        /// <summary>
        /// Gets the permissions descriptions and their localized instances from DevX Content Repo.
        /// </summary>
        /// <param name="locale">The locale of the permissions descriptions file.</param>
        /// <param name="org">The org or owner of the repo.</param>
        /// <param name="branchName">The name of the branch with the file version.</param>
        /// <returns>The localized instance of permissions descriptions.</returns>
        private async Task<IDictionary<string, IDictionary<string, ScopeInformation>>> GetPermissionsDescriptionsFromGithub(string org,
                                                                                                                            string branchName,
                                                                                                                            string locale = DefaultLocale)
        {
            _telemetryClient?.TrackTrace($"Retrieving permissions for locale '{locale}' from GitHub repository",
                                         SeverityLevel.Information,
                                         _permissionsTraceProperties);

            string host = _configuration["BlobStorage:GithubHost"];
            string repo = _configuration["BlobStorage:RepoName"];

            string localizedFilePathSource = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, _scopesInformation, locale);

            // Get the absolute url from configuration and query param, then read from the file
            var queriesFilePathSource = string.Concat(host, org, repo, branchName, FileServiceConstants.DirectorySeparator, localizedFilePathSource);

            // Get file contents from source
            string scopesInfoJson = await FetchHttpSourceDocument(queriesFilePathSource);

            var scopesInformationDictionary = CreateScopesInformationTables(scopesInfoJson);

            _telemetryClient?.TrackTrace($"Return permissions for locale '{locale}' from GitHub repository",
                                         SeverityLevel.Information,
                                         _permissionsTraceProperties);

            return scopesInformationDictionary;
        }

        /// <summary>
        /// Fetches document from a Http source.
        /// </summary>
        /// <param name="sourceUri">The relative file path.</param>
        /// <returns>A document retrieved from the Http source.</returns>
        private async Task<string> FetchHttpSourceDocument(string sourceUri)
        {
            // Construct the http request message
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, sourceUri);

            return await _httpClientUtility.ReadFromDocumentAsync(httpRequestMessage);
        }

        /// <summary>
        /// Creates a dictionary of scopes information.
        /// </summary>
        /// <param name="filePath">The path of the file from Github.</param>
        /// <param name="cacheEntry">An optional cache entry param.</param>
        /// <returns>A dictionary of scopes information.</returns>
        private IDictionary<string, IDictionary<string, ScopeInformation>> CreateScopesInformationTables(string scopesInfoJson)
        {
            if (string.IsNullOrEmpty(scopesInfoJson))
            {
                _telemetryClient?.TrackTrace($"{nameof(scopesInfoJson)} empty or null when creating the scopes information tables",
                                             SeverityLevel.Error,
                                             _permissionsTraceProperties);
                return null;
            }

            _telemetryClient?.TrackTrace("Creating the scopes information tables",
                                         SeverityLevel.Information,
                                         _permissionsTraceProperties);

            var scopesInformationList = JsonConvert.DeserializeObject<ScopesInformationList>(scopesInfoJson);

            _telemetryClient?.TrackTrace($"DelegatedScopesList count: {scopesInformationList.DelegatedScopesList.Count}, ApplicationScopesList count: {scopesInformationList.ApplicationScopesList.Count}",
                                         SeverityLevel.Information,
                                         _permissionsTraceProperties);

            var delegatedScopesInfoTable = scopesInformationList.DelegatedScopesList.ToDictionary(x => x.ScopeName);
            var applicationScopesInfoTable = scopesInformationList.ApplicationScopesList.ToDictionary(x => x.ScopeName);

            _telemetryClient?.TrackTrace("Finished creating the scopes information tables. " +
                                         $"Delegated scopes count: {delegatedScopesInfoTable.Count}. " +
                                         $"Application scopes count: {applicationScopesInfoTable.Count}",
                                         SeverityLevel.Information,
                                         _permissionsTracePropertiesWithSanitizeIgnore);

            return new Dictionary<string, IDictionary<string, ScopeInformation>>
            {
                { Delegated, delegatedScopesInfoTable },
                { Application, applicationScopesInfoTable }
            };
        }

        ///<inheritdoc/>
        public async Task<List<ScopeInformation>> GetScopesAsync(string scopeType = "DelegatedWork",
                                                                 string locale = DefaultLocale,
                                                                 string requestUrl = null,
                                                                 string method = null,
                                                                 string org = null,
                                                                 string branchName = null)
        {


            IDictionary<string, IDictionary<string, ScopeInformation>> scopesInformationDictionary;
            var permissionsData = PermissionsData;
            if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(branchName))
            {
                // Creates a dict of scopes information from GitHub files
                scopesInformationDictionary = await GetPermissionsDescriptionsFromGithub(org, branchName, locale);
            }
            else
            {
                // Creates a dict of scopes information from cached files
                scopesInformationDictionary = await GetOrCreatePermissionsDescriptionsAsync(locale);
            }

            var scopesInfo = new List<ScopeInformation>();

            if (string.IsNullOrEmpty(requestUrl))  // fetch all permissions
            {
                _telemetryClient?.TrackTrace("Fetching all permissions",
                                             SeverityLevel.Information,
                                             _permissionsTraceProperties);

                var scopes = permissionsData.ScopesListTable.Values.OfType<JToken>()
                                                    .SelectMany(x => x).OfType<JProperty>()
                                                    .SelectMany(x => x.Value).OfType<JProperty>()
                                                    .Where(x => x.Name.Equals(scopeType))
                                                    .SelectMany(x => x.Value)
                                                    .Values<string>().Distinct().ToList();
                scopes.Remove("N/A");

                scopesInfo = GetScopesInformation(scopesInformationDictionary, scopes, scopeType, true);

                _telemetryClient?.TrackTrace("Return all permissions",
                                             SeverityLevel.Information,
                                             _permissionsTraceProperties);
            }
            else // fetch permissions for a given request url and method
            {
                if (string.IsNullOrEmpty(method))
                {
                    throw new ArgumentNullException(nameof(method), "The HTTP method value cannot be null or empty.");
                }

                requestUrl = CleanRequestUrl(requestUrl);

                // Check if requestUrl is contained in our Url Template table
                TemplateMatch resultMatch = permissionsData.UriTemplateMatcher.Match(new Uri(requestUrl, UriKind.RelativeOrAbsolute));

                if (resultMatch is null)
                {
                    _telemetryClient?.TrackTrace($"Url '{requestUrl}' not found",
                                                 SeverityLevel.Error,
                                                 _permissionsTraceProperties);

                    return null;
                }

                if (int.TryParse(resultMatch.Key, out int key) is false)
                {
                    throw new InvalidOperationException($"Failed to parse '{resultMatch.Key}' to int.");
                }

                if (permissionsData.ScopesListTable[key] is not JToken resultValue)
                {
                    _telemetryClient?.TrackTrace($"Key '{permissionsData.ScopesListTable[key]}' in the {nameof(permissionsData.ScopesListTable)} has a null value.",
                                                 SeverityLevel.Error,
                                                 _permissionsTraceProperties);

                    return null;
                }

                var scopes = resultValue.OfType<JProperty>()
                                        .Where(x => method.Equals(x.Name, StringComparison.OrdinalIgnoreCase))
                                        .SelectMany(x => x.Value).OfType<JProperty>()
                                        .FirstOrDefault(x => scopeType.Equals(x.Name, StringComparison.OrdinalIgnoreCase))
                                        ?.Value?.ToObject<List<string>>().Distinct().ToList();

                if (scopes is null)
                {
                    _telemetryClient?.TrackTrace($"No '{scopeType}' permissions found for the url '{requestUrl}' and method '{method}'",
                                                 SeverityLevel.Error,
                                                 _permissionsTraceProperties);

                    return null;
                }

                scopesInfo = GetScopesInformation(scopesInformationDictionary, scopes, scopeType);

                _telemetryClient?.TrackTrace($"Return '{scopeType}' permissions for url '{requestUrl}' and method '{method}'",
                                             SeverityLevel.Information,
                                             _permissionsTraceProperties);
            }

            return scopesInfo;
        }



        /// <summary>
        /// Cleans up the request url by applying string formatting operations
        /// on the target value in line with the expected standardized output value.
        /// </summary>
        /// <remarks>The expected standardized output value is the request url value
        /// format as captured in the permissions doc. This is to ensure efficacy
        /// of the uri template matching.</remarks>
        /// <param name="requestUrl">The target request url string value.</param>
        /// <returns>The target request url formatted to the expected standardized
        /// output value.</returns>
        private static string CleanRequestUrl(string requestUrl)
        {
            if (string.IsNullOrEmpty(requestUrl))
            {
                return requestUrl;
            }

            requestUrl = requestUrl.BaseUriPath() // remove any query params
                                   .UriTemplatePathFormat(true)
                                   .RemoveParentheses();

            /* Remove ${value} segments from paths,
             * ex: /me/photo/$value --> $value or /applications/{application-id}/owners/$ref --> $ref
             * Because these segments are not accounted for in the permissions doc.
             * ${value} segments will always appear as the last segment in a path.
            */
            return Regex.Replace(requestUrl, @"(\$.*)", string.Empty)
                        .TrimEnd('/')
                        .ToLowerInvariant();
        }

        /// <summary>
        /// Retrieves the scopes information for a given list of scopes.
        /// </summary>
        /// <param name="scopesInformationDictionary">The source of the scopes information.</param>
        /// <param name="scopes">The target list of scopes.</param>
        /// <param name="scopeType">The type of scope from which to retrieve the scopes information for.</param>
        /// <param name="getAllPermissions">Optional: Whether to return all available permissions for a given <paramref name="scopeType"/>.</param>
        /// <returns>A list of <see cref="ScopeInformation"/>.</returns>
        private List<ScopeInformation> GetScopesInformation(IDictionary<string, IDictionary<string, ScopeInformation>> scopesInformationDictionary,
                                                                   List<string> scopes,
                                                                   string scopeType,
                                                                   bool getAllPermissions = false)
        {
            if (scopesInformationDictionary is null)
            {
                throw new ArgumentNullException(nameof(scopesInformationDictionary));
            }

            if (scopes is null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            if (string.IsNullOrEmpty(scopeType))
            {
                throw new ArgumentException($"'{nameof(scopeType)}' cannot be null or empty.", nameof(scopeType));
            }

            var key = scopeType.Contains(Delegated, StringComparison.InvariantCultureIgnoreCase) ? Delegated : Application;
            if (!scopesInformationDictionary[key].Values.Any())
            {
                var errMsg = $"{nameof(scopesInformationDictionary)}:[{key}] has no values.";
                _telemetryClient?.TrackTrace(errMsg,
                                             SeverityLevel.Error,
                                             _permissionsTraceProperties);
            }

            var scopesInfo = scopes.Select(scope =>
            {
                scopesInformationDictionary[key].TryGetValue(scope, out var scopeInfo);                
                return scopeInfo ?? new() { ScopeName = scope };
            }).ToList();

            if (getAllPermissions)
            {
                scopesInfo.AddRange(scopesInformationDictionary[key]
                          .Where(kvp => !scopesInfo.Exists(x => x.ScopeName.Equals(kvp.Value.ScopeName, StringComparison.OrdinalIgnoreCase)))
                          .Select(kvp => kvp.Value));
            }

            return scopesInfo;
        }

        ///<inheritdoc/>
        public UriTemplateMatcher GetUriTemplateMatcher()
        {
            return PermissionsData.UriTemplateMatcher;
        }
    }
}
