// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Implements an <see cref="IFileUtility"/> that reads a file from an HTTP source.
    /// </summary>
    public class HttpClientUtility: IFileUtility
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// The request uri of the HTTP resource.
        /// </summary>
        public string RequestUri { get; set; }

        /// <summary>
        /// Dictionary of key value pairs of the HTTP request header values.
        /// </summary>
        public Dictionary<string, string> RequestHeaderValues { get; set; } = null;

        /// <summary>
        /// The HttpMethod to use when sending requests.
        /// </summary>
        public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public HttpClientUtility(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Reads the contents of a file from an HTTP source.
        /// </summary>
        /// <param name="filePathSource">The absolute uri of the file source.</param>
        /// <returns>The file contents from the HTTP source.</returns>
        public async Task<string> ReadFromFile(string filePathSource)
        {
            if (string.IsNullOrEmpty(filePathSource))
            {
                throw new ArgumentNullException(nameof(filePathSource), "Value cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(RequestUri))
            {
                throw new ArgumentNullException(nameof(RequestUri), "Property value cannot be null or empty.");
            }

            var requestMessage = new HttpRequestMessage(HttpMethod, RequestUri);

            if (RequestHeaderValues != null)
            {
                // Set the request headers
                foreach (var item in RequestHeaderValues)
                {
                    requestMessage.Headers.Add(item.Key, item.Value);
                }
            }

            var httpResponseMessage = await _httpClient.SendAsync(requestMessage);
            var fileContents = await httpResponseMessage.Content.ReadAsStringAsync();

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new Exception(fileContents);
            }

            return fileContents;
        }

        public Task WriteToFile(string fileContents, string filePathSource)
        {
            throw new NotImplementedException();
        }
    }
}
