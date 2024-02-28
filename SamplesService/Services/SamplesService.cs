// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using SamplesService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SamplesService.Services
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
        /// <param name="orderSamples">"Value indicating whether the sample queries are to be ordered or not."</param>
        /// <returns>The deserialized list of <see cref="SampleQueryModel"/> objects.</returns>
        public static SampleQueriesList DeserializeSampleQueriesList(string jsonString, bool orderSamples = false)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return null;
            }

            SampleQueriesList sampleQueriesList = JsonSerializer.Deserialize<SampleQueriesList>(jsonString);

            if (orderSamples)
            {
                return OrderSamplesQueries(sampleQueriesList);
            }

            return sampleQueriesList;
        }

        private static readonly JsonSerializerOptions JsonSerializerSettings = new() { WriteIndented = true };

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

            var sampleQueriesJson = JsonSerializer.Serialize(
                sampleQueriesList,
                JsonSerializerSettings);
            return sampleQueriesJson;
        }

        /// <summary>
        /// Orders the list of sample queries alphabetically.
        /// </summary>
        /// <param name="sampleQueriesList">An instance of <see cref="SampleQueriesList"/> whose list of sample queries need to be ordered.</param>
        /// <returns>An instance of <see cref="SampleQueriesList"/> whose list of sample queries have been ordered alphabetically with 'Getting Started'
        /// as the top-most sample query.</returns>
        private static SampleQueriesList OrderSamplesQueries(SampleQueriesList sampleQueriesList)
        {
            // This currently applies to English supported sample queries only
            List<SampleQueryModel> sortedSampleQueries = sampleQueriesList.SampleQueries
                .Where(s => s.Category != "Getting Started") // skipped, as it should always be the top-most sample query in the list
                .OrderBy(s => s.Category)
                .ToList();

            SampleQueriesList sortedSampleQueriesList = new SampleQueriesList();

            // Add back 'Getting Started' to the top of the list
            sortedSampleQueriesList.SampleQueries.AddRange(sampleQueriesList.SampleQueries.FindAll(s => s.Category == "Getting Started"));

            // Add the rest of the sample queries
            sortedSampleQueriesList.SampleQueries.AddRange(sortedSampleQueries);

            return sortedSampleQueriesList;
        }
    }
}
