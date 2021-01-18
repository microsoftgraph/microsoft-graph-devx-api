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
    /// Implements an <see cref="IFileUtility"/> that reads a file from an HTTP source.
    /// </summary>
    public class HttpClientUtility: IFileUtility
    {
        private readonly HttpClient _httpClient;
        
        /// <summary>
        /// Class constructor.
        /// </summary>
        public HttpClientUtility(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
                
        public  Task<string> ReadFromFile(string filePathSource)
        {            
            throw new NotImplementedException();
        }

        public Task WriteToFile(string fileContents, string filePathSource)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads the contents of a file from an HTTP source.
        /// </summary>
        /// <param name="requestMessage">The HTTP request mesaage.</param>
        /// <returns>The file contents from the HTTP source.</returns>
        public async Task<string> ReadFromHttpSource(HttpRequestMessage requestMessage)
        {
            if (requestMessage == null)
            {
                throw new ArgumentNullException(nameof(requestMessage), "Value cannot be null.");
            }

            requestMessage.Method = requestMessage.Method ?? HttpMethod.Get; // default is GET
            var httpResponseMessage = await _httpClient.SendAsync(requestMessage);
            var fileContents = await httpResponseMessage.Content.ReadAsStringAsync();

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new Exception(fileContents);
            }

            return fileContents;
        }
    }
}
