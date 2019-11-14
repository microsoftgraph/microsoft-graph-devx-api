using GraphExplorerSamplesService.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

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
                throw new ArgumentNullException(nameof(jsonString), "The JSON string to be deserialized cannot be null or empty.");
            }

            SampleQueriesList sampleQueriesList = JsonConvert.DeserializeObject<SampleQueriesList>(jsonString);
            return SanitizeSampleQueries(sampleQueriesList);
        }

        /// <summary>
        /// Serializes an instance of a <see cref="SampleQueriesList"/> into JSON string.
        /// </summary>
        /// <param name="sampleQueriesList">The instance of <see cref="SampleQueriesList"/> to be deserialized.</param>
        /// <returns>The serialized JSON string from an instance of a <see cref="SampleQueriesList"/>.</returns>
        public static string SerializeSampleQueriesList(SampleQueriesList sampleQueriesList)
        {
            if (sampleQueriesList == null)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of sample queries cannot be null.");
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

            // Get the index of the sample query model in the list of sample queries
            int sampleQueryIndex = sampleQueriesList.SampleQueries.FindIndex(x => x.Id == sampleQueryId);

            // Check to ascertain the sample query is existent
            if (sampleQueryIndex < 0)
            {
                throw new InvalidOperationException($"No sample query found with id: {sampleQueryId}");
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
        /// <param name="sampleQueryId">The Id value of the <see cref="SampleQueryModel"/> object to be deleted.</param>
        /// <returns>The new instance of a <see cref="SampleQueriesList"/> without the removed <see cref="SampleQueryModel"/> object.</returns>
        public static SampleQueriesList RemoveSampleQuery(SampleQueriesList sampleQueriesList, Guid sampleQueryId)
        {
            if (sampleQueriesList == null || sampleQueriesList.SampleQueries.Count == 0)
            {
                throw new ArgumentNullException(nameof(sampleQueriesList), "The list of sample queries cannot be null or empty.");
            }
            if (sampleQueryId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(sampleQueryId), "The sample query id cannot be empty.");
            }

            // Get the index of the sample query model in the list of sample queries
            int sampleQueryIndex = sampleQueriesList.SampleQueries.FindIndex(x => x.Id == sampleQueryId);

            // Check to ascertain the sample query is existent
            if (sampleQueryIndex < 0)
            {
                throw new InvalidOperationException($"No sample query found with id: {sampleQueryId}");
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
        /// <param name="sampleQueriesList">The instance of a <see cref="SampleQueriesList"/> where the given <see cref="SampleQueryModel"/> 
        /// object should be inserted into.
        /// <param name="sampleQuery">The <see cref="SampleQueryModel"/> object which needs to be inserted.</param>
        /// <returns>The zero-based index where the <see cref="SampleQueryModel"/> object needs to be inserted into in an instance of a
        /// <see cref="SampleQueriesList"/>.</returns>
        private static int GetNewSampleQueryIndex(SampleQueriesList sampleQueriesList, SampleQueryModel sampleQuery)
        {
            // The current sample category will be the starting point of the list of categories
            string currentCategory = SampleQueriesCategories.CategoriesList.Find(x => x == sampleQuery.Category);

            if (sampleQueriesList.SampleQueries.Count == 0)
            {                
                return 0; // the list is empty; this will be the first sample query
            }

            // Search for this category from the list of sample queries
            foreach (SampleQueryModel sampleQueryItem in sampleQueriesList.SampleQueries)
            {
                if (sampleQueryItem.Category.Contains(currentCategory))
                {
                    // Find the index of the last sample query in the batch of matched category
                    int index = sampleQueriesList.SampleQueries.FindLastIndex(x => x.Category == currentCategory);

                    return ++index; // new sample should be added in the next index position
                }
            }

            /* All sample queries categories in the list have been traversed with no match found; 
             * Add it to the top of the list */
            return 0;
        }

        /// <summary>
        /// Orders the list of sample queries alphabetically based on their category names with 'Getting Started' as the top-most sample query.
        /// </summary>
        /// <param name="sampleQueriesList">An instance of <see cref="SampleQueriesList"/> whose list of sample queries need to be ordered.</param>
        /// <returns>An instance of <see cref="SampleQueriesList"/> whose list of sample queries have been ordered alphabetically with 'Getting Started' 
        /// as the top-most sample query.</returns>
        private static SampleQueriesList OrderSamplesQueries(SampleQueriesList sampleQueriesList)
        {
            List<SampleQueryModel> sortedSampleQueries = sampleQueriesList.SampleQueries
                .OrderBy(s => s.Category)
                .Where(s => s.Category != "Getting Started") // skipped, as it should always be the top-most sample query in the list
                .ToList();

            SampleQueriesList sortedSampleQueriesList = new SampleQueriesList();

            // Add back 'Getting Started' to the top of the list
            sortedSampleQueriesList.SampleQueries.AddRange(sampleQueriesList.SampleQueries.FindAll(s => s.Category == "Getting Started"));

            // Add the rest of the sample queries
            sortedSampleQueriesList.SampleQueries.AddRange(sortedSampleQueries);

            return sortedSampleQueriesList;
        }

        /// <summary>
        /// Replaces the default password with a random Guid value in the postBody template of the Users sample query.
        /// </summary>
        /// <param name="sampleQueriesList">An instance of <see cref="SampleQueriesList"/> with the Users sample query whose password in the postBody template 
        /// needs to be replaced with a random Guid value.</param>
        private static void GenerateUniqueUserPassword(SampleQueriesList sampleQueriesList)
        {
            SampleQueryModel sampleQuery = sampleQueriesList.SampleQueries.Find(s => s.Category == "Users" && s.Method == SampleQueryModel.HttpMethods.POST);

            if (sampleQuery != null && !string.IsNullOrEmpty(sampleQuery.PostBody))
            {
                try
                {
                    JObject postBodyObject = JObject.Parse(sampleQuery.PostBody);

                    postBodyObject["passwordProfile"]["password"] = Guid.NewGuid();

                    sampleQuery.PostBody = postBodyObject.ToString();
                }
                catch
                {
                    // no action required
                }    
            }
        }

        /// <summary>
        /// Preprocesses sample queries with the established business rules.
        /// </summary>
        /// <param name="sampleQueriesList">An instance of <see cref="SampleQueriesList"/> that requires preprocessing.</param>
        /// <returns>An instance of <see cref="SampleQueriesList"/> that has been preprocessed with the established business rules.</returns>
        private static SampleQueriesList SanitizeSampleQueries(SampleQueriesList sampleQueriesList)
        {
            SampleQueriesList orderedSampleQueries = OrderSamplesQueries(sampleQueriesList);

            GenerateUniqueUserPassword(orderedSampleQueries);

            return orderedSampleQueries;
        }
    }
}