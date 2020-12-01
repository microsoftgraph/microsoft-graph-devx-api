using FileService.Common;
using FileService.Interfaces;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Implements an <see cref="IFileUtility"/> that reads from Github blob storage.
    /// </summary>
    public class GithubBlobStorageUtility : IFileUtility
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public GithubBlobStorageUtility(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }       
       
        public async Task<string> ReadFromFile(string filePathSource)
        {
            string hostName = FileServiceConstants.HostName;
          
            // Download sample query file contents from github
            var client = _httpClientFactory.CreateClient(hostName);
            var sampleQueriesFiles = await client.GetAsync(filePathSource);
            var fileContents = await sampleQueriesFiles.Content.ReadAsStringAsync();

            return fileContents;      
        }
        public async Task WriteToFile(string fileContents, string filePathSource)
        {
            throw new NotImplementedException();
        }
    }
}   
