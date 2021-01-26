// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Common;
using FileService.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MockTestUtility
{
    /// <summary>
    /// Defines a Mock class that retrieves files from a directory path.
    /// </summary>
    public class FileUtilityMock : IFileUtility
    {
        public async Task<string> ReadFromFile(string filePathSource)
        {
            if (string.IsNullOrEmpty(filePathSource))
            {
                throw new ArgumentNullException(nameof(filePathSource), "Value cannot be null");
            }

            if (filePathSource.IndexOf(FileServiceConstants.DirectorySeparator) < 1)
            {
                throw new ArgumentException("Improperly formatted file path source.", nameof(filePathSource));
            }

            // Prepend the root directory notation since we're reading off of a relative folder location
            filePathSource = $".\\{filePathSource}";

            using StreamReader streamReader = new StreamReader(filePathSource);
            return await streamReader.ReadToEndAsync();
        }

        public async Task<string> ReadFromSource(HttpRequestMessage requestMessage)
        {
            if (requestMessage == null)
            {
                throw new ArgumentNullException(nameof(requestMessage), "Value cannot be null");
            }
            // Mock reading from an HTTP source.
            return await ReadFromFile(requestMessage.RequestUri.OriginalString);
        }

        public Task WriteToFile(string fileContents, string filePathSource)
        {
            // Not implemented
            return Task.CompletedTask;
        }
    }
}