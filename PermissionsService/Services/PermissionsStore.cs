// -------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AsyncKeyedLock;
using FileService.Common;
using FileService.Interfaces;
using Kibali;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using PermissionsService.Interfaces;
using PermissionsService.Models;
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
        private readonly Dictionary<ScopeType, string> permissionDescriptionGroups =
            new() { { ScopeType.DelegatedWork, Delegated },
                    { ScopeType.DelegatedPersonal, Delegated },
                    { ScopeType.Application, Application } };
        private readonly string _permissionsContainerName;
        private readonly string _permissionsBlobName;
        private readonly string _scopesInformation;
        private readonly int _defaultRefreshTimeInHours; // life span of the in-memory cache
        private const string DefaultLocale = "en-US"; // default locale language
        private static readonly AsyncKeyedLocker<string> _asyncKeyedLocker = new();
        private const string Delegated = "Delegated";
        private const string Application = "Application";
        private const string CacheRefreshTimeConfig = "FileCacheRefreshTimeInHours:Permissions";
        private const string ScopesInfoBlobConfig = "BlobStorage:Blobs:Permissions:Descriptions";
        private const string PermissionsNameBlobConfig = "BlobStorage:Blobs:Permissions:Name";
        private const string PermissionsContainerBlobConfig = "BlobStorage:Containers:Permissions";
        private const string NullValueError = "Value cannot be null";
        private class PermissionsDataInfo
        {
            public UriTemplateMatcher UriTemplateMatcher
            {
                get; set;
            }

            public Dictionary<int, Dictionary<string, Dictionary<ScopeType, SchemePermissions>>> PathPermissions
            {
                get; set;
            } = new();
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
            _permissionsBlobName = configuration.GetSection(PermissionsNameBlobConfig).Get<string>()
                ?? throw new ArgumentNullException(nameof(PermissionsNameBlobConfig), $"Config path missing: {PermissionsNameBlobConfig}");
            _scopesInformation = configuration[ScopesInfoBlobConfig]
                ?? throw new ArgumentNullException(nameof(ScopesInfoBlobConfig), $"Config path missing: {ScopesInfoBlobConfig}");
            _defaultRefreshTimeInHours = FileServiceHelper.GetFileCacheRefreshTime(configuration[CacheRefreshTimeConfig]);
        }

        private async Task<PermissionsDataInfo> GetPermissionsDataAsync()
        {
            using(await _asyncKeyedLocker.LockAsync("PermissionData"))
            {
                return await _cache.GetOrCreateAsync("PermissionsData", async entry =>
                {
                    var permissionsData = await LoadPermissionsDataAsync();
                    entry.AbsoluteExpirationRelativeToNow = permissionsData is not null ? TimeSpan.FromHours(_defaultRefreshTimeInHours) : TimeSpan.FromMilliseconds(1);
                    return permissionsData;
                });
            }
        }

        private async Task<PermissionsDocument> LoadDocumentAsync()
        {
            using(await _asyncKeyedLocker.LockAsync("PermissionsDocument"))
            {
                return await _cache.GetOrCreateAsync("PermissionsDocument", async entry =>
                {
                    _telemetryClient?.TrackTrace($"Fetching permissions from file source '{_permissionsBlobName}'",
                                SeverityLevel.Information,
                                _permissionsTracePropertiesWithSanitizeIgnore);
                    PermissionsDocument permissionsDocument;

                    try
                    {
                        string relativePermissionPath = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, _permissionsBlobName);
                        string permissions = await _fileUtility.ReadFromFileAsync(relativePermissionPath);
                        permissionsDocument = PermissionsDocument.Load(permissions);
                        entry.AbsoluteExpirationRelativeToNow = permissionsDocument is not null ? TimeSpan.FromHours(_defaultRefreshTimeInHours) : TimeSpan.FromMilliseconds(1);
                    }
                    catch (Exception exception)
                    {

                        _telemetryClient?.TrackException(exception);
                        permissionsDocument = null;
                    }
                    return permissionsDocument;
                });
            }
        }

        /// <summary>
        /// Populates the template table with the request urls and the scopes table with the permission scopes.
        /// </summary>
        /// <returns></returns>
        private async Task<PermissionsDataInfo> LoadPermissionsDataAsync()
        {
            PermissionsDataInfo permissionsData;
            try
            {
                _telemetryClient?.TrackTrace($"Fetching permissions from file source '{_permissionsBlobName}'",
                                             SeverityLevel.Information,
                                             _permissionsTracePropertiesWithSanitizeIgnore);

                // Get file contents from source
                string relativePermissionPath = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, _permissionsBlobName);

                string permissionsJson = await _fileUtility.ReadFromFileAsync(relativePermissionPath);
                var fetchedPermissions = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<ScopeType, SchemePermissions>>>>(permissionsJson);

                _telemetryClient?.TrackTrace("Finished fetching permissions from file",
                                         SeverityLevel.Information,
                                         _permissionsTraceProperties);


                int count = 0;
                var urlTemplateMatcher = new UriTemplateMatcher();
                var pathPermissions = new Dictionary<int, Dictionary<string, Dictionary<ScopeType, SchemePermissions>>>();

                _telemetryClient?.TrackTrace("Started seeding permissions table",
                                         SeverityLevel.Information,
                                         _permissionsTraceProperties);

                foreach (var key in fetchedPermissions.Keys)
                {
                    // Remove any '(...)' from the request url and set to lowercase for uniformity
                    string requestUrl = key.RemoveParentheses().ToLower();

                    count++;

                    // Add the request url
                    urlTemplateMatcher.Add(count.ToString(), requestUrl);

                    // Add the permission scopes for path
                    pathPermissions.Add(count, fetchedPermissions[key]);
                }

                permissionsData = new PermissionsDataInfo()
                {
                    UriTemplateMatcher = urlTemplateMatcher,
                    PathPermissions = pathPermissions
                };

                _telemetryClient?.TrackTrace("Finished seeding permissions table",
                                             SeverityLevel.Information,
                                             _permissionsTraceProperties);
            }
            catch (Exception exception)
            {
                _telemetryClient?.TrackException(exception);
                permissionsData = null;
            }

            return permissionsData;
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

            // making sure only a single thread at a time access the cache
            // when already seeded, lock will resolve fast and access the cache
            // when not seeded, lock will resolve slow for all other threads and seed the cache on the first thread
            using (await _asyncKeyedLocker.LockAsync("scopes"))
            {
                var scopesInformationDictionary = await _cache.GetOrCreateAsync($"ScopesInfoList_{locale}", async cacheEntry =>
                {
                    _telemetryClient?.TrackTrace($"In-memory cache 'ScopesInfoList_{locale}' empty. " +
                                                $"Seeding permissions for locale '{locale}' from Azure blob resource",
                                                SeverityLevel.Information,
                                                _permissionsTraceProperties);

                    string relativeScopesInfoPath = FileServiceHelper.GetLocalizedFilePathSource(_permissionsContainerName, _scopesInformation, locale);

                    // Get file contents from source
                    string scopesInfoJson = await _fileUtility.ReadFromFileAsync(relativeScopesInfoPath);
                    _telemetryClient?.TrackTrace($"Successfully seeded permissions for locale '{locale}' from Azure blob resource",
                                                SeverityLevel.Information,
                                                _permissionsTraceProperties);

                    var seededScopesInfoDictionary = CreateScopesInformationTables(scopesInfoJson);
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);

                    _telemetryClient?.TrackTrace($"Return locale '{locale}' permissions from Azure blob resource",
                                                SeverityLevel.Information,
                                                _permissionsTraceProperties);

                    return seededScopesInfoDictionary;
                });

                return scopesInformationDictionary;
            }
        }

        /// <summary>
        /// Gets the permissions descriptions and their localized instances from DevX Content Repo.
        /// </summary>
        /// <param name="locale">The locale of the permissions descriptions file.</param>
        /// <param name="org">The org or owner of the repo.</param>
        /// <param name="branchName">The name of the branch with the file version.</param>
        /// <returns>The localized instance of permissions descriptions.</returns>
        private async Task<IDictionary<string, IDictionary<string, ScopeInformation>>> GetPermissionsDescriptionsFromGithubAsync(string org,
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
            string scopesInfoJson = await FetchHttpSourceDocumentAsync(queriesFilePathSource);

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
        private async Task<string> FetchHttpSourceDocumentAsync(string sourceUri)
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

            var scopesInformationList = JsonSerializer.Deserialize<ScopesInformationList>(scopesInfoJson);
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
        public async Task<PermissionResult> GetScopesAsync(List<RequestInfo> requests = null,
                                                   string locale = DefaultLocale,
                                                   ScopeType? scopeType = null,
                                                   bool includeHidden = false,
                                                   bool leastPrivilegeOnly = false,
                                                   string org = null,
                                                   string branchName = null)
        {
            var permissionsDocument = await LoadDocumentAsync() ?? throw new InvalidOperationException("Failed to fetch permissions");
            var scopes = new List<ScopeInformation>();
            var errors = new List<PermissionError>();

            bool getAllScopes = false;
            if (requests == null || requests.Count == 0)
            {
                // Get all scopes if no request URLs are provided
                getAllScopes = true;
                var allScopes = GetAllScopesFromDocument(permissionsDocument, scopeType);
                scopes.AddRange(allScopes);
            }
            else
            {
                var authZChecker = new AuthZChecker() { LenientMatch = true };
                authZChecker.Load(permissionsDocument);

                var scopesByRequestUrl = new ConcurrentDictionary<string, IEnumerable<ScopeInformation>>();
                var uniqueRequests = requests.DistinctBy(static x => $"{x.HttpMethod}{x.RequestUrl}", StringComparer.OrdinalIgnoreCase);
                Parallel.ForEach(uniqueRequests, (request) =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(request.RequestUrl))
                            throw new InvalidOperationException("The request URL cannot be null or empty.");

                        if (string.IsNullOrEmpty(request.HttpMethod))
                            throw new InvalidOperationException("The HTTP method value cannot be null or empty.");

                        var requestUrl = CleanRequestUrl(request.RequestUrl);

                        var resource = authZChecker.FindResource(requestUrl) ?? throw new InvalidOperationException($"Permissions information for '{request.HttpMethod} {request.RequestUrl}' was not found.");
                        var permissions = GetPermissionsFromResource(scopeType, request, resource);
                        if (permissions.Count == 0)
                            throw new InvalidOperationException($"Permissions information for '{request.HttpMethod} {request.RequestUrl}' was not found.");

                        scopesByRequestUrl.TryAdd($"{request.HttpMethod} {request.RequestUrl}", permissions);
                    }
                    catch (Exception exception)
                    {
                        errors.Add(new PermissionError()
                        {
                            RequestUrl = request.RequestUrl,
                            Message = exception.Message
                        });
                        _telemetryClient?.TrackException(exception);
                    }
                });

                var allLeastPrivilegeScopes = scopesByRequestUrl.Values
                    .SelectMany(static x => x).Where(static x => x.IsLeastPrivilege == true)
                    .DistinctBy(static x => $"{x.ScopeName}{x.ScopeType}", StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var scopeSet in scopesByRequestUrl.Values)
                {
                    var higherPrivilegedScopes = scopeSet.Where(static x => x.IsLeastPrivilege == false);

                    // If any of the higher privilege permissions is a leastPrivilegePermissions somewhere, ignore
                    bool foundInOthers = higherPrivilegedScopes.Any(scope =>
                            allLeastPrivilegeScopes.Any(leastScope =>
                                leastScope.ScopeName.Equals(scope.ScopeName, StringComparison.OrdinalIgnoreCase) &&
                                    leastScope.ScopeType == scope.ScopeType));

                    if (!foundInOthers)
                        scopes.AddRange(scopeSet);
                }

                if (leastPrivilegeOnly)
                    scopes = scopes.Where(static x => x.IsLeastPrivilege == true).ToList();
            }


            if (scopes.Count == 0 && errors.Count == 0)
            {
                errors.Add(new PermissionError()
                {
                    Message = "No permissions found."
                });
                return new PermissionResult()
                {
                    Results = scopes,
                    Errors = errors.Count != 0 ? errors : null,
                };
            }

            // Create a dict of scopes information from GitHub files or cached files
            var scopesInformationDictionary = !(string.IsNullOrEmpty(org) || string.IsNullOrEmpty(branchName))
                ? await GetPermissionsDescriptionsFromGithubAsync(org, branchName, locale)
                : await GetOrCreatePermissionsDescriptionsAsync(locale);

            // Get consent display name and description
            var scopesInfo = GetAdditionalScopesInformation(
                scopesInformationDictionary,
                scopes.DistinctBy(static x => $"{x.ScopeName}{x.ScopeType}", StringComparer.OrdinalIgnoreCase).ToList(),
                scopeType,
                getAllScopes);

            // exclude hidden permissions unless stated otherwise
            scopesInfo = scopesInfo.Where(x => includeHidden || !x.IsHidden).ToList();

            if (scopesInfo.Count == 0 && errors.Count == 0)
            {
                errors.Add(new PermissionError()
                {
                    Message = "No permissions found."
                });
            }

            _telemetryClient?.TrackTrace(requests == null || requests.Count == 0 ?
                        "Return all permissions" : $"Return permissions for '{string.Join(", ", requests.Select(x => x.RequestUrl))}'",
                SeverityLevel.Information,
                _permissionsTraceProperties);
            return new PermissionResult()
            {
                Results = scopesInfo,
                Errors = errors.Count != 0 ? errors : null,
            };
        }

        private static IEnumerable<ScopeInformation> GetAllScopesFromDocument(PermissionsDocument permissionsDocument, ScopeType? scopeType)
        {
            var allPermissions = permissionsDocument.Permissions;

            if (scopeType == null)
            {
                return allPermissions.SelectMany(item => item.Value.Schemes.Keys,
                    (item, scope) => new ScopeInformation
                    {
                        ScopeType = Enum.TryParse(scope, out ScopeType type) ? type : default,
                        ScopeName = item.Key
                    });
            }
            else
            {
                return allPermissions
                    .Where(x => x.Value.Schemes.Keys.Any(k => k.Equals(scopeType.ToString(), StringComparison.OrdinalIgnoreCase)))
                    .Select(grant => new ScopeInformation
                    {
                        ScopeName = grant.Key,
                        ScopeType = scopeType.Value
                    });
            }
        }

        private static List<ScopeInformation> GetPermissionsFromResource(ScopeType? scopeType, RequestInfo request, ProtectedResource resource)
        {

            if (resource.SupportedMethods.TryGetValue(request.HttpMethod, out var methodPermissions))
            {
                if (scopeType == null)
                {
                    return methodPermissions.Keys
                        .Where(key => Enum.TryParse(key, out ScopeType type))
                        .SelectMany(key =>
                        {
                            var type = Enum.Parse<ScopeType>(key);
                            var least = FetchLeastPrivilege(resource, request.HttpMethod, type);
                            return methodPermissions[key].Select(grant => new ScopeInformation
                            {
                                ScopeName = grant.Permission,
                                ScopeType = type,
                                IsLeastPrivilege = grant.Permission == least
                            });
                        })
                        .ToList();
                }
                if (methodPermissions.TryGetValue(scopeType.ToString(), out var scopedPermissions))
                {
                    var least = FetchLeastPrivilege(resource, request.HttpMethod, scopeType);
                    return scopedPermissions.Select(grant => new ScopeInformation
                    {
                        ScopeName = grant.Permission,
                        ScopeType = scopeType.Value,
                        IsLeastPrivilege = grant.Permission == least
                    }).ToList();
                }
            }
            return new List<ScopeInformation>();
        }

        private static string FetchLeastPrivilege(ProtectedResource resource, string requestHttpMethod, ScopeType? type)
        {
            var leastPrivilege = resource.FetchLeastPrivilege(requestHttpMethod, type.ToString());
            var scopedPermission = leastPrivilege?.Values.FirstOrDefault()?.GetValueOrDefault(type.ToString());
            return scopedPermission?.FirstOrDefault();
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
            return Regex.Replace(requestUrl, @"(\$.*)", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(5))
                        .TrimEnd('/')
                        .ToLowerInvariant();
        }

        /// <summary>
        /// Retrieves the scopes information for a given list of scopes.
        /// </summary>
        /// <param name="scopesInformationDictionary">The source of the scopes information.</param>
        /// <param name="scopes">The target list of scopes.</param>
        /// <returns>A list of <see cref="ScopeInformation"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private List<ScopeInformation> GetAdditionalScopesInformation(IDictionary<string, IDictionary<string, ScopeInformation>> scopesInformationDictionary,
            List<ScopeInformation> scopes, ScopeType? scopeType = null, bool getAllPermissions = false)
        {
            ArgumentNullException.ThrowIfNull(scopesInformationDictionary);

            ArgumentNullException.ThrowIfNull(scopes);

            var descriptionGroups = permissionDescriptionGroups.Values.Distinct().Except(scopesInformationDictionary.Keys);
            foreach (var group in descriptionGroups)
            {
                var errMsg = $"{nameof(scopesInformationDictionary)} does not contain a dictionary for {group} scopes.";
                _telemetryClient?.TrackTrace(errMsg, SeverityLevel.Error, _permissionsTraceProperties);
            }

            var scopesInfo = scopes.Select(scope =>
            {
                permissionDescriptionGroups.TryGetValue((ScopeType)scope.ScopeType, out var schemeKey);
                if (scopesInformationDictionary[schemeKey].TryGetValue(scope.ScopeName, out var scopeInfo))
                {
                    return new ScopeInformation()
                    {
                        ScopeName = scopeInfo.ScopeName,
                        DisplayName = scopeInfo.DisplayName,
                        IsAdmin = scopeInfo.IsAdmin,
                        IsHidden = scopeInfo.IsHidden,
                        Description = scopeInfo.Description,
                        ScopeType = scope.ScopeType,
                        IsLeastPrivilege = scope.IsLeastPrivilege
                    };
                }
                return scope;
            }).ToList();

            if (getAllPermissions)
            {
                foreach (var key in permissionDescriptionGroups.Keys)
                {
                    if (scopeType != null && key != scopeType)
                        continue;

                    scopesInfo.AddRange(
                        scopesInformationDictionary[permissionDescriptionGroups[key]].Values
                        .Where(info => !scopesInfo.Exists(x => x.ScopeName.Equals(info.ScopeName, StringComparison.OrdinalIgnoreCase)))
                        .Select(scope =>
                        {
                            return new ScopeInformation()
                            {
                                ScopeName = scope.ScopeName,
                                DisplayName = scope.DisplayName,
                                IsAdmin = scope.IsAdmin,
                                Description = scope.Description,
                                ScopeType = key
                            };
                        }));
                }
            }
            return scopesInfo;
        }

        ///<inheritdoc/>
        public async Task<UriTemplateMatcher> GetUriTemplateMatcherAsync()
        {
            var permissionsData = await GetPermissionsDataAsync();
            return permissionsData.UriTemplateMatcher;
        }
    }
}
