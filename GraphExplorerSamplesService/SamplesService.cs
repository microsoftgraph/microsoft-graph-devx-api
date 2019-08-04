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

        /// <summary>
        /// Serializes a list of sample queries into a JSON string.
        /// </summary>
        /// <param name="sampleQueriesList">The list of sample queries to be deserialized.</param>
        /// <returns>The serialized JSON string from a list of sample queries.</returns>
        public static string SerializeSampleQueriesList(SampleQueriesList sampleQueriesList)
        {
            if (sampleQueriesList == null)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of sample queries cannot be null.");
            }

            var sampleQueriesJson = JsonConvert.SerializeObject(sampleQueriesList);
            return sampleQueriesJson;
        }

        /// <summary>
        /// Updates a sample query model object in a list of sample queries.
        /// </summary>
        /// <param name="sampleQueriesList">The list of sample queries which contains the sample query model object be updated.</param>
        /// <param name="sampleQueryModel">The sample query model object to update.</param>
        /// <param name="sampleQueryId">The Id value of the sample query model object to be updated.</param>
        /// <returns>The updated list of sample queries.</returns>
        public static SampleQueriesList UpdateSampleQueriesList(SampleQueriesList sampleQueriesList, SampleQueryModel sampleQueryModel, Guid sampleQueryId)
        {
            if (sampleQueriesList == null)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of sample queries cannot be null.");
            }
            if (sampleQueryModel == null)
            {
                throw new ArgumentNullException(nameof(sampleQueryModel), "The sample query model object cannot be null.");
            }
            if (sampleQueryId == null)
            {
                throw new ArgumentNullException(nameof(sampleQueryId), "The sample query id cannot be null.");
            }

            // Check if the sample query model exists in the list of sample queries using its Id
            int sampleQueryIndex = sampleQueriesList.SampleQueries.FindIndex(x => x.Id == sampleQueryId);

            if (sampleQueryIndex < 0)
            {
                // sample query model object is not in the list of sample queries
                return null;                
            }

            // Insert the new sample query object into the list of sample queries at the index of the original sample query object
            sampleQueriesList.SampleQueries.Insert(sampleQueryIndex, sampleQueryModel);

            // Original sample query object pushed to the next index; increment index then remove it
            sampleQueriesList.SampleQueries.RemoveAt(++sampleQueryIndex);

            // Return the new list of sample queries
            return sampleQueriesList;
        }               

        /// <summary>
        /// Adds a sample query model object into a list of sample queries.
        /// </summary>
        /// <param name="sampleQueriesList">The list of sample queries which the sample query model object will be added into.</param>
        /// <param name="sampleQueryModel">The sample query model object to be added.</param>
        /// <returns>The list of sample queries with the newly added sample query model object.</returns>
        public static SampleQueriesList AddToSampleQueriesList(SampleQueriesList sampleQueriesList, ref SampleQueryModel sampleQueryModel)
        {
            if (sampleQueriesList == null)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of sample queries cannot be null.");
            }
            if (sampleQueryModel == null)
            {
                throw new ArgumentNullException(nameof(sampleQueryModel), "The sample query model object cannot be null.");
            }

            // Assign a new Id to the new sample query
            sampleQueryModel.Id = Guid.NewGuid();

            // Determine the location in the list of query samples to add the new sample query in 
            int sampleQueryIndex = GetNewSampleQueryIndex(sampleQueriesList, sampleQueryModel);
                        
            // Insert the new sample query into the list of sample queries
            sampleQueriesList.SampleQueries.Insert(sampleQueryIndex, sampleQueryModel);

            // Return the new list of sample queries
            return sampleQueriesList;
        }

        /// <summary>
        /// Removes a sample query model object from a list of sample queries.
        /// </summary>
        /// <param name="sampleQueriesList">The list of sample queries which the sample query model object will be removed from.</param>
        /// <param name="sampleQueryId">The Id value of the sample query model object to be removed.</param>
        /// <returns>The new list of sample queries without the removed sample query model object.</returns>
        public static SampleQueriesList RemoveSampleQuery(SampleQueriesList sampleQueriesList,  Guid sampleQueryId)
        {
            if (sampleQueriesList == null)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of sample queries cannot be null.");
            }
            if (sampleQueryId == null)
            {
                throw new ArgumentNullException(nameof(sampleQueryId), "The sample query id cannot be null.");
            }

            // Find the index of the sample query with the provided id from the list of sample queries
            int sampleQueryIndex = sampleQueriesList.SampleQueries.FindIndex(x => x.Id == sampleQueryId);

            if (sampleQueryIndex < 0)
            {
                // Sample query model object is not in the list of sample queries
                return null;                
            }

            // Delete the sample query model from the list of sample queries at its given index                
            sampleQueriesList.SampleQueries.RemoveAt(sampleQueryIndex);

            // Return the new list of sample queries
            return sampleQueriesList;
        }

        /// <summary>
        /// Determines the zero-based index value in the list of sample queries where a given sample query object should be inserted.
        /// </summary>
        /// <param name="sampleQuery">The sample query object which needs to be inserted.</param>
        /// <returns>The zero-based index where the sample query needs to be inserted into the list of sample queries.</returns>
        private static int GetNewSampleQueryIndex(SampleQueriesList sampleQueriesList, SampleQueryModel sampleQuery)
        {
            // The current sample category will be the starting point of the linked list of categories
            var currentCategory = sampleQuery.Categories.Find(sampleQuery.Category);

            if (sampleQueriesList.SampleQueries.Count == 0 || currentCategory.Previous == null)
            {
                // The list is either empty or the sample query category is the first in the hierarchy of the linked list of categories
                return 0;
            }

            /* 
             Given the starting position of the sample query's category in the linked list of categories,
             search for a matching category value from the list of sample queries.
             Repeat this for all the categories higher up the hierarchy of categories in the linked list. 
             If a match is found, then the sample query should be inserted below the index value
             of the matched category; else, the current category is the top-most ranked category 
             in the list of sample queries and the sample query should be added to the top of the list. 
            */
            while (currentCategory != null)
            {
                foreach (var sampleQueryItem in sampleQueriesList.SampleQueries)
                {
                    if (sampleQueryItem.Category.Contains(currentCategory.Value))
                    {
                        // Find the index of the last sample query in the batch of matched category
                        int index = sampleQueriesList.SampleQueries.FindLastIndex(x => x.Category == currentCategory.Value);

                        return ++index; // new sample should be added in the next index position
                    }                    
                }

                // Go up the hierarchy and search again
                currentCategory = currentCategory.Previous;
            }

            /* All categories up the hierarchy have been traversed with no match found; 
             * this is currently the top-most ranked category in the list of sample queries */
            return 0;
        }
    }
}
