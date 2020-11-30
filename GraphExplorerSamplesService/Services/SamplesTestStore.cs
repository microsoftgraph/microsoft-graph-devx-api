// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Common;
using FileService.Interfaces;
using GraphExplorerSamplesService.Interfaces;
using GraphExplorerSamplesService.Models;
using Microsoft.Extensions.Configuration;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _sampleQueriesContainerName;
        private readonly string _sampleQueriesBlobName;
        private readonly string  _host;
        private readonly string _repo;

        public SamplesTestStore(IHttpClientFactory httpClientFactory, IFileUtility fileUtility, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _host = _configuration["BlobStorage:GithubHost"];
            _repo = _configuration["BlobStorage:RepoName"];
            _sampleQueriesContainerName = _configuration["BlobStorage:Containers:SampleQueries"];
            _sampleQueriesBlobName = _configuration[$"BlobStorage:Blobs:SampleQueries"];
        }

        /// <summary>
        /// Fetches the sample query files from Github and returns a deserialized instance a
        ///  /// <see cref="SampleQueriesList"/> from this.
        /// </summary>
        /// <param name="locale"></param>
        /// <param name="org"></param>
        /// <param name="branchName"></param>
        /// <returns>The deserialized instance of a <see cref="SampleQueriesList"/>.</returns>
        public async Task<SampleQueriesList> FetchSampleQueriesListAsync(string locale, string org, string branchName)
        {
            string source = "github";
            var client = _httpClientFactory.CreateClient(source);
            
            /* download sample query file contents*/

            // Fetch the requisite sample path source based on the locale
            string queriesFilePathSource = FileServiceHelper.GetLocalizedFilePathSource(_sampleQueriesContainerName, _sampleQueriesBlobName, locale, source);

            var completeFilePath = string.Concat(_host, org, _repo, branchName, FileServiceConstants.GithubDirectorySeparator, queriesFilePathSource);
            var responseMessage = await client.GetAsync(completeFilePath);

            if (!(responseMessage.IsSuccessStatusCode))
            {
                return null;
            }

            var fileContents = await responseMessage.Content.ReadAsStringAsync();

            //order English translation of the sample queries
            bool orderSamples = locale.Equals("en-us", StringComparison.OrdinalIgnoreCase);
            
            SampleQueriesList sampleQueriesList = SamplesService.DeserializeSampleQueriesList(fileContents, orderSamples);
            
            return sampleQueriesList;
        }
    }
}
