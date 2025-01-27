// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Implements an <see cref="IFileUtility"/> that reads from and writes contents to a file on disk.
    /// </summary>
    public class DiskFileUtility : IFileUtility
    {
        /// <summary>
        /// Reads the contents of a provided file on disk.
        /// </summary>
        /// <param name="filePathSource">The directory path name of the file on disk.</param>
        /// <returns>The contents of the file.</returns>
        public async Task<string> ReadFromFileAsync(string filePathSource)
        {
            using (StreamReader streamReader = new StreamReader(filePathSource))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Writes contents to a provided file on disk.
        /// </summary>
        /// <param name="fileContents">The string content to be written.</param>
        /// <param name="filePathSource">The directory path name of the file on disk.</param>
        /// <returns></returns>
        public async Task WriteToFileAsync(string fileContents, string filePathSource)
        {
            using (StreamWriter streamWriter = new StreamWriter(filePathSource))
            {
                await streamWriter.WriteLineAsync(fileContents);
            }
        }
    }
}
