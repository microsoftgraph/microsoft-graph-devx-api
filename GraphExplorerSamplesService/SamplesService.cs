using Newtonsoft.Json;
using System;
using System.IO;

namespace GraphExplorerSamplesService
{
    public class SamplesService : ISamplesService
    {
        /// <summary>
        /// Reads from a provided JSON file.
        /// </summary>
        /// <param name="filePathName">The directory path name of the JSON file.</param>
        /// <returns>A list of all the Graph Explorer Sample Queries in the provided JSON file.</returns>
        public SampleQueriesList ReadFromJsonFile(string filePathName)
        {
            try
            {
                if (File.Exists(filePathName))
                {
                    using (StreamReader streamReader = new StreamReader(filePathName))
                    {
                        string samplesQueriesString = streamReader.ReadToEnd();
                        SampleQueriesList sampleQueriesList = JsonConvert.DeserializeObject<SampleQueriesList>(samplesQueriesString);
                        return sampleQueriesList;
                    }
                }
                else
                {
                    throw new FileNotFoundException("File not found!", filePathName);
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }            
        }
    }
}
