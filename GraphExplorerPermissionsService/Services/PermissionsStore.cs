// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using GraphExplorerPermissionsService.Interfaces;
using GraphExplorerPermissionsService.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Tavis.UriTemplates;

namespace GraphExplorerPermissionsService
{
    public class PermissionsStore : IPermissionsStore
    {
        private readonly UriTemplateTable _urlTemplateTable;
        private readonly IDictionary<int, object> _scopesListTable;
        private readonly IDictionary<string, ScopeInformation> _delegatedScopesInfoTable;
        private readonly IDictionary<string, ScopeInformation> _applicationScopesInfoTable;
        private readonly IFileUtility _fileUtility;
        private readonly List<string> _permissionsFilePaths;
        private readonly string _scopesInformation;

        public PermissionsStore(IFileUtility fileUtility, IConfiguration configuration)
        {
            _urlTemplateTable = new UriTemplateTable();
            _scopesListTable = new Dictionary<int, object>();
            _delegatedScopesInfoTable = new Dictionary<string, ScopeInformation>();
            _applicationScopesInfoTable = new Dictionary<string, ScopeInformation>();
            _fileUtility = fileUtility;
            _permissionsFilePaths = configuration.GetSection("Permissions:FilePaths").Get<List<string>>();
            _scopesInformation = configuration["Permissions:ScopesInformationList"];

            SeedTables();
        }

        private void SeedTables()
        {
            try
            {
                /* Order of seeding matters. 
                 * Scopes info. tables are less important to us vis-à-vis the Permission tables;
                 * In case of an exception when seeding the Scopes info. table we can safely exit 
                 * with confidence since permissions are already seeded. */

                SeedPermissionsTables();
                SeedScopesInfoTables();
            }
            catch
            {
                // Do nothing; the tables will just be empty
            }
        }

        /// <summary>
        /// Populates the template table with the request urls and the scopes table with the permission scopes.
        /// </summary>
        private void SeedPermissionsTables()
        {
            HashSet<string> uniqueRequestUrlsTable = new HashSet<string>();
            int count = 0;

            foreach (string permissionFilePath in _permissionsFilePaths)
            {
                string jsonString = _fileUtility.ReadFromFile(permissionFilePath).GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(jsonString))
                {
                    JObject permissionsObject = JObject.Parse(jsonString);

                    JToken apiPermissions = permissionsObject.First.First;

                    foreach (JProperty property in apiPermissions)
                    {
                        // Remove any '(...)' from the request url and set to lowercase for uniformity
                        string requestUrl = Regex.Replace(property.Name.ToLower(), @"\(.*?\)", string.Empty);

                        if (uniqueRequestUrlsTable.Add(requestUrl))
                        {
                            count++;

                            // Add the request url
                            _urlTemplateTable.Add(count.ToString(), new UriTemplate(requestUrl));

                            // Add the permission scopes
                            _scopesListTable.Add(count, property.Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Populates the delegated and application scopes information tables. 
        /// </summary>
        private void SeedScopesInfoTables()
        {
            string scopesInfoJson = _fileUtility.ReadFromFile(_scopesInformation).GetAwaiter().GetResult();

            if (!string.IsNullOrEmpty(scopesInfoJson))
            {
                ScopesInformationList scopesInformationList = JsonConvert.DeserializeObject<ScopesInformationList>(scopesInfoJson);

                foreach (ScopeInformation delegatedScopeInfo in scopesInformationList.DelegatedScopesList)
                {
                    _delegatedScopesInfoTable.Add(delegatedScopeInfo.ScopeName, delegatedScopeInfo);
                }

                foreach (ScopeInformation applicationScopeInfo in scopesInformationList.ApplicationScopesList)
                {
                    _applicationScopesInfoTable.Add(applicationScopeInfo.ScopeName, applicationScopeInfo);
                }
            }
        }

        /// <summary>
        /// Retrieves the permission scopes for a given request url.
        /// </summary>
        /// <param name="requestUrl">The target request url whose scopes are to be retrieved.</param>
        /// <param name="method">The target http verb of the request url whose scopes are to be retrieved.</param>
        /// <param name="scopeType">The type of scope to be retrieved for the target request url.</param>
        /// <returns>A list of scopes for the target request url given a http verb and type of scope.</returns>
        public List<ScopeInformation> GetScopes(string requestUrl = null, string method = "GET", string scopeType = "DelegatedWork")
        {
            if (!_scopesListTable.Any())
            {
                throw new InvalidOperationException($"The permissions and scopes data sources are empty; " +
                    $"check the source file or check whether the file path is properly set. File path: {_permissionsFilePaths}");
            }

            try
            {
                JArray resultValue = new JArray();
                string[] scopes = null;

                if (string.IsNullOrEmpty(requestUrl))
                {
                    var listOfScopes = _scopesListTable.Values.ToArray();
                    List<string> permissionsList = new List<string>();

                    foreach (var scope in listOfScopes)
                    {
                        var result = (JArray)scope;

                        string[] permissions = result.FirstOrDefault()?
                        .SelectToken(scopeType)?
                        .Select(s => (string)s)
                        .ToArray();

                        if (permissions != null)
                        {
                            permissionsList.AddRange(permissions);
                        }
                            
                    }
                    if (permissionsList.Count > 0)
                    {
                        scopes = permissionsList.Distinct().ToArray();
                    }
                }
                else
                {
                    requestUrl = Regex.Replace(requestUrl, @"\?.*", string.Empty); // remove any query params
                    requestUrl = Regex.Replace(requestUrl, @"\(.*?\)", string.Empty); // remove any '(...)' resource modifiers 

                    // Check if requestUrl is contained in our Url Template table
                    TemplateMatch resultMatch = _urlTemplateTable.Match(new Uri(requestUrl.ToLower(), UriKind.RelativeOrAbsolute));

                    if (resultMatch == null)
                    {
                        return null;
                    }
                    resultValue = (JArray)_scopesListTable[int.Parse(resultMatch.Key)];

                    string[] permissions = resultValue.FirstOrDefault(x => x.Value<string>("HttpVerb") == method)?
                        .SelectToken(scopeType)?
                        .Select(s => (string)s)
                        .ToArray();

                    scopes = permissions;
                }

                List<ScopeInformation> scopesList = new List<ScopeInformation>();

                if (scopes != null)
                {

                    foreach (string scopeName in scopes)
                    {
                        ScopeInformation scopeInfo = null;
                        if (scopeType.Contains("Delegated"))
                        {
                            if (_delegatedScopesInfoTable.ContainsKey(scopeName))
                            {
                                scopeInfo = _delegatedScopesInfoTable[scopeName];
                            }
                        }
                        else // Application scopes
                        {
                            if (_applicationScopesInfoTable.ContainsKey(scopeName))
                            {
                                scopeInfo = _applicationScopesInfoTable[scopeName];
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

                return null;
            }
            catch (ArgumentException)
            {
                return null; // equivalent to no match for the given requestUrl
            }
        }
    }
}
