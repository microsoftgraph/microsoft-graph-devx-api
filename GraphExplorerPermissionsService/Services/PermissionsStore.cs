using FileService.Interfaces;
using GraphExplorerPermissionsService.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Tavis.UriTemplates;

namespace GraphExplorerPermissionsService
{
    public class PermissionsStore : IPermissionsStore
    {
        private readonly UriTemplateTable _urlTemplateTable;
        private readonly IDictionary<int, object> _scopesTable;
        private readonly IFileUtility _fileUtility;
        private readonly string _permissionsFilePathSource;        

        public PermissionsStore(IFileUtility fileUtility, IConfiguration configuration)
        {
            _urlTemplateTable = new UriTemplateTable();
            _scopesTable = new Dictionary<int, object>();
            _fileUtility = fileUtility;
            _permissionsFilePathSource = configuration["Permissions:PermissionsAndScopesFilePathName"];

            SeedTables();
        }

        /// <summary>
        /// Populates the template table with the request urls and the scopes table with the permission scopes.
        /// </summary>
        private void SeedTables()
        {
            try
            {
                string jsonString = _fileUtility.ReadFromFile(_permissionsFilePathSource).GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(jsonString))
                {
                    JObject permissionsObject = JObject.Parse(jsonString);
                                        
                    JToken apiPermissions = permissionsObject.First.First;

                    int count = 0;
                    foreach (JProperty property in apiPermissions)
                    {
                        count++;

                        // Add the request url
                        _urlTemplateTable.Add(count.ToString(), new UriTemplate(property.Name));

                        // Add the permission scopes
                        _scopesTable.Add(count, property.Value);
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
        /// <param name="httpVerb">The target http verb of the request url whose scopes are to be retrieved.</param>
        /// <param name="scopeType">The type of scope to be retrieved for the target request url.</param>
        /// <returns>A list of scopes for the target request url given a http verb and type of scope.</returns>
        public string[] GetScopes(string requestUrl, string httpVerb = "GET", string scopeType = "DelegatedWork")
        {
            if (!_scopesTable.Any())
            {
                throw new InvalidOperationException($"The permissions and scopes data sources are empty; " +
                    $"check the source file or check whether the file path is properly set. File path: {_permissionsFilePathSource}");
            }
            if (string.IsNullOrEmpty(requestUrl))
            {
                throw new ArgumentNullException(nameof(requestUrl), "The request url cannot be null or empty.");
            }

            try
            {
                // Check if requestUrl is contained in our Url Template table
                var resultMatch = _urlTemplateTable.Match(new Uri(requestUrl, UriKind.RelativeOrAbsolute));

                if (resultMatch == null)
                {
                    return null;
                }

                var resultValue = (JArray)_scopesTable[int.Parse(resultMatch.Key)];

                string[] scopes = resultValue.FirstOrDefault(x => x.Value<string>("HttpVerb") == httpVerb)?
                    .SelectToken(scopeType)?
                    .Select(s => (string)s)
                    .ToArray();

                if (scopes == null || scopes.Count() == 0)
                {
                    return null;
                }

                return scopes;
            }
            catch (ArgumentException)
            {
                return null; // equivalent to no match for the given requestUrl
            }                      
        }
    }
}
