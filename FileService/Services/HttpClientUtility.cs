// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Implements a <see cref="IHttpClientUtility"/> that reads documents from HTTP sources.
    /// </summary>
    public class HttpClientUtility: IHttpClientUtility
    {
        private readonly HttpClient _httpClient;
        
        /// <summary>
        /// Class constructor.
        /// </summary>
        public HttpClientUtility(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException("Value cannot be null");
        }

        /// <summary>
        /// Reads the contents of a document from an HTTP source.
        /// </summary>
        /// <param name="requestMessage">The HTTP request message.</param>
        /// <returns>The document contents from the HTTP source.</returns>
        public async Task<string> ReadFromDocument(HttpRequestMessage requestMessage)
        {
            if (requestMessage == null)
            {
                throw new ArgumentNullException(nameof(requestMessage), "Value cannot be null.");
            }

            requestMessage.Method = requestMessage.Method ?? HttpMethod.Get; // default is GET
            var httpResponseMessage = await _httpClient?.SendAsync(requestMessage);
            var fileContents = await httpResponseMessage?.Content.ReadAsStringAsync();

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new Exception(fileContents);
            }

        }
            return fileContents;
    }
}