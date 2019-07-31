using Newtonsoft.Json;
using System;

namespace GraphExplorerSamplesService
{
    public static class SamplesService
    {
        /// <summary>
        /// Deserializes a JSON string into a list of sample query objects.
        /// </summary>
        /// <param name="jsonString">The JSON string to be deserialized into a list of sample query objects.</param>
        /// <returns>The deserialized list of sample query objects.</returns>
        public static SampleQueriesList GetSampleQueriesList(string jsonString)
        {
            if(string.IsNullOrEmpty(jsonString))
            {
                throw new ArgumentNullException(nameof(jsonString), "The JSON string to be deserialized cannot be null.");
            }

            var sampleQueriesList = JsonConvert.DeserializeObject<SampleQueriesList>(jsonString);
            return sampleQueriesList;
        }
    }
}
