// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UtilityService;

namespace MockTestUtility
{
    /// <summary>
    /// Defines a Mock class that retrieves files from a directory path.
    /// </summary>
    public class FileUtilityMock : IFileUtility, IHttpClientUtility
    {
        public async Task<string> ReadFromFileAsync(string filePathSource)
        {
            UtilityFunctions.CheckArgumentNull(filePathSource, nameof(filePathSource));

            // Prepend the root directory notation since we're reading off of a relative folder location
            filePathSource = Path.Combine(Environment.CurrentDirectory, filePathSource);
            if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // path separator kinds are mixed on linux platforms because of the configuration files, which leads to failures
                filePathSource = filePathSource.Replace('\\', Path.DirectorySeparatorChar);

            using StreamReader streamReader = new StreamReader(filePathSource);
            return await streamReader.ReadToEndAsync();
        }

        public async Task<string> ReadFromDocumentAsync(HttpRequestMessage requestMessage)
        {
            UtilityFunctions.CheckArgumentNull(requestMessage, nameof(requestMessage));

            // Mock reading from an HTTP source.
            return await ReadFromFileAsync(requestMessage.RequestUri.OriginalString);
        }

        public Task WriteToFileAsync(string fileContents, string filePathSource)
        {
            // Not implemented
            return Task.CompletedTask;
        }
    }
}
