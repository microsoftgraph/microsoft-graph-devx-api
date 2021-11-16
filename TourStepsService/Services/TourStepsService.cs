using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TourStepsService.Models;
using Newtonsoft.Json;

namespace TourStepsService.Services
{
    public static class TourStepsService
    {
        /// <summary>
        /// Deserializes a JSON string into a list of <see cref="TourStepsModel"/> objects.
        /// </summary>
        /// <param name="jsonString">The JSON string to be deserialized into a list of <see cref="TourStepsModel"/> objects.</param>       
        /// <returns>The deserialized list of <see cref="TourStepsModel"/> objects.</returns>
        public static TourStepsList DeserializeTourStepsList(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                throw new ArgumentNullException(nameof(jsonString), "The JSON string to be deserialized cannot be null or empty.");
            }
            TourStepsList tourStepsList = JsonConvert.DeserializeObject<TourStepsList>(jsonString);
            return tourStepsList;
        }

        /// <summary>
        /// Serializes an instance of a <see cref="TourStepsList"/> into JSON string.
        /// </summary>
        /// <param name="tourStepsList">The instance of <see cref="TourStepsList"/> to be deserialized.</param>
        /// <returns>The serialized JSON string from an instance of a <see cref="TourStepsList"/>.</returns>
        public static string SerializetourStepsList(TourStepsList tourStepsList)
        {
            if (tourStepsList == null)
            {
                throw new ArgumentNullException(nameof(tourStepsList), "The list of tour steps cannot be null.");
            }

            string tourStepsJson = JsonConvert.SerializeObject(tourStepsList, Formatting.Indented);
            return tourStepsJson;
        }
    }
}
