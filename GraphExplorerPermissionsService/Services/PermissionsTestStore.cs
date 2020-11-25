// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphExplorerPermissionsService.Interfaces;
using GraphExplorerPermissionsService.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GraphExplorerPermissionsService.Services
{
    /// <summary>
    /// Defines a method that retrieves raw permissions descriptions files from the devX Content repo.
    /// </summary>
    public class PermissionsTestStore : IPermissionsTestStore
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PermissionsTestStore(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task<List<ScopeInformation>> FetchPermissionsDescriptionsFromGithub(string locale, string org, string branchName)
        {
            var client = _httpClientFactory.CreateClient("github");
            // download sample query file contents
            var permissionsFilePath = string.Concat("https://raw.githubusercontent.com/", org, "/microsoft-graph-devx-content/", branchName, "/permissions/permissions-descriptions.json");
            var responseMessage = await client.GetAsync(permissionsFilePath);

            if (!(responseMessage.IsSuccessStatusCode))
            {
                return null;
            }

            var scopesInfoJson = await responseMessage.Content.ReadAsStringAsync();
            List<ScopeInformation> scopesInformationList = JsonConvert.DeserializeObject<List<ScopeInformation>>(scopesInfoJson);

            return scopesInformationList;
        }
    }
}
