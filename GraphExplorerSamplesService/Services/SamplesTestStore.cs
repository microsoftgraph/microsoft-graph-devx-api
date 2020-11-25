// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphExplorerSamplesService.Interfaces;
using GraphExplorerSamplesService.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GraphExplorerSamplesService.Services
{
    /// <summary>
    /// Defines a method that retrieves raw sample query files from the devX Content repo.
    /// </summary>
    public class SamplesTestStore : ISamplesTestStore
    {
        //private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;

        public SamplesTestStore(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Fetches the sample query files from Github and returns a deserialized instance a
        ///  /// <see cref="SampleQueriesList"/> from this.
        /// </summary>
        /// <param name="locale"></param>
        /// <param name="org"></param>
        /// <param name="branchName"></param>
        /// <returns>The deserialized instance of a <see cref="SampleQueriesList"/>.</returns>
        public async Task<SampleQueriesList> FetchSampleQueriesFromGithub(string locale, string org, string branchName)
        {
            var client = _httpClientFactory.CreateClient("github");

            // download sample query file contents
            var samplesFilePath = string.Concat("https://raw.githubusercontent.com/", org, "/microsoft-graph-devx-content/", branchName, "/sample-queries/sample-queries.json");
            var responseMessage = await client.GetAsync(samplesFilePath);

            //order English translation of the sample queries
            bool orderSamples = locale.Equals("en-us", StringComparison.OrdinalIgnoreCase);

            if (!(responseMessage.IsSuccessStatusCode))
            {
                return null;
            }
            
            var fileContents = await responseMessage.Content.ReadAsStringAsync();
            SampleQueriesList sampleQueriesList = SamplesService.DeserializeSampleQueriesList(fileContents, orderSamples);

            
            return sampleQueriesList;
        }
    }
}
