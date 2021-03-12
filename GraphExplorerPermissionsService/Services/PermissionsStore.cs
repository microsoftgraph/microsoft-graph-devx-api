// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Common;
using FileService.Interfaces;
using GraphExplorerPermissionsService.Interfaces;
using GraphExplorerPermissionsService.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UriMatchingService;

namespace GraphExplorerPermissionsService
{
    public class PermissionsStore : IPermissionsStore
    {
        private UriTemplateMatcher _urlTemplateMatcher;
        private IDictionary<int, object> _scopesListTable;
        private readonly IMemoryCache _permissionsCache;
        private readonly IFileUtility _fileUtility;
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly IConfiguration _configuration;
        private readonly string _permissionsContainerName;
        private readonly List<string> _permissionsBlobNames;
        private readonly string _scopesInformation;
        private readonly int _defaultRefreshTimeInHours; // life span of the in-memory cache
        private const string DefaultLocale = "en-US"; // default locale language
        private readonly object _permissionsLock = new object();
        private readonly object _scopesLock = new object();
        private static bool _permissionsRefreshed = false;
        private const string Delegated = "Delegated";
        private const string Application = "Application";
        private const string CacheRefreshTimeConfig = "FileCacheRefreshTimeInHours";
        private const string ScopesInfoBlobConfig = "BlobStorage:Blobs:Permissions:Descriptions";
        private const string PermissionsNamesBlobConfig = "BlobStorage:Blobs:Permissions:Names";
        private const string PermissionsContainerBlobConfig = "BlobStorage:Containers:Permissions";
        private const string NullValueError = "Value cannot be null";

        public PermissionsStore(IConfiguration configuration, IHttpClientUtility httpClientUtility,
                                IFileUtility fileUtility, IMemoryCache permissionsCache)
        {
            _configuration = configuration
               ?? throw new ArgumentNullException(nameof(configuration), $"{ NullValueError }: { nameof(configuration) }");
            _permissionsCache = permissionsCache
                ?? throw new ArgumentNullException(nameof(permissionsCache), $"{ NullValueError }: { nameof(permissionsCache) }");
            _httpClientUtility = httpClientUtility
                ?? throw new ArgumentNullException(nameof(httpClientUtility), $"{ NullValueError }: { nameof(httpClientUtility) }");
            _fileUtility = fileUtility
                ?? throw new ArgumentNullException(nameof(fileUtility), $"{ NullValueError }: { nameof(fileUtility) }");
            _permissionsContainerName = configuration[PermissionsContainerBlobConfig]
                ?? throw new ArgumentNullException(nameof(PermissionsContainerBlobConfig), $"Config path missing: { PermissionsContainerBlobConfig }");
            _permissionsBlobNames = configuration.GetSection(PermissionsNamesBlobConfig).Get<List<string>>()
                ?? throw new ArgumentNullException(nameof(PermissionsNamesBlobConfig), $"Config path missing: { PermissionsNamesBlobConfig }");
            _scopesInformation = configuration[ScopesInfoBlobConfig]
                ?? throw new ArgumentNullException(nameof(ScopesInfoBlobConfig), $"Config path missing: { ScopesInfoBlobConfig }");
            _defaultRefreshTimeInHours = FileServiceHelper.GetFileCacheRefreshTime(configuration[CacheRefreshTimeConfig]);
        }

