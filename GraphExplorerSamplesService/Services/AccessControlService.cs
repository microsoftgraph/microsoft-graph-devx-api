using GraphExplorerSamplesService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GraphExplorerSamplesService.Services
{
    public class AccessControlService
    {
        /// <summary>
        /// Deserializes a JSON string into a list of <see cref="CategoryRights"/> objects.
        /// </summary>
        /// <param name="jsonString">The JSON string to be deserialized into a list of <see cref="SampleQueryModel"/> objects.</param>
        /// <returns>The deserialized list of <see cref="SampleQueryModel"/> objects.</returns>
        public static SampleQueriesAccessRights DeserializeSampleQueriesAccessRights(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                throw new ArgumentNullException(nameof(jsonString), "The JSON string to be deserialized cannot be null.");
            }

            SampleQueriesAccessRights accessRights = JsonConvert.DeserializeObject<SampleQueriesAccessRights>(jsonString);
            return accessRights;
        }

        /// <summary>
        /// Serializes an instance of a <see cref="SampleQueriesAccessRights"/> into JSON string.
        /// </summary>
        /// <param name="sampleQueriesAccessRights">The instance of <see cref="SampleQueriesAccessRights"/> to be deserialized.</param>
        /// <returns>The serialized JSON string from an instance of a <see cref="SampleQueriesAccessRights"/>.</returns>
        public static string SerializeSampleQueriesAccessRights(SampleQueriesAccessRights sampleQueriesAccessRights)
        {
            if (sampleQueriesAccessRights == null || sampleQueriesAccessRights.AccessRights.Count == 0)
            {
                throw new ArgumentNullException(nameof(sampleQueriesAccessRights), "The list of access rights cannot be null or empty.");
            }

            string accessRightsJson = JsonConvert.SerializeObject(sampleQueriesAccessRights);
            return accessRightsJson;
        }

        /// <summary>
        /// Creates a default template of access rights for the sample query categories.
        /// </summary>
        /// <returns>An instance of <see cref="SampleQueriesAccessRights"/> containing a list of <see cref="CategoryRights"/>.</returns>
        public SampleQueriesAccessRights CreateDefaultAccessRightsTemplate()
        {
            if (SampleQueriesCategories.CategoriesLinkedList.Count == 0)
            {
                throw new InvalidOperationException("Cannot create a default access rights template; the list of categories is empty.");
            }

            // List to hold the category access rights
            List<CategoryRights> categoryRights = new List<CategoryRights>();

            // Create the default access rights template for each category in the list
            foreach(string category in SampleQueriesCategories.CategoriesLinkedList)
            {
                CategoryRights categoryRight = new CategoryRights()
                {
                    CategoryName = category,
                    UserRights = new List<UserRight>()
                    {
                        new UserRight()
                        {
                            UserPermissions = new List<Permission>() { new Permission() }
                        }
                    }
                };

                categoryRights.Add(categoryRight);
            }

            SampleQueriesAccessRights accessRights = new SampleQueriesAccessRights()
            {
                AccessRights = categoryRights
            };

            return accessRights;
        }
    }
}
