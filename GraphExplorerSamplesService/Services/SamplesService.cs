using GraphExplorerSamplesService.Models;
using Newtonsoft.Json;
using System;

namespace GraphExplorerSamplesService.Services
{
    /// <summary>
    /// Provides utility functions for manipulating <see cref="SampleQueriesList"/> and <see cref="SampleQueryModel"/> objects.
    /// </summary>
    public static class SamplesService
    {
        /// <summary>
        /// Deserializes a JSON string into a list of <see cref="SampleQueryModel"/> objects.
        /// </summary>
        /// <param name="jsonString">The JSON string to be deserialized into a list of <see cref="SampleQueryModel"/> objects.</param>
        /// <returns>The deserialized list of <see cref="SampleQueryModel"/> objects.</returns>
        public static SampleQueriesList DeserializeSampleQueriesList(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                throw new ArgumentNullException(nameof(jsonString), "The JSON string to be deserialized cannot be null.");
            }

            SampleQueriesList sampleQueriesList = JsonConvert.DeserializeObject<SampleQueriesList>(jsonString);
            return sampleQueriesList;
        }

        /// <summary>
        /// Serializes an instance of a <see cref="SampleQueriesList"/> into JSON string.
        /// </summary>
        /// <param name="sampleQueriesList">The instance of <see cref="SampleQueriesList"/> to be deserialized.</param>
        /// <returns>The serialized JSON string from an instance of a <see cref="SampleQueriesList"/>.</returns>
        public static string SerializeSampleQueriesList(SampleQueriesList sampleQueriesList)
        {
            if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of sample queries cannot be null or empty.");
            }

            string sampleQueriesJson = JsonConvert.SerializeObject(sampleQueriesList, Formatting.Indented);
            return sampleQueriesJson;
        }

        /// <summary>
        /// Updates a <see cref="SampleQueryModel"/> object within an instance of a <see cref="SampleQueriesList"/>. 
        /// The new <see cref="SampleQueryModel"/> object overwrites the existing one entirely except for its Id property.
        /// </summary>
        /// <param name="sampleQueriesList">The list of sample queries which contains the <see cref="SampleQueryModel"/> model object be updated.</param>
        /// <param name="sampleQueryModel">The <see cref="SampleQueryModel"/> object to update.</param>
        /// <param name="sampleQueryId">The Id value of the <see cref="SampleQueryModel"/> object to be updated.</param>
        /// <returns>The updated list of <see cref="SampleQueriesList"/>.</returns>
        public static SampleQueriesList UpdateSampleQueriesList(SampleQueriesList sampleQueriesList, SampleQueryModel sampleQueryModel, Guid sampleQueryId)
        {
            if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of sample queries cannot be null or empty.");
            }
            if (sampleQueryModel == null)
            {
                throw new ArgumentNullException(nameof(sampleQueryModel), "The sample query model object cannot be null.");
            }
            if (sampleQueryId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(sampleQueryId), "The sample query id cannot be empty.");
            }

            // Check if the sample query model exists in the list of sample queries using its Id
            int sampleQueryIndex = sampleQueriesList.SampleQueries.FindIndex(x => x.Id == sampleQueryId);

            if (sampleQueryIndex < 0)
            {
                return null; // sample query not in the list of sample queries             
            }

            // Check if Id property for the sample query model object is empty
            if (sampleQueryModel.Id == Guid.Empty)
            {
                // Assign the Id before inserting this sample query object in the list
                sampleQueryModel.Id = sampleQueryId;
            }

            // Insert the new sample query object into the list of sample queries at the index of the original sample query object
            sampleQueriesList.SampleQueries.Insert(sampleQueryIndex, sampleQueryModel);

            // Original sample query object pushed to the next index; increment index then remove it
            sampleQueriesList.SampleQueries.RemoveAt(++sampleQueryIndex);

            // Return the new list of sample queries
            return sampleQueriesList;
        }


        /// <summary>
        /// Adds a <see cref="SampleQueryModel"/> object into an instance of a <see cref="SampleQueriesList"/>.
        /// </summary>
        /// <param name="sampleQueriesList">The instance of a <see cref="SampleQueriesList"/> which the <see cref="SampleQueryModel"/> 
        /// object will be added into.</param>
        /// <param name="sampleQueryModel">The <see cref="SampleQueryModel"/> object to be added.</param>
        /// <returns>The instance of a <see cref="SampleQueriesList"/> with the newly added <see cref="SampleQueryModel"/> object.</returns>
        public static SampleQueriesList AddToSampleQueriesList(SampleQueriesList sampleQueriesList, SampleQueryModel sampleQueryModel)
        {
            if (sampleQueriesList == null)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of sample queries cannot be null.");
            }
            if (sampleQueryModel == null)
            {
                throw new ArgumentNullException(nameof(sampleQueryModel), "The sample query model object cannot be null.");
            }                       

            // Determine the location in the list of query samples to add the new sample query in 
            int sampleQueryIndex = GetNewSampleQueryIndex(sampleQueriesList, sampleQueryModel);

            // Insert the new sample query into the list of sample queries
            sampleQueriesList.SampleQueries.Insert(sampleQueryIndex, sampleQueryModel);

            // Return the new list of sample queries
            return sampleQueriesList;
        }

        /// <summary>
        /// Removes a <see cref="SampleQueryModel"/> object from an instance of a <see cref="SampleQueriesList"/>.
        /// </summary>
        /// <param name="sampleQueriesList">The instance of a <see cref="SampleQueriesList"/> which the <see cref="SampleQueryModel"/> 
        /// object will be removed from.</param>
        /// <param name="sampleQueryId">The Id value of the <see cref="SampleQueryModel"/> object to be removed.</param>
        /// <returns>The new instance of a <see cref="SampleQueriesList"/> without the removed <see cref="SampleQueryModel"/> object.</returns>
        public static SampleQueriesList RemoveSampleQuery(SampleQueriesList sampleQueriesList, Guid sampleQueryId)
        {
            if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of sample queries cannot be null or empty.");
            }
            if (sampleQueryId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(sampleQueryId), "The sample query id cannot be null.");
            }

            // Find the index of the sample query with the provided id from the list of sample queries
            int sampleQueryIndex = sampleQueriesList.SampleQueries.FindIndex(x => x.Id == sampleQueryId);

            if (sampleQueryIndex < 0)
            {
                return null; // sample query not in the list of sample queries               
            }

            // Delete the sample query model from the list of sample queries at its given index                
            sampleQueriesList.SampleQueries.RemoveAt(sampleQueryIndex);

            // Return the new list of sample queries
            return sampleQueriesList;
        }

        /// <summary>
        /// Determines the zero-based index value in the instance of a <see cref="SampleQueriesList"/> where a given <see cref="SampleQueryModel"/> 
        /// object should be inserted.
        /// </summary>
        /// <param name="sampleQuery">The <see cref="SampleQueryModel"/> object which needs to be inserted.</param>
        /// <returns>The zero-based index where the <see cref="SampleQueryModel"/> object needs to be inserted into in an instance of a
        /// <see cref="SampleQueriesList"/>.</returns>
        private static int GetNewSampleQueryIndex(SampleQueriesList sampleQueriesList, SampleQueryModel sampleQuery)
        {
            // The current sample category will be the starting point of the linked list of categories
            var currentCategory = SampleQueriesCategories.CategoriesLinkedList.Find(sampleQuery.Category);

            if (sampleQueriesList.SampleQueries.Count == 0 || currentCategory == null || currentCategory.Previous == null)
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
