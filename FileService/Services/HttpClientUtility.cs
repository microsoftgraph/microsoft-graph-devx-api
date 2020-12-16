// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Common;
using FileService.Interfaces;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Implements an <see cref="IFileUtility"/> that reads from Github blob storage.
    /// </summary>
    public class HttpClientUtility : IFileUtility
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Instantiates a new HTTP client and configures the default request headers
        /// </summary>
        /// <param name="baseUrl">The host url.</param>
        /// <param name="mediaType"> Specifies the content type of the response.</param>
        public HttpClientUtility(string baseUrl, string mediaType = FileServiceConstants.ApplicationJsonMediaType)
        {
            _httpClient = new HttpClient
            {
                // Configure a HTTPClient instance with the specified baseUrl
                BaseAddress = new Uri(baseUrl)
            };
            // GitHub API versioning and add a user agent
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
        }

        /// <summary>
        /// Gets the file path and parses its contents
        /// </summary>
        /// <param name="filePathSource"> the path of the file to be parsed.</param>
        /// <returns> A json string of file contents.</returns>
        public async Task<string> ReadFromFile(string filePathSource)
        {
            // Download sample query file contents from github
            var httpResponseMessage = await _httpClient.GetAsync(filePathSource);
            var fileContents = await httpResponseMessage.Content.ReadAsStringAsync();

            return fileContents;
        }

        /// <summary>
        /// Allows one to edit the file.
        /// </summary>
        /// <param name="fileContents"> Contents of the file.</param>
        /// <param name="filePathSource"> The path of the file.</param>
        /// <returns></returns>
        public async Task WriteToFile(string fileContents, string filePathSource)
        {
            throw new NotImplementedException();
        }
    }
}
