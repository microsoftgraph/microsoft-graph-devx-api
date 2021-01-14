// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Common;
using FileService.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MockTestUtility
{
    /// <summary>
    /// Defines a Mock class that simulates retrieving blobs from a remote blob storage container
    /// </summary>
    public class FileUtilityMock : IFileUtility
    {
        /// <summary>
        /// Gets the file path and parses its contents
        /// </summary>
        /// <param name="filePathSource"> The path of the file.</param>
        /// <returns>A json string of file contents.</returns>
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

        /// <summary>
        /// Allows one to edit the file.
        /// </summary>
        /// <param name="fileContents"> Contents of the file.</param>
        /// <param name="filePathSource"> The path of the file.</param>
        /// <returns></returns>
        public Task WriteToFile(string fileContents, string filePathSource)
        {
            // Not implemented
            return Task.CompletedTask;
        }
    }
}
