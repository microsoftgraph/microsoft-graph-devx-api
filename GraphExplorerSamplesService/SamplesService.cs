using Newtonsoft.Json;
using System;
using System.IO;

namespace GraphExplorerSamplesService
{
    public class SamplesService
    {
        /// <summary>
        /// Deserializes a JSON string into a list of sample query objects.
        /// </summary>
        /// <param name="jsonString">The JSON string to be deserialized into a list of sample query objects.</param>
        /// <exception cref="JsonException">Thrown when an error occurs during deserialization.</exception>
        /// <exception cref="Exception">Thrown when any other error occurs during deserialization.</exception>
        /// <returns>The deserialized list of sample query objects.</returns>
        public static SampleQueriesList GetSampleQueriesList(string jsonString)
        {
            try
            {
                SampleQueriesList sampleQueriesList = JsonConvert.DeserializeObject<SampleQueriesList>(jsonString);
                return sampleQueriesList;
            }
            catch (JsonException jsonException)
            {
                throw jsonException;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }
    }
}
