using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace GraphExplorerSamplesService
{
    public static class SamplesService
    {
        /// <summary>
        /// Deserializes a JSON string into a list of sample query objects.
        /// </summary>
        /// <param name="jsonString">The JSON string to be deserialized into a list of sample query objects.</param>
        /// <exception cref="ArgumentNullException">Thrown when the parameter is empty or null.</exception>
        /// <exception cref="JsonReaderException">Thrown when an error occurs when reading the JSON string.</exception>
        /// <exception cref="JsonException">Thrown when an error occurs during deserialization.</exception>
        /// <returns>The deserialized list of sample query objects.</returns>
        public static SampleQueriesList GetSampleQueriesList(string jsonString)
        {
            if(string.IsNullOrEmpty(jsonString))
            {
                throw new ArgumentNullException(nameof(jsonString), "The JSON string to be deserialized cannot be null.");
            }

            try
            {
                var sampleQueriesList = JsonConvert.DeserializeObject<SampleQueriesList>(jsonString);
                return sampleQueriesList;
            }
            catch(JsonReaderException readerException)
            {
                throw readerException;
            }
            catch (JsonException jsonException)
            {
                throw jsonException;
            } 
        }
    }
}
