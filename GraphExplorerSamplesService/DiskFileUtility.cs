using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GraphExplorerSamplesService
{
    public class DiskFileUtility : IFileUtility
    {
        /// <summary>
        /// Reads from a provided JSON file from disk.
        /// </summary>
        /// <param name="filePathSource">The directory path name of the JSON file.</param>
        /// <exception cref="FileNotFoundException">Thrown when the provided file path name is unable to be resolved.</exception>
        /// <returns>An enumerable collection of all the Graph Explorer sample queries in the provided JSON file.</returns>
        //public SampleQueriesList ReadFromFile(string filePathSource)
        //{
        //    try
        //    {
        //        if (File.Exists(filePathSource))
        //        {
        //            using (StreamReader streamReader = new StreamReader(filePathSource))
        //            {
        //                string samplesQueriesString = streamReader.ReadToEnd();
        //                SampleQueriesList sampleQueriesList = JsonConvert.DeserializeObject<SampleQueriesList>(samplesQueriesString);
        //                return sampleQueriesList;
        //            }
        //        }
        //        else
        //        {
        //            throw new FileNotFoundException("File not found!", filePathSource);
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        throw exception;
        //    }
        //}

        
        public ReadFromFile(string filePathSource)
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(filePathSource))
                {
                    string fileContent = streamReader.ReadToEnd();
                    return fileContent;
                }
            }
            catch(DirectoryNotFoundException dirException)
            {
                throw dirException;
            }
            catch(FileNotFoundException fileException)
            {
                throw fileException;
            }
            catch (IOException ioException)
            {
                throw ioException;
            }
        }
    }
}
