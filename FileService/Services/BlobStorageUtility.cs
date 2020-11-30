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
using System.Net.Http;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Implements an <see cref="IFileUtility"/> that reads from an Azure blob storage.
    /// </summary>
    public class BlobStorageUtility : IFileUtility
    {
        private readonly string _connectionString;
        private readonly IHttpClientFactory _httpClientFactory;
        private string _containerName;
        private string _blobName;

        public BlobStorageUtility(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _connectionString = configuration["BlobStorage:ConnectionString"];
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> ReadFromFile(string filePathSource, string host = null)
        {
            FileServiceHelper.CheckArgumentNullOrEmpty(filePathSource, nameof(filePathSource));

            if (!string.IsNullOrEmpty(host))
            {
                // download sample query file contents
                var client = _httpClientFactory.CreateClient(host);
                var files = await client.GetAsync(filePathSource);
                var fileContents = await files.Content.ReadAsStringAsync();

                return fileContents;
            }

            else
            {
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
        }

        public async Task WriteToFile(string fileContents, string filePathSource)
        {
            throw new NotImplementedException();
        }
    }
}
