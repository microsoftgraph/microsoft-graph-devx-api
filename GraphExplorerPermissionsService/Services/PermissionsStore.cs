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
        public PermissionsStore(IConfiguration configuration, IFileUtility fileUtility = null, IMemoryCache permissionsCache = null, IHttpClientUtility httpClientUtility = null)
        {
            _defaultRefreshTimeInHours = FileServiceHelper.GetFileCacheRefreshTime(configuration["FileCacheRefreshTimeInHours"]);
            _permissionsCache = permissionsCache;
            _fileUtility = fileUtility;
            _permissionsContainerName = configuration["BlobStorage:Containers:Permissions"];
            _permissionsBlobNames = configuration.GetSection("BlobStorage:Blobs:Permissions:Names").Get<List<string>>();
            _scopesInformation = configuration["BlobStorage:Blobs:Permissions:Descriptions"];
        }
        /// <summary>
        /// Populates the template table with the request urls and the scopes table with the permission scopes.
        /// </summary>
        private void SeedPermissionsTables(string org = null,
                                           string branchName = null)
        {
            _urlTemplateMatcher = new UriTemplateMatcher();
            _scopesListTable = new Dictionary<int, object>();

            foreach (string permissionFilePath in _permissionsBlobNames)
            {
                string localizedPermissionPath = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, permissionFilePath);
                string relativePermissionPath = localizedPermissionPath;
                string jsonStringPermissions;

                if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(branchName))
                {
                    // Fetch the permissions from GitHub blob
                    string host = _configuration["BlobStorage:GithubHost"];
                    string repo = _configuration["BlobStorage:RepoName"];

                    relativePermissionPath = string.Concat(host, org, repo, branchName, FileServiceConstants.DirectorySeparator, localizedPermissionPath);
                    jsonStringPermissions = FetchHttpSourceDocument(relativePermissionPath).GetAwaiter().GetResult();
                }
                else
                {
                    jsonStringPermissions = _fileUtility.ReadFromFile(relativePermissionPath).GetAwaiter().GetResult();
                }

                ParsePermissionsTables(jsonStringPermissions, relativePermissionPath);
            }
        }
        private void ParsePermissionsTables(string jsonString, string relativePermissionPath)
        {
            HashSet<string> uniqueRequestUrlsTable = new HashSet<string>();
            int count = 0;
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
                    var seededScopesInfoDictionary = _permissionsCache.Get<Dictionary<string, IDictionary<string, ScopeInformation>>>($"ScopesInfoList_{locale}");
                    if (seededScopesInfoDictionary == null)
                    {
                        string relativeScopesInfoPath = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, _scopesInformation, locale);
                        // Get file contents from source
                        string scopesInfoJson = _fileUtility.ReadFromFile(relativeScopesInfoPath).GetAwaiter().GetResult();
                        seededScopesInfoDictionary = CreateScopesInformationTables(scopesInfoJson, cacheEntry);
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
        private async Task<IDictionary<string, IDictionary<string, ScopeInformation>>> GetPermissionsFromGithub(string locale,
                                                                                                                string org,
                                                                                                                string branchName)
        {
            string host = _configuration["BlobStorage:GithubHost"];
            string repo = _configuration["BlobStorage:RepoName"];
            string localizedFilePathSource = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, _scopesInformation, locale);

                        foreach (ScopeInformation applicationScopeInfo in scopesInformationList.ApplicationScopesList)
                        {
                            _applicationScopesInfoTable.Add(applicationScopeInfo.ScopeName, applicationScopeInfo);
                        }

            // Get file contents from source
            string scopesInfoJson = await FetchHttpSourceDocument(queriesFilePathSource);

            var scopesInformationDictionary = CreateScopesInformationTables(scopesInfoJson);

            return scopesInformationDictionary;
        }
        private async Task<string> FetchHttpSourceDocument(string sourceUri)
        {
            // Construct the http request message
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, sourceUri);
            return await _httpClientUtility.ReadFromFile(httpRequestMessage);
        }
        /// <summary>
        /// Creates a dictionary of scopes information
        /// </summary>
        /// <param name="filePath">The path of the file from Github.</param>
        /// <param name="cacheEntry">An optional cache entry param.</param>
        /// <returns>A dictionary of scopes information.</returns>
        private IDictionary<string, IDictionary<string, ScopeInformation>> CreateScopesInformationTables(string scopesInfoJson, ICacheEntry cacheEntry = null)
        {
            var _delegatedScopesInfoTable = new Dictionary<string, ScopeInformation>();
            var _applicationScopesInfoTable = new Dictionary<string, ScopeInformation>();
            if (string.IsNullOrEmpty(scopesInfoJson))
            {
                return null;
            }
            ScopesInformationList scopesInformationList = JsonConvert.DeserializeObject<ScopesInformationList>(scopesInfoJson);
            foreach (ScopeInformation delegatedScopeInfo in scopesInformationList.DelegatedScopesList)
            {
                _delegatedScopesInfoTable.Add(delegatedScopeInfo.ScopeName, delegatedScopeInfo);
            }
            foreach (ScopeInformation applicationScopeInfo in scopesInformationList.ApplicationScopesList)
            {
                _applicationScopesInfoTable.Add(applicationScopeInfo.ScopeName, applicationScopeInfo);
            }
            if (cacheEntry != null)
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);
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
        private bool RefreshPermissionsTables(string cacheName)
        {
            bool refresh = false;
            bool cacheState = (bool)(_permissionsCache?.GetOrCreate(cacheName, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);
                _permissionsRefreshed = false;
                return refresh = true;
            }));
            return refresh;
        }
        /// <summary>
        /// Retrieves permissions scopes.
        /// </summary>
        /// <param name="scopeType">The type of scope to be retrieved for the target request url.</param>
        /// <param name="locale">The language code for the preferred localized file.</param>
        /// <param name="requestUrl">Optional: The target request url whose scopes are to be retrieved.</param>
        /// <param name="method">Optional: The target http verb of the request url whose scopes are to be retrieved.</param>
        /// /// <param name="org">Optional: The name of the org/owner of the repo.</param>
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
                IDictionary<string, IDictionary<string, ScopeInformation>> scopesInformationDictionary;
                if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(branchName))
                {
                    InitializePermissions(org, branchName);
                    // Creates a dict of scopes information from GitHub files
                    scopesInformationDictionary = await GetPermissionsFromGithub(locale, org, branchName);
                }
                else
                {
                    InitializePermissions();
                    // Creates a dict of scopes information from cached files
                    scopesInformationDictionary = await GetOrCreatePermissionsDescriptionsAsync(locale);
                }
                var scopesList = CreateScopesList(scopesInformationDictionary, scopeType, requestUrl, method);
                return scopesList;
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
        /// Creates a list of scope information.
        /// </summary>
        /// <param name="scopesInformationDictionary">A dictionary of scopes information.</param>
        /// <param name="scopeType">The type of scope to be retrieved for the target request url.</param>
        /// <param name="locale">The language code for the preferred localized file.</param>
        /// <param name="requestUrl">The target request url whose scopes are to be retrieved.</param>
        /// <param name="method">The target http verb of the request url whose scopes are to be retrieved.</param>
        /// <returns>A list of scopes for the target request url given a http verb and type of scope.</returns>
        private List<ScopeInformation> CreateScopesList(IDictionary<string, IDictionary<string, ScopeInformation>> scopesInformationDictionary,
                                                                    string scopeType = "DelegatedWork",
                                                                    string requestUrl = null,
                                                                    string method = null)
        {
            if (string.IsNullOrEmpty(requestUrl))  // fetch all permissions
            {
                List<ScopeInformation> scopesListInfo = new List<ScopeInformation>();
                if (scopesInformationDictionary.ContainsKey(Delegated))
                {
                    foreach (var scopesInfo in scopesInformationDictionary[Delegated])
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
                TemplateMatch resultMatch = _urlTemplateMatcher.Match(new Uri(requestUrl.ToLower(), UriKind.RelativeOrAbsolute));
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
                    }
                    if (scopeInfo == null)
                    {
                        scopesList.Add(new ScopeInformation
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
                    else
                    {
                        scopesList.Add(scopeInfo);
                    }
                }
                return scopesList;
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
        private void InitializePermissions(string org = null,
                                           string branchName = null)
        {
            string cacheName = "PermissionsTablesState";
            if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(branchName))
            {
                cacheName = $"{org}_{branchName}";
            }
            /* Add multiple checks to ensure thread that
             * populated scopes information successfully
             * completed seeding.
            */
            if (RefreshPermissionsTables(cacheName) ||
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
                        SeedPermissionsTables(org, branchName);
                        _permissionsRefreshed = true;
                    }
                }
            }
        }
    }
}