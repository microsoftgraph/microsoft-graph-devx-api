// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Net.Http;
using System.Threading.Tasks;

namespace FileService.Interfaces
{
    /// <summary>
    /// Provides a contract for reading documents from HTTP sources.
    /// </summary>
    public interface IHttpClientUtility
    {
        Task<string> ReadFromDocumentAsync(HttpRequestMessage requestMessage);

        HttpClient GetHttpClient();
    }
}
