using GraphExplorerSamplesService.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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

            string policiesJson = JsonConvert.SerializeObject(policies, Formatting.Indented);
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
        /// Adds to or updates a <see cref="UserClaim"/> in a target <see cref="CategoryPolicy"/> object.
        /// </summary>
        /// <param name="categoryPolicy">The target <see cref="CategoryPolicy"/> object where the <see cref="UserClaim"/> needs to be updated or added into.</param>
        /// <param name="policies">The list of <see cref="CategoryPolicy"/> where the target <see cref="CategoryPolicy"/> object is contained.</param>
        /// <returns>The updated list of <see cref="SampleQueriesPolicies"/> 
        /// with the new <see cref="UserClaim"/> added or updated at the target <see cref="CategoryPolicy"/> object.</returns>
        public static SampleQueriesPolicies ModifyUserClaim(SampleQueriesPolicies policies, CategoryPolicy categoryPolicy)
        {
            if (policies == null || policies.CategoryPolicies.Count == 0)
            {
                throw new ArgumentNullException(nameof(SampleQueriesPolicies), "The list of policies cannot be null or empty.");
            }
            if (categoryPolicy == null)
            {
                throw new ArgumentNullException(nameof(CategoryPolicy), "The category policy cannot be null.");
            }            

            // Search the target category policy from the list of policies
            CategoryPolicy tempCategoryPolicy = policies.CategoryPolicies.Find(x => x.CategoryName == categoryPolicy.CategoryName);

            if (tempCategoryPolicy == null)
            {
                throw new InvalidOperationException($"The specified category policy doesn't exist: {categoryPolicy.CategoryName}");
            }

            // This will be used later to insert the updated category policy back into the list of policies
            int tempCategoryPolicyIndex = policies.CategoryPolicies.FindIndex(x => x == tempCategoryPolicy);
            
            // Fetch the first user claim from the argument supplied
            UserClaim userClaim = categoryPolicy.UserClaims.FirstOrDefault();

            if (userClaim == null)
            {
                throw new ArgumentNullException(nameof(CategoryPolicy), "User claim information missing.");
            }

            // Get the location of the provided user claim from the temp. category policy
            int userClaimIndex = tempCategoryPolicy.UserClaims.FindIndex(x => x.UserPrincipalName == userClaim.UserPrincipalName);

            // Add new user claim request
            if (userClaimIndex < 0)
            {
                // Check first whether we have default user claim values in the temp. category policy

                CategoryPolicy defaultCategoryPolicyTemplate = new CategoryPolicy
                {
                    UserClaims = new List<UserClaim>()
                    {
                        new UserClaim()
                    }
                };

                if (tempCategoryPolicy.UserClaims.First().UserPrincipalName ==
                    defaultCategoryPolicyTemplate.UserClaims.First().UserPrincipalName)
                {
                    /* This is the first claim for this category policy;
                       clear the default user claim and add the new user claim. */
                    tempCategoryPolicy.UserClaims.Clear();
                    tempCategoryPolicy.UserClaims.Add(userClaim);
                }
                else // we already have other unique user claim values in this category policy
                {
                    // Insert the new user claim info. to the end of list of user claims
                    tempCategoryPolicy.UserClaims.Add(userClaim);
                }
            }
            else // Update user claim request
            {
                // Update the current index with new user claim info.
                tempCategoryPolicy.UserClaims.Insert(userClaimIndex, userClaim);

                // Delete the original user claim pushed to the next index
                tempCategoryPolicy.UserClaims.RemoveAt(++userClaimIndex);
            }

            // Update the modified category policy back into list of policies           
            policies.CategoryPolicies.Insert(tempCategoryPolicyIndex, tempCategoryPolicy);

            // Delete the original category policy pushed to the next index
            policies.CategoryPolicies.RemoveAt(++tempCategoryPolicyIndex);

            return policies;
        }

        /// <summary>
        /// Removes a <see cref="UserClaim"/> from a <see cref="CategoryPolicy"/> object.
        /// </summary>
        /// <param name="policies">The list of <see cref="CategoryPolicy"/> where the target <see cref="CategoryPolicy"/> object is contained.</param>
        /// <param name="categoryPolicyName">The target <see cref="CategoryPolicy"/> object where the <see cref="UserClaim"/> needs to be removed from.</param>    
        /// <param name="userPrincipalName">The target User Principal Name whose <see cref="UserClaim"/> needs to be removed from the <see cref="CategoryPolicy"/> object.</param>
        /// <returns>The updated list of <see cref="SampleQueriesPolicies"/> 
        /// with the <see cref="UserClaim"/> of a target User Principal Name removed from the target <see cref="CategoryPolicy"/> object.</returns>
        public static SampleQueriesPolicies RemoveUserClaim(SampleQueriesPolicies policies, string categoryPolicyName,
                                                            string userPrincipalName)
        {
            if (policies == null || policies.CategoryPolicies.Count == 0)
            {
                throw new ArgumentNullException(nameof(SampleQueriesPolicies), "The list of policies cannot be null or empty.");
            }            
            if (string.IsNullOrEmpty(categoryPolicyName))
            {
                throw new ArgumentNullException(nameof(CategoryPolicy), "The category policy name cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(userPrincipalName))
            {
                throw new ArgumentNullException(nameof(CategoryPolicy), "The user prinicpal name cannot be null or empty.");
            }

            // Search the target category policy from the list of policies
            CategoryPolicy categoryPolicy = policies.CategoryPolicies.Find(
                                                x => x.CategoryName.ToLower() == categoryPolicyName.ToLower());

            if (categoryPolicy == null)
            {
                throw new InvalidOperationException($"The specified category policy doesn't exist: {categoryPolicyName}");
            }

            // This will be used later to insert the updated category policy back into the list of policies
            int categoryPolicyIndex = policies.CategoryPolicies.FindIndex(x => x == categoryPolicy);

            // Fetch the user claim
            UserClaim userClaim = categoryPolicy.UserClaims.FirstOrDefault(
                                    x => x.UserPrincipalName.ToLower() == userPrincipalName.ToLower());

            if (userClaim == null)
            {
                throw new InvalidOperationException($"The specified user principal name has no claim in the specified category. " +
                                                    $"UPN: {userPrincipalName}");
            }

            // Get the location of the provided user claim from the category policy
            int userClaimIndex = categoryPolicy.UserClaims.FindIndex(x => x.UserPrincipalName == userClaim.UserPrincipalName);

            // Remove this user claim from the list of user claims
            categoryPolicy.UserClaims.RemoveAt(userClaimIndex);

            // Update the modified category policy back into list of policies           
            policies.CategoryPolicies.Insert(categoryPolicyIndex, categoryPolicy);

            // Delete the original category policy pushed to the next index
            policies.CategoryPolicies.RemoveAt(++categoryPolicyIndex);

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
