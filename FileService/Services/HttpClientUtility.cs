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
    /// Implements an <see cref="IFileUtility"/> that reads a file from an Http source.
    /// </summary>
    public class HttpClientUtility: IFileUtility
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// The base address of the HttpClient.
        /// </summary>
        public string BaseAddress { get; set; }

        /// <summary>
        /// Dictionary of key value pairs of request header values of the HttpClient.
        /// </summary>
        public Dictionary<string, string> RequestHeaderValues { get; set; } = null;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public HttpClientUtility(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Reads the contents of a file from an Http source.
        /// </summary>
        /// <param name="filePathSource">The relative uri of the file source.</param>
        /// <returns></returns>
        public async Task<string> ReadFromFile(string filePathSource)
        {
            if (string.IsNullOrEmpty(BaseAddress))
            {
                throw new ArgumentNullException(nameof(BaseAddress), "Property value cannot be null or empty.");
            }

            _httpClient.BaseAddress = new Uri(BaseAddress);

            if (RequestHeaderValues != null)
            {
                foreach (var item in RequestHeaderValues)
                {
                    _httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
                }
            }

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
