using FileService.Interfaces;
using GraphExplorerPermissionsService.Interfaces;
using Microsoft.Extensions.Configuration;
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
        private readonly IDictionary<int, object> _scopesTable;
        private readonly IFileUtility _fileUtility;
        private readonly List<string> _permissionsFilePaths;        

        public PermissionsStore(IFileUtility fileUtility, IConfiguration configuration)
        {
            try
            {
                _urlTemplateTable = new UriTemplateTable();
                _scopesTable = new Dictionary<int, object>();
                _fileUtility = fileUtility;
                _permissionsFilePaths = configuration.GetSection("Permissions:FilePaths").Get<List<string>>();

                SeedTables();
            }
            catch
            {
                throw;
            }            
        }

        /// <summary>
        /// Populates the template table with the request urls and the scopes table with the permission scopes.
        /// </summary>
        private void SeedTables()
        {
            try
            {
                HashSet<string> uniqueRequestUrlsTable = new HashSet<string>();
                int count = 0;

                foreach (string permissionPath in _permissionsFilePaths)
                {
                    string jsonString = _fileUtility.ReadFromFile(permissionPath).GetAwaiter().GetResult();

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
                                _scopesTable.Add(count, property.Value);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Do nothing; the tables will just be empty
            }                  
        }

        /// <summary>
        /// Retrieves the permission scopes for a given request url.
        /// </summary>
        /// <param name="requestUrl">The target request url whose scopes are to be retrieved.</param>
        /// <param name="method">The target http verb of the request url whose scopes are to be retrieved.</param>
        /// <param name="scopeType">The type of scope to be retrieved for the target request url.</param>
        /// <returns>A list of scopes for the target request url given a http verb and type of scope.</returns>
        public string[] GetScopes(string requestUrl, string method = "GET", string scopeType = "DelegatedWork")
        {
            if (!_scopesTable.Any())
            {
                throw new InvalidOperationException($"The permissions and scopes data sources are empty; " +
                    $"check the source file or check whether the file path is properly set. File path: {_permissionsFilePaths}");
            }
            if (string.IsNullOrEmpty(requestUrl))
            {
                throw new ArgumentNullException(nameof(requestUrl), "The request url cannot be null or empty.");
            }

            try
            {
                requestUrl = Regex.Replace(requestUrl.ToLower(), @"\(.*?\)", string.Empty);

                // Check if requestUrl is contained in our Url Template table
                TemplateMatch resultMatch = _urlTemplateTable.Match(new Uri(requestUrl, UriKind.RelativeOrAbsolute));

                if (resultMatch == null)
                {
                    return null;
                }

                JArray resultValue = (JArray)_scopesTable[int.Parse(resultMatch.Key)];

                string[] scopes = resultValue.FirstOrDefault(x => x.Value<string>("HttpVerb") == method)?
                    .SelectToken(scopeType)?
                    .Select(s => (string)s)
                    .ToArray();

                return scopes ?? null;
            }
            catch (ArgumentException)
            {
                return null; // equivalent to no match for the given requestUrl
            }                      
        }
    }
}