        /// <summary>
        /// Populates the template table with the request urls and the scopes table with the permission scopes.
        /// </summary>
        private void SeedPermissionsTables()
        {
            _urlTemplateMatcher = new UriTemplateMatcher();
            _scopesListTable = new Dictionary<int, object>();

            HashSet<string> uniqueRequestUrlsTable = new HashSet<string>();
            int count = 0;

            foreach (string permissionFilePath in _permissionsBlobNames)
            {
                string relativePermissionPath = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, permissionFilePath);
                string jsonString = _fileUtility.ReadFromFile(relativePermissionPath).GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(jsonString))
                {
                    JObject permissionsObject = JObject.Parse(jsonString);

                    if (permissionsObject.Count < 1)
                    {
                        throw new InvalidOperationException($"The permissions data sources cannot be empty." +
                            $"Check the source file or check whether the file path is properly set. File path: " +
                            $"{relativePermissionPath}");
                    }

                    JToken apiPermissions = permissionsObject.First.First;

                    foreach (JProperty property in apiPermissions)
                    {
                        // Remove any '(...)' from the request url and set to lowercase for uniformity
                        string requestUrl = Regex.Replace(property.Name.ToLower(), @"\(.*?\)", string.Empty);

                        if (uniqueRequestUrlsTable.Add(requestUrl))
                        {
                            count++;

                            // Add the request url
                            _urlTemplateMatcher.Add(count.ToString(), requestUrl);

                            // Add the permission scopes
                            _scopesListTable.Add(count, property.Value);
                        }
                    }

                    _permissionsRefreshed = true;
                }
            }
        }

        /// <summary>
        /// Gets or creates the localized permissions descriptions from the cache.
        /// </summary>
        /// <param name="locale">The locale of the permissions decriptions file.</param>
        /// <returns>The localized instance of permissions descriptions.</returns>
        private async Task<IDictionary<string, IDictionary<string, ScopeInformation>>> GetOrCreatePermissionsDescriptionsAsync(string locale = DefaultLocale)
        {
            var scopesInformationDictionary = await _permissionsCache.GetOrCreateAsync($"ScopesInfoList_{locale}", cacheEntry =>
            {
                /* Localized copy of permissions descriptions
                   is to be seeded by only one executing thread.
                */
                lock (_scopesLock)
                {
                    /* Check whether a previous thread already seeded an
                     * instance of the localized permissions descriptions
                     * during the lock.
                     */
                    var seededScopesInfoDictionary = _permissionsCache.Get<IDictionary<string, IDictionary<string, ScopeInformation>>>($"ScopesInfoList_{locale}");

                    if (seededScopesInfoDictionary == null)
                    {
                        string relativeScopesInfoPath = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, _scopesInformation, locale);

                        // Get file contents from source
                        string scopesInfoJson = _fileUtility.ReadFromFile(relativeScopesInfoPath).GetAwaiter().GetResult();

                        seededScopesInfoDictionary = CreateScopesInformationTables(scopesInfoJson);

                        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);
                    }

                    /* Fetch the localized cached permissions descriptions
                       already seeded by previous thread. */
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
            string host = _configuration["BlobStorage:GithubHost"];
            string repo = _configuration["BlobStorage:RepoName"];

            string localizedFilePathSource = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, _scopesInformation, locale);

            // Get the absolute url from configuration and query param, then read from the file
            var queriesFilePathSource = string.Concat(host, org, repo, branchName, FileServiceConstants.DirectorySeparator, localizedFilePathSource);

            // Get file contents from source
            string scopesInfoJson = await FetchHttpSourceDocument(queriesFilePathSource);

            var scopesInformationDictionary = CreateScopesInformationTables(scopesInfoJson);

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

            return await _httpClientUtility.ReadFromDocument(httpRequestMessage);
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
                return null;
            }

            ScopesInformationList scopesInformationList = JsonConvert.DeserializeObject<ScopesInformationList>(scopesInfoJson);

            var _delegatedScopesInfoTable = new Dictionary<string, ScopeInformation>();
            var _applicationScopesInfoTable = new Dictionary<string, ScopeInformation>();

            foreach (ScopeInformation delegatedScopeInfo in scopesInformationList.DelegatedScopesList)
            {
                _delegatedScopesInfoTable.Add(delegatedScopeInfo.ScopeName, delegatedScopeInfo);
            }

            foreach (ScopeInformation applicationScopeInfo in scopesInformationList.ApplicationScopesList)
            {
                _applicationScopesInfoTable.Add(applicationScopeInfo.ScopeName, applicationScopeInfo);
            }

            return new Dictionary<string, IDictionary<string, ScopeInformation>>
            {
                { Delegated, _delegatedScopesInfoTable },
                { Application, _applicationScopesInfoTable }
            };
        }

        /// <summary>
        /// Determines whether the permissions tables need to be refreshed with new data based on the elapsed time
        /// duration since the previous refresh.
        /// </summary>
        /// <returns>true or false based on whether the elapsed time duration is greater or less than the specified
        /// refresh time duration.</returns>
        private bool RefreshPermissionsTables()
        {
            bool refresh = false;

            bool cacheState = _permissionsCache.GetOrCreate("PermissionsTablesState", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);
                _permissionsRefreshed = false;
                return refresh = true;
            });

            return refresh;
        }

        /// <summary>
        /// Retrieves permissions scopes.
        /// </summary>
        /// <param name="scopeType">The type of scope to be retrieved for the target request url.</param>
        /// <param name="locale">The language code for the preferred localized file.</param>
        /// <param name="requestUrl">Optional: The target request url whose scopes are to be retrieved.</param>
        /// <param name="method">Optional: The target http verb of the request url whose scopes are to be retrieved.</param>
        /// <param name="org">Optional: The name of the org/owner of the repo.</param>
        /// <param name="branchName">Optional: The name of the branch containing the files.</param>
        /// <returns>A list of scopes for the target request url given a http verb and type of scope.</returns>
        public async Task<List<ScopeInformation>> GetScopesAsync(string scopeType = "DelegatedWork",
                                                                 string locale = DefaultLocale,
                                                                 string requestUrl = null,
                                                                 string method = null,
                                                                 string org = null,
                                                                 string branchName = null)
        {
            try
            {
                InitializePermissions();

                IDictionary<string, IDictionary<string, ScopeInformation>> scopesInformationDictionary;

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

                if (string.IsNullOrEmpty(requestUrl))  // fetch all permissions
                {
                    List<ScopeInformation> scopesListInfo = new List<ScopeInformation>();

                    if (scopeType.Contains(Delegated))
                    {
                        if (scopesInformationDictionary.ContainsKey(Delegated))
                        {
                            foreach (var scopesInfo in scopesInformationDictionary[Delegated])
                            {
                                scopesListInfo.Add(scopesInfo.Value);
                            }
                        }
                    }
                    else // Application scopes
                    {
                        if (scopesInformationDictionary.ContainsKey(Application))
                        {
                            foreach (var scopesInfo in scopesInformationDictionary[Application])
                            {
                                scopesListInfo.Add(scopesInfo.Value);
                            }
                        }
                    }

                    return scopesListInfo;
                }
                else // fetch permissions for a given request url and method
                {
                    if (string.IsNullOrEmpty(method))
                    {
                        throw new ArgumentNullException(nameof(method), "The HTTP method value cannot be null or empty.");
                    }

                    requestUrl = Regex.Replace(requestUrl, @"\?.*", string.Empty); // remove any query params
                    requestUrl = Regex.Replace(requestUrl, @"\(.*?\)", string.Empty); // remove any '(...)' resource modifiers

                    // Check if requestUrl is contained in our Url Template table
                    TemplateMatch resultMatch = _urlTemplateMatcher.Match(new Uri(requestUrl.ToLowerInvariant(), UriKind.RelativeOrAbsolute));

                    if (resultMatch == null)
                    {
                        return null;
                    }

                    JArray resultValue = new JArray();
                    resultValue = (JArray)_scopesListTable[int.Parse(resultMatch.Key)];

                    var scopes = resultValue.FirstOrDefault(x => x.Value<string>("HttpVerb") == method)?
                        .SelectToken(scopeType)?
                        .Select(s => (string)s)
                        .ToArray();

                    if (scopes == null)
                    {
                        return null;
                    }

                    List<ScopeInformation> scopesList = new List<ScopeInformation>();

                    foreach (string scopeName in scopes)
                    {
                        ScopeInformation scopeInfo = null;

                        if (scopeType.Contains(Delegated))
                        {
                            if (scopesInformationDictionary[Delegated].ContainsKey(scopeName))
                            {
                                scopeInfo = scopesInformationDictionary[Delegated][scopeName];
                            }
                        }
                        else // Application scopes
                        {
                            if (scopesInformationDictionary[Application].ContainsKey(scopeName))
                            {
                                scopeInfo = scopesInformationDictionary[Application][scopeName];
                            }
                        }
                        if (scopeInfo == null)
                        {
                            scopesList.Add(new ScopeInformation
                            {
                                ScopeName = scopeName
                            });
                        }
                        else
                        {
                            scopesList.Add(scopeInfo);
                        }
                    }

                    return scopesList;
                }
            }
            catch (ArgumentNullException exception)
            {
                throw exception;
            }
            catch (ArgumentException)
            {
                return null; // equivalent to no match for the given requestUrl
            }
        }

        /// <summary>
        /// Initializes Permissions.
        /// </summary>
        private void InitializePermissions()
        {
            /* Add multiple checks to ensure thread that
             * populated scopes list information successfully
             * completed seeding.
            */
            if (RefreshPermissionsTables() ||
                _scopesListTable == null ||
                !_scopesListTable.Any())
            {
                /* Permissions tables are not localized, so no need to keep different localized cached copies.
                   Refresh tables only after the specified time duration has elapsed or no cached copy exists.
                */
                lock (_permissionsLock)
                {
                    /* Ensure permissions tables are seeded by only one executing thread,
                       once per refresh cycle.
                    */
                    if (!_permissionsRefreshed)
                    {
                        SeedPermissionsTables();
                        _permissionsRefreshed = true;
                    }
                }
            }
        }
    }
}