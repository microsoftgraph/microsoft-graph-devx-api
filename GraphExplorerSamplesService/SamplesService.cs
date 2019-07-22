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
        /// <exception cref="FileNotFoundException">Thrown when the provided file path name is unable to be resolved.</exception>
        /// <returns>An enumerable collection of all the Graph Explorer sample queries in the provided JSON file.</returns>
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
