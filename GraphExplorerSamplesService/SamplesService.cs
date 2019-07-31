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

        /// <summary>
        /// Updates a list of sample queries with a sample query model object.
        /// </summary>
        /// <param name="sampleQueriesList">The samples queries list to be updated.</param>
        /// <param name="sampleQueryModel">The sample query model object to update.</param>
        /// <param name="sampleQueryId">The Id value of the sample query model object to be updated.</param>
        /// <returns>The list of updated samples queries.</returns>
        public static SampleQueriesList UpdateSampleQueriesList(SampleQueriesList sampleQueriesList, SampleQueryModel sampleQueryModel, Guid sampleQueryId)
        {
            if (sampleQueriesList == null)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of Sample Queries cannot be null.");
            }
            if (sampleQueryModel == null)
            {
                throw new ArgumentNullException(nameof(sampleQueryModel), "The Sample Query model object cannot be null.");
            }

            // Check if the sample query model exists in the sample queries list using its Id
            int sampleQueryIndex = sampleQueriesList.SampleQueries.FindIndex(x => x.Id == sampleQueryId);
            if (sampleQueryIndex >= 0)
            {
                // Insert the new sample query model into the sample queries list at the index of the original sample query model
                sampleQueriesList.SampleQueries.Insert(sampleQueryIndex, sampleQueryModel);

                // Remove the original sample query from the next index
                sampleQueriesList.SampleQueries.RemoveAt(++sampleQueryIndex);

                // Return the list 
                return sampleQueriesList;
            }
            else // sample query model object is not in the sample queries list
            {
                return null;
            }
        }

        /// <summary>
        /// Serializes a list of samples queries into a JSON string.
        /// </summary>
        /// <param name="sampleQueriesList">The samples queries list to be deserialized.</param>
        /// <returns>The serialized JSON string from a list of samples queries.</returns>
        public static string SerializeSampleQueriesList (SampleQueriesList sampleQueriesList)
        {
            if (sampleQueriesList == null)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of Sample Queries cannot be null.");
            }

            var sampleQueriesJson = JsonConvert.SerializeObject(sampleQueriesList);
            return sampleQueriesJson;
        }

        /// <summary>
        /// Adds a sample query model object into a list of samples queries list.
        /// </summary>
        /// <param name="sampleQueriesList">The samples queries list which the sample query model object will be added into.</param>
        /// <param name="sampleQueryModel">The sample query model object to be added.</param>
        /// <returns>The list of samples queries with the newly added sample query model object.</returns>
        public static SampleQueriesList AddToSampleQueriesList(SampleQueriesList sampleQueriesList, ref SampleQueryModel sampleQueryModel)
        {
            if (sampleQueriesList == null)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of Sample Queries cannot be null.");
            }
            if (sampleQueryModel == null)
            {
                throw new ArgumentNullException(nameof(sampleQueryModel), "The Sample Query model object cannot be null.");
            }

            // Append new GUID Id to the new sample query
            sampleQueryModel.Id = Guid.NewGuid();

            // Add the new sample query into the samples queries list
            sampleQueriesList.SampleQueries.Add(sampleQueryModel);

            // Return the samples queries list
            return sampleQueriesList;
        }
    }
}
