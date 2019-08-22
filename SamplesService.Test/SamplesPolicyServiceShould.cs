using GraphExplorerSamplesService.Models;
using GraphExplorerSamplesService.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace SamplesService.Test
{
    public class SamplesPolicyServiceShould
    {
        #region Serialize Sample Queries Policies Tests

        [Fact]
        public void SerializeSampleQueriesPolicies()
        {
            /* Arrange */

            // Create list to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            /* Create category policies */

            CategoryPolicy categoryPolicy1 = new CategoryPolicy()
            {
                CategoryName = "Getting Started",
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

            CategoryPolicy categoryPolicy2 = new CategoryPolicy()
            {
                CategoryName = "Users",
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

            categoryPolicies.Add(categoryPolicy1);
            categoryPolicies.Add(categoryPolicy2);

            SampleQueriesPolicies policies = new SampleQueriesPolicies()
            {
                CategoryPolicies = categoryPolicies
            };

            // Act
            string policiesJson = SamplesPolicyService.SerializeSampleQueriesPolicies(policies);

            /* Assert */

            Assert.NotEmpty(policiesJson);
            Assert.Contains("Getting Started", policiesJson);
            Assert.Contains("Users", policiesJson);
        }

        [Fact]
        public void SerializeSampleQueriesPoliciesIfPoliciesParameterIsEmptyCollection()
        {
            // Arrange
            SampleQueriesPolicies emptyPolicy = new SampleQueriesPolicies();

            // Act
            string policyJson = SamplesPolicyService.SerializeSampleQueriesPolicies(emptyPolicy);

            // Assert
            Assert.NotEmpty(policyJson);
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfSerializeSampleQueriesPoliciesIfPoliciesParameterIsNull()
        {
            // Arrange
            SampleQueriesPolicies nullPolicy = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                SamplesPolicyService.SerializeSampleQueriesPolicies(nullPolicy));
        }

        #endregion

        #region Deserialize Sample Queries Policies Tests
        [Fact]
        public void DeserializeSampleQueriesPolicies()
        {
            // Arrange
            string policiesJson = @"{
                    ""CategoryPolicies"":[
                    {
                        ""categoryName"":""Getting Started"",
	                    ""userClaims"":[
                            {
	                            ""userName"":""John Doe"",
	                            ""userPrincipalName"":""john.doe@microsoft.com"",
	                            ""userPermissions"":[
                                    {
	                                    ""name"":""POST"",
	                                    ""value"":true
                                    },
                                    {
	                                    ""name"":""PUT"",
	                                    ""value"":true
                                    },
                                    {
	                                    ""name"":""DELETE"",
	                                    ""value"":true
                                    }]
                            }]
                   },
	               {
		                ""categoryName"":""Users"",
		                ""userClaims"":[
                            {
		                        ""userName"":""Jane Doe"",
		                        ""userPrincipalName"":""jane.doe@microsoft.com"",
		                        ""userPermissions"":[
                                    {
		                                ""name"":""POST"",
		                                ""value"":true
	                                },
	                                {
		                                ""name"":""PUT"",
		                                ""value"":true
	                                },
	                                {
		                                ""name"":""DELETE"",
		                                ""value"":true
	                                }]
                            }]
	                }]}";

            // Act
            SampleQueriesPolicies policies = SamplesPolicyService.DeserializeSampleQueriesPolicies(policiesJson);

            // Assert
            Assert.Collection(policies.CategoryPolicies,
                item =>
                {
                    Assert.Equal("Getting Started", item.CategoryName);
                    Assert.Equal("John Doe", item.UserClaims[0].UserName);
                    Assert.Equal("john.doe@microsoft.com", item.UserClaims[0].UserPrincipalName);
                    Assert.Equal("POST", item.UserClaims[0].UserPermissions[0].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[0].Value);
                    Assert.Equal("PUT", item.UserClaims[0].UserPermissions[1].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[1].Value);
                    Assert.Equal("DELETE", item.UserClaims[0].UserPermissions[2].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[2].Value);
                },
                item =>
                {
                    Assert.Equal("Users", item.CategoryName);
                    Assert.Equal("Jane Doe", item.UserClaims[0].UserName);
                    Assert.Equal("jane.doe@microsoft.com", item.UserClaims[0].UserPrincipalName);
                    Assert.Equal("POST", item.UserClaims[0].UserPermissions[0].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[0].Value);
                    Assert.Equal("PUT", item.UserClaims[0].UserPermissions[1].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[1].Value);
                    Assert.Equal("DELETE", item.UserClaims[0].UserPermissions[2].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[2].Value);
                });
        }

        [Fact]
        public void ThrowJsonReaderExceptionIfDeserializeSampleQueriesPoliciesJsonStringParameterIsInvalidJsonString()
        {
            /* Arrange */

            // JSON string missing a closing brace '}' and comma separator ',' after the first object definition
            string invalidPoliciesJson = @"{
                    ""CategoryPolicies"":[
                    {
                        ""categoryName"":""Getting Started"",
	                    ""userClaims"":[
                            {
	                            ""userName"":""John Doe"",
	                            ""userPrincipalName"":""john.doe@microsoft.com"",
	                            ""userPermissions"":[
                                    {
	                                    ""name"":""POST"",
	                                    ""value"":true
                                    },
                                    {
	                                    ""name"":""PUT"",
	                                    ""value"":true
                                    },
                                    {
	                                    ""name"":""DELETE"",
	                                    ""value"":true
                                    }]
                            }]
                   
	               {
		                ""categoryName"":""Users"",
		                ""userClaims"":[
                            {
		                        ""userName"":""Jane Doe"",
		                        ""userPrincipalName"":""jane.doe@microsoft.com"",
		                        ""userPermissions"":[
                                    {
		                                ""name"":""POST"",
		                                ""value"":true
	                                },
	                                {
		                                ""name"":""PUT"",
		                                ""value"":true
	                                },
	                                {
		                                ""name"":""DELETE"",
		                                ""value"":true
	                                }]
                            }]
	                }]}";

            // Act and Assert
            Assert.Throws<JsonReaderException>(() => SamplesPolicyService.DeserializeSampleQueriesPolicies(invalidPoliciesJson));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfDeserializeSampleQueriesPoliciesJsonStringParameterIsNullOrEmpty()
        {
            /* Arrange */

            string nullJsonArg = null;
            string emptyJsonArg = "";

            /* Act and Assert */
            
            // Null JSON string arg.
            Assert.Throws<ArgumentNullException>(() =>
                SamplesPolicyService.DeserializeSampleQueriesPolicies(nullJsonArg));

            // Empty JSON string arg.
            Assert.Throws<ArgumentNullException>(() => 
                SamplesPolicyService.DeserializeSampleQueriesPolicies(emptyJsonArg));
        }

        #endregion

        #region Create Sample Queries Policies Template Tests

        [Fact]
        public void CreateDefaultPoliciesTemplate()
        {
            // Arrange and Act
            SampleQueriesPolicies policiesTemplate = SamplesPolicyService.CreateDefaultPoliciesTemplate();

            // Assert
            Assert.NotEmpty(policiesTemplate.CategoryPolicies);
        }

        #endregion

        #region User Policy-Claim Authorization Tests

        [Fact]
        public void AuthorizeUserBasedOnPolicyAndClaims()
        {
            /* Arrange */

            // Create list to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            /* Create the category policies */

            CategoryPolicy categoryPolicy = new CategoryPolicy()
            {
                CategoryName = "Getting Started",
                UserClaims = new List<UserClaim>()
                {
                    new UserClaim()
                    {
                        UserName = "John Doe",
                        UserPrincipalName = "john.doe@microsoft.com",
                        UserPermissions = new List<Permission>()
                        {
                            new Permission()
                            {
                                Name = HttpMethods.Post,
                                Value = true // Authorized to Create
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Put,
                                Value = true
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Delete,
                                Value = false // Unauthorized to Delete
                            }
                        }
                    }
                }
            };

            categoryPolicies.Add(categoryPolicy);

            SampleQueriesPolicies policies = new SampleQueriesPolicies()
            {
                CategoryPolicies = categoryPolicies
            };

            /* Act */

            bool isAuthorizedToCreate =
                SamplesPolicyService.IsUserAuthorized(policies, "john.doe@microsoft.com", "Getting Started", HttpMethods.Post);

            bool isAuthorizedToDelete =
                SamplesPolicyService.IsUserAuthorized(policies, "john.doe@microsoft.com", "Getting Started", HttpMethods.Delete);

            /* Assert */

            Assert.True(isAuthorizedToCreate);
            Assert.False(isAuthorizedToDelete);
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfIsUserAuthorizedPoliciesParameterIsNullOrEmptyCollection()
        {
            /* Arrange */
                        
            SampleQueriesPolicies nullPolicies = null;
            SampleQueriesPolicies emptyPolicies = new SampleQueriesPolicies();

            string userPrincipalName = "john.doe@microsoft.com";
            string categoryName = "Getting Started";
            string httpAction = HttpMethods.Post;

            /* Act and Assert */

            // Null policies arg.
            Assert.Throws<ArgumentNullException>(() =>
                SamplesPolicyService.IsUserAuthorized(nullPolicies, userPrincipalName, categoryName, httpAction));

            // Empty policies arg.
            Assert.Throws<ArgumentNullException>(() =>
               SamplesPolicyService.IsUserAuthorized(emptyPolicies, userPrincipalName, categoryName, httpAction));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfIsUserAuthorizedUserPrincipalNameParameterIsNullOrEmpty()
        {
            /* Arrange */

            // Create list to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            /* Create category policy */

            CategoryPolicy categoryPolicy = new CategoryPolicy()
            {
                CategoryName = "Getting Started",
                UserClaims = new List<UserClaim>()
                {
                    new UserClaim()
                    {
                        UserName = "John Doe",
                        UserPrincipalName = "john.doe@microsoft.com",
                        UserPermissions = new List<Permission>()
                        {
                            new Permission()
                            {
                                Name = HttpMethods.Post,
                                Value = true
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Put,
                                Value = true
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Delete,
                                Value = true
                            }
                        }
                    }
                }
            };

            categoryPolicies.Add(categoryPolicy);

            SampleQueriesPolicies policies = new SampleQueriesPolicies()
            {
                CategoryPolicies = categoryPolicies
            };

            string nullUserPrincipalName = null;
            string emptyUserPrincipalName = "";

            string categoryName = "Getting Started";
            string httpAction = HttpMethods.Post;

            /* Act and Assert */

            // Null user principal name arg.
            Assert.Throws<ArgumentNullException>(() =>
                SamplesPolicyService.IsUserAuthorized(policies, nullUserPrincipalName, categoryName, httpAction));

            // Empty user principal name arg.
            Assert.Throws<ArgumentNullException>(() =>
               SamplesPolicyService.IsUserAuthorized(policies, emptyUserPrincipalName, categoryName, httpAction));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfIsUserAuthorizedCategoryNameParameterIsNullOrEmpty()
        {
            /* Arrange */

            // Create list to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            /* Create category policies */

            CategoryPolicy categoryPolicy = new CategoryPolicy()
            {
                CategoryName = "Getting Started",
                UserClaims = new List<UserClaim>()
                {
                    new UserClaim()
                    {
                        UserName = "John Doe",
                        UserPrincipalName = "john.doe@microsoft.com",
                        UserPermissions = new List<Permission>()
                        {
                            new Permission()
                            {
                                Name = HttpMethods.Post,
                                Value = true
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Put,
                                Value = true
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Delete,
                                Value = true
                            }
                        }
                    }
                }
            };

            categoryPolicies.Add(categoryPolicy);

            SampleQueriesPolicies policies = new SampleQueriesPolicies()
            {
                CategoryPolicies = categoryPolicies
            };

            string nullCategoryName = null;
            string emptyCategoryName = "";

            string userPrincipalName = "john.doe@microsoft.com";            
            string httpAction = HttpMethods.Post;

            /* Act and Assert */

            // Null category name arg.
            Assert.Throws<ArgumentNullException>(() =>
                SamplesPolicyService.IsUserAuthorized(policies, userPrincipalName, nullCategoryName, httpAction));

            // Empty category name arg.
            Assert.Throws<ArgumentNullException>(() =>
               SamplesPolicyService.IsUserAuthorized(policies, userPrincipalName, emptyCategoryName, httpAction));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfIsUserAuthorizedHttpMethodNameParameterIsNullOrEmpty()
        {
            /* Arrange */

            // Create list to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            /* Create category policies */

            CategoryPolicy categoryPolicy = new CategoryPolicy()
            {
                CategoryName = "Getting Started",
                UserClaims = new List<UserClaim>()
                {
                    new UserClaim()
                    {
                        UserName = "John Doe",
                        UserPrincipalName = "john.doe@microsoft.com",
                        UserPermissions = new List<Permission>()
                        {
                            new Permission()
                            {
                                Name = HttpMethods.Post,
                                Value = true
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Put,
                                Value = true
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Delete,
                                Value = true
                            }
                        }
                    }
                }
            };

            categoryPolicies.Add(categoryPolicy);

            SampleQueriesPolicies policies = new SampleQueriesPolicies()
            {
                CategoryPolicies = categoryPolicies
            };

            string nullHttpAction = null;
            string emptyHttpAction = "";

            string categoryName = "Getting Started";
            string userPrincipalName = "john.doe@microsoft.com";
            

            /* Act and Assert */

            // Null http action name arg.
            Assert.Throws<ArgumentNullException>(() =>
                SamplesPolicyService.IsUserAuthorized(policies, userPrincipalName, categoryName, nullHttpAction));

            // Empty http action name arg.
            Assert.Throws<ArgumentNullException>(() =>
               SamplesPolicyService.IsUserAuthorized(policies, userPrincipalName, categoryName, emptyHttpAction));
        }

        #endregion
    }
}
