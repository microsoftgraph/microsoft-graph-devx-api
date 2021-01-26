// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
//

using System.Net.Http;
using System.Threading.Tasks;

namespace FileService.Interfaces
{
    /// <summary>
    /// Provides a contract for reading from HTTP file sources.
    /// </summary>
    public interface IHttpClientUtility
    {
        Task<string> ReadFromSource(HttpRequestMessage requestMessage);
        Task WriteToFile(string fileContents, string filePathSource);
    }
}