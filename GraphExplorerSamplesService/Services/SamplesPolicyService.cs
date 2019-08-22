using GraphExplorerSamplesService.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GraphExplorerSamplesService.Services
{
    /// <summary>
    /// Provides utility functions for manipulating sample queries policies and validating sample queries claims. 
    /// </summary>
    public class SamplesPolicyService
    {
        /// <summary>
        /// Deserializes a JSON string into a list of <see cref="CategoryPolicy"/> objects.
        /// </summary>
        /// <param name="jsonString">The JSON string to be deserialized into a list of <see cref="SampleQueryModel"/> objects.</param>
        /// <returns>The deserialized list of <see cref="SampleQueryModel"/> objects.</returns>
        public static SampleQueriesPolicies DeserializeSampleQueriesPolicies(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                throw new ArgumentNullException(nameof(jsonString), "The JSON string to be deserialized cannot be null or empty.");
            }

            SampleQueriesPolicies policies = JsonConvert.DeserializeObject<SampleQueriesPolicies>(jsonString);
            return policies;
        }

        /// <summary>
        /// Serializes an instance of a <see cref="SampleQueriesPolicies"/> into JSON string.
        /// </summary>
        /// <param name="policies">The instance of <see cref="SampleQueriesPolicies"/> to be deserialized.</param>
        /// <returns>The serialized JSON string from an instance of a <see cref="SampleQueriesPolicies"/>.</returns>
        public static string SerializeSampleQueriesPolicies(SampleQueriesPolicies policies)
        {
            if (policies == null)
            {
                throw new ArgumentNullException(nameof(policies), "The list of policies cannot be null.");
            }

            string policiesJson = JsonConvert.SerializeObject(policies);
            return policiesJson;
        }

        /// <summary>
        /// Creates a default template of policies for the sample query categories.
        /// </summary>
        /// <returns>An instance of <see cref="SampleQueriesPolicies"/> containing a list of <see cref="CategoryPolicy"/>.</returns>
        public static SampleQueriesPolicies CreateDefaultPoliciesTemplate()
        {
            if (SampleQueriesCategories.CategoriesLinkedList.Count == 0)
            {
                throw new InvalidOperationException("Cannot create a default policy template; the list of categories is empty.");
            }

            // List to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            // Create the default policy template for each category in the list
            foreach(string category in SampleQueriesCategories.CategoriesLinkedList)
            {
                CategoryPolicy categoryPolicy = new CategoryPolicy()
                {
                    CategoryName = category,
                    UserClaims = new List<UserClaim>()
                    {
                        new UserClaim()
                        {
                            UserPermissions = new List<Permission>()
                            {
                                new Permission() { Name = HttpMethods.Post },
                                new Permission() { Name = HttpMethods.Put },
                                new Permission() { Name = HttpMethods.Delete }
                            }
                        }
                    }
                };

                categoryPolicies.Add(categoryPolicy);
            }

            SampleQueriesPolicies policies = new SampleQueriesPolicies()
            {
                CategoryPolicies = categoryPolicies
            };

            return policies;
        }

        /// <summary>
        /// Checks whether a user is authorized to access a sample query with a given action.
        /// </summary>
        /// <param name="policies">The list of category of policies.</param>
        /// <param name="userPrincipalName">The User Principal Name of the user who is seeking authorization confirmation.</param>
        /// <param name="categoryName">The category which the authorization confirmation is been requested for.</param>
        /// <param name="httpMethodName">The http action to be performed on the category.</param>
        /// <returns>True if user is authorized or false if user is not authorized.</returns>
        public static bool IsUserAuthorized(SampleQueriesPolicies policies, string userPrincipalName, 
                                            string categoryName, string httpMethodName)
        {
            if (policies == null || policies.CategoryPolicies.Count == 0)
            {
                throw new ArgumentNullException(nameof(policies), "The list of policies cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(userPrincipalName))
            {
                throw new ArgumentNullException(nameof(userPrincipalName), "The user principal name cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(categoryName))
            {
                throw new ArgumentNullException(nameof(categoryName), "The category name cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(httpMethodName))
            {
                throw new ArgumentNullException(nameof(httpMethodName), "The http method name cannot be null or empty.");
            }

            bool isAuthorized =
                policies.CategoryPolicies.Exists(x => x.CategoryName.ToLower() == categoryName.ToLower() && 
                x.UserClaims.Exists(y => y.UserPrincipalName.ToLower() == userPrincipalName.ToLower() && 
                y.UserPermissions.Exists(z => z.Name.ToLower() == httpMethodName.ToLower() && z.Value == true)));

            return isAuthorized;
        }
    }
}
