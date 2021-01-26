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
    /// Defines a Mock class that simulates retrieving blobs from an Azure Blob Storage
    /// </summary>
    public class AzureBlobStorageUtilityMock : IFileUtility
    {
        public async Task<string> ReadFromFile(string filePathSource)
        {
            if (filePathSource.IndexOf(FileServiceConstants.DirectorySeparator) < 1)
            {
                throw new ArgumentException("Improperly formatted file path source.", nameof(filePathSource));
            }

            // Prepend the root directory notation since we're reading off of a relative folder location
            filePathSource = $".\\{filePathSource}";

            // Mock reading blob source from upstream Azure storage account
            using (StreamReader streamReader = new StreamReader(filePathSource))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        public Task<string> ReadFromSource(HttpRequestMessage requestMessage)
        {
            throw new NotImplementedException();
        }

        public Task WriteToFile(string fileContents, string filePathSource)
        {
            // Not implemented
            return Task.CompletedTask;
        }
    }
}
