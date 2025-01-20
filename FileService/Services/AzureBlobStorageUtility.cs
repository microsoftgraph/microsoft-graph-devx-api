// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Azure.Identity;
using FileService.Common;
using FileService.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;


namespace FileService.Services
{
    /// <summary>
    /// Implements an <see cref="IFileUtility"/> that reads from an Azure blob storage.
    /// </summary>
    public class AzureBlobStorageUtility : IFileUtility
    {
        private readonly IConfiguration _configuration;
        private readonly BlobServiceClient _blobServiceClient;

        public AzureBlobStorageUtility(IConfiguration configuration)
        {
            _configuration = configuration
                   ?? throw new ArgumentNullException(nameof(configuration), $"Value cannot be null: {nameof(configuration)}");

            var managedIdentityCredential = new ManagedIdentityCredential(_configuration["BlobStorage:Identity"]);
            _blobServiceClient = new BlobServiceClient(new Uri($"https://{_configuration["BlobStorage:AccountName"]}.blob.core.windows.net"), 
                managedIdentityCredential);
        }

        /// <summary>
        /// Gets the file path and parses its contents
        /// </summary>
        /// <param name="filePathSource"> The path of the file.</param>
        /// <returns>A json string of file contents.</returns>
        public async Task<string> ReadFromFileAsync(string filePathSource)
        {
            FileServiceHelper.CheckArgumentNullOrEmpty(filePathSource, nameof(filePathSource));
            CheckFileFormat(filePathSource);

            (var containerName, var blobName) = FileServiceHelper.RetrieveFilePathSourceValues(filePathSource);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            if (await containerClient.ExistsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobName);

                if (await blobClient.ExistsAsync())
                {
                    var response = await blobClient.DownloadAsync();
                    using (var streamReader = new StreamReader(response.Value.Content))
                    {
                        return await streamReader.ReadToEndAsync();
                    }
                }
                else
                {
                    throw new IOException($"The '{blobName}' blob doesn't exist.");
                }
            }
            else
            {
                throw new IOException($"The '{containerName}' container doesn't exist.");
            }
            throw new IOException("Failed to connect to the blob storage account.");
        }

        /// <summary>
        /// Allows one to edit the file.
        /// </summary>
        /// <param name="fileContents"> Contents of the file.</param>
        /// <param name="filePathSource"> The path of the file.</param>
        /// <returns></returns>
        public Task WriteToFileAsync(string fileContents, string filePathSource)
        {
            throw new NotImplementedException();
        }

        private static void CheckFileFormat(string filePathSource)
        {
            if (filePathSource.IndexOf(FileServiceConstants.DirectorySeparator) < 1)
            {
                throw new ArgumentException("Improperly formatted file path source.", nameof(filePathSource));
            }
        }
    }
}
