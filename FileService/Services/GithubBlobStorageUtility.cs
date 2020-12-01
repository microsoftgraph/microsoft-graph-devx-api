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
        public HttpClient _client { get; }

        public GithubBlobStorageUtility(HttpClient client)
        {
            // Configure a HTTPClient instance to interact and make calls to Github API
            client.BaseAddress = new Uri("https://api.github.com/");
            // GitHub API versioning and add a user agent
            client.DefaultRequestHeaders.Add("Accept",
                "application/vnd.github.v3+json");          
            client.DefaultRequestHeaders.Add("User-Agent",
                "HttpClientFactory-Sample");

            _client = client;            
        }     
      
        public async Task<string> ReadFromFile(string filePathSource)
        {
            // Download sample query file contents from github
            var sampleQueriesFiles = await _client.GetAsync(filePathSource);
            var fileContents = await sampleQueriesFiles.Content.ReadAsStringAsync();

            return fileContents;      
        }
        public async Task WriteToFile(string fileContents, string filePathSource)
        {
            throw new NotImplementedException();
        }
    }
}   
