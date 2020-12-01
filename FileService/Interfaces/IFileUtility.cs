// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace FileService.Interfaces
{
    /// <summary>
    /// Provides a contract for reading from and writing to file sources.
    /// </summary>
    public interface IFileUtility
    {
        // Reads file contents from a blob storage account given the source of the path
        Task<string> ReadFromFile(string filePathSource);

        // Allows one to make changes to a file
        Task WriteToFile(string fileContents, string filePathSource);
    }
}
