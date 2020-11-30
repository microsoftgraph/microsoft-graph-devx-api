// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Common;
using FileService.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Implements an <see cref="IFileUtility"/> that reads from an Azure blob storage.
    /// </summary>
    public class AzureBlobStorageUtility : IFileUtility
    {
        private readonly string _connectionString;
        private string _containerName;
        private string _blobName;

        public AzureBlobStorageUtility(IConfiguration configuration)
        {
            _connectionString = configuration["BlobStorage:ConnectionString"];
        }

        public async Task<string> ReadFromFile(string filePathSource)
        {
            FileServiceHelper.CheckArgumentNullOrEmpty(filePathSource, nameof(filePathSource));

            if (filePathSource.IndexOf(FileServiceConstants.AzureDirectorySeparator) < 1)
            {
                throw new ArgumentException("Improperly formatted file path source.", nameof(filePathSource));
            }

            (_containerName, _blobName) = FileServiceHelper.RetrieveFilePathSourceValues(filePathSource);

            if (CloudStorageAccount.TryParse(_connectionString, out CloudStorageAccount storageAccount))
            {
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(_containerName);

                if (await container.ExistsAsync())
                {
                    CloudBlockBlob blob = container.GetBlockBlobReference(_blobName);

                    if (await blob.ExistsAsync())
                    {
                        return await blob.DownloadTextAsync();
                    }
                    else
                    {
                        throw new IOException($"The '{_blobName}' blob doesn't exist.");
                    }
                }
                else
                {
                    throw new IOException($"The '{_containerName}' container doesn't exist.");
                }
            }

            throw new IOException("Failed to connect to the blob storage account.");
        }

        public async Task WriteToFile(string fileContents, string filePathSource)
        {
            throw new NotImplementedException();
        }
    }
}
