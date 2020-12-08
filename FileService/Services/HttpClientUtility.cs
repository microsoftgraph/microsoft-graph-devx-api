// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Implements an <see cref="IFileUtility"/> that reads a file from an Http source.
    /// </summary>
    public class HttpClientUtility: IFileUtility
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="baseUrl">The base url.</param>
        /// <param name="requestHeaderValues">Dictionary of key value pairs of request header values.</param>
        public HttpClientUtility(string baseUrl, Dictionary<string, string> requestHeaderValues = null)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException(nameof(baseUrl), "Value cannot be null or empty.");
            }

            _httpClient = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip
            })
            {
                BaseAddress = new Uri(baseUrl)
            };

            // Set the Accept-Encoding header
            _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            // Set the request header values
            if (requestHeaderValues != null)
            {
                foreach (var item in requestHeaderValues)
                {
                    _httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
                }
            }
        }

        /// <summary>
        /// Reads the contents of a file from an Http source.
        /// </summary>
        /// <param name="filePathSource">The relative uri of the file source.</param>
        /// <returns></returns>
        public async Task<string> ReadFromFile(string filePathSource)
        {
            var httpResponseMessage = await _httpClient.GetAsync(filePathSource);
            var fileContents = await httpResponseMessage.Content.ReadAsStringAsync();

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new Exception(fileContents);
            }

            return fileContents;
        }

        public async Task WriteToFile(string fileContents, string filePathSource)
        {
            throw new NotImplementedException();
        }
    }
}
