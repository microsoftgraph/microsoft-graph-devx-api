using FileService.Common;
using FileService.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MockTestUtility
{
    public class HttpClientUtilityMock : IFileUtility
    {      
        public async Task<string> ReadFromFile(string filePathSource)
        {
            if (filePathSource.IndexOf(FileServiceConstants.DirectorySeparator) < 1)
            {
                throw new ArgumentException("Improperly formatted file path source.", nameof(filePathSource));
            }

            // Prepend the root directory notation since we're reading off of a relative folder location
            filePathSource = $".\\GithubTestFiles\\{filePathSource}";

            // Mock reading from github repo
            using (StreamReader streamReader = new StreamReader(filePathSource))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        public async Task WriteToFile(string fileContents, string filePathSource)
        {
            // Not implemented
        }
    }
}
