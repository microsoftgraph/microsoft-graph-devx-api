// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UtilityService;

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
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient), $"Value cannot be null: { nameof(httpClient) }");
        }

        /// <summary>
        /// Reads the contents of a document from an HTTP source.
        /// </summary>
        /// <param name="requestMessage">The HTTP request message.</param>
        /// <returns>The document contents from the HTTP source.</returns>
        public async Task<string> ReadFromDocumentAsync(HttpRequestMessage requestMessage)
        {
            UtilityFunctions.CheckArgumentNull(requestMessage, nameof(requestMessage));

            requestMessage.Method ??= HttpMethod.Get; // default is GET

            using var httpResponseMessage = await _httpClient?.SendAsync(requestMessage);
            if (httpResponseMessage == null)
            {
                throw new InvalidOperationException("Invalid HttpRequestMessage provided");
            }

            return await httpResponseMessage.Content?.ReadAsStringAsync();
        }
    }
}
