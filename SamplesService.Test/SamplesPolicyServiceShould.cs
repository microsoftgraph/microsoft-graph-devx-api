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

            /* Assert that all items in the collection follow the specified
             * template structure. */

            Assert.All(policiesTemplate.CategoryPolicies,
                policy =>
                {
                    Assert.NotNull(policy.CategoryName);
                    Assert.Collection(policy.UserClaims,
                        claim =>
                        {
                            Assert.Equal("Unspecified", claim.UserName);
                            Assert.Equal("unspecified@xyz.com", claim.UserPrincipalName);
                            Assert.Collection(claim.UserPermissions,
                                permission =>
                                {
                                    Assert.Equal(HttpMethods.Post, permission.Name);
                                    Assert.False(permission.Value);                                                                        
                                },
                                permission =>
                                {
                                    Assert.Equal(HttpMethods.Put, permission.Name);
                                    Assert.False(permission.Value);
                                },
                                permission =>
                                {
                                    Assert.Equal(HttpMethods.Delete, permission.Name);
                                    Assert.False(permission.Value);
                                });
                        });
                });
        }

        #endregion

        #region Modify User Claim Tests

        [Fact]
        public void ThrowArgumentNullExceptionIfModifyUserClaimCategoryPolicyParameterIsNull()
        {
            /* Arrange */
                        
            CategoryPolicy targetCategoryPolicy = null;

            // Create list to hold category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            /* Create category policies */

            CategoryPolicy categoryPolicy = new CategoryPolicy()
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

            categoryPolicies.Add(categoryPolicy);

            SampleQueriesPolicies policies = new SampleQueriesPolicies()
            {
                CategoryPolicies = categoryPolicies
            };

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => SamplesPolicyService.ModifyUserClaim(policies, targetCategoryPolicy));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfModifyUserClaimPoliciesParameterIsNull()
        {
            /* Arrange */

            SampleQueriesPolicies nullPolicies = null;
            SampleQueriesPolicies emptyPolicies = new SampleQueriesPolicies();

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

            /* Act and Assert */

            // Null policies
            Assert.Throws<ArgumentNullException>(() => SamplesPolicyService.ModifyUserClaim(nullPolicies, categoryPolicy));

            // Empty policies
            Assert.Throws<ArgumentNullException>(() => SamplesPolicyService.ModifyUserClaim(emptyPolicies, categoryPolicy));
        }

        [Fact]
        public void ThrowInvalidOperationExceptionIfModifyUserClaimCategoryPolicyParameterNotExistsInPoliciesParameter()
        {
            /* Arrange */

            // Create the target category policy
            CategoryPolicy targetCategoryPolicy = new CategoryPolicy()
            {
                CategoryName = "Foobar", // non-existent category
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

            /* Create category policies */

            // Create list to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

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
                                Value = false
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Put,
                                Value = false
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Delete,
                                Value = false
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

            // Assert and Act
            Assert.Throws<InvalidOperationException>(() => SamplesPolicyService.ModifyUserClaim(policies, targetCategoryPolicy));
        }

        [Fact]
        public void AddNewUserClaimInCategoryPolicyIfNotExists()
        {
            /* Arrange */

            // Create the target category policy
            CategoryPolicy targetCategoryPolicy = new CategoryPolicy()
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

            /* Create category policies */

            // Create list to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            CategoryPolicy categoryPolicy = new CategoryPolicy()
            {
                CategoryName = "Getting Started",
                UserClaims = new List<UserClaim>()
                {
                   new UserClaim()
                    {
                        UserName = "Jane Doe",
                        UserPrincipalName = "jane.doe@microsoft.com",
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

            // Act - add new user claim for 'john.doe@microsoft.com'
            SampleQueriesPolicies updatedPolicies = SamplesPolicyService.ModifyUserClaim(policies, targetCategoryPolicy);

            // Assert
            Assert.Collection(updatedPolicies.CategoryPolicies,
                item =>
                {
                    // Assert 'jane.doe@microsoft.com' as the first user claim in the category
                    Assert.Equal("Getting Started", item.CategoryName);
                    Assert.Equal("Jane Doe", item.UserClaims[0].UserName);
                    Assert.Equal("jane.doe@microsoft.com", item.UserClaims[0].UserPrincipalName);
                    Assert.Equal("POST", item.UserClaims[0].UserPermissions[0].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[0].Value);
                    Assert.Equal("PUT", item.UserClaims[0].UserPermissions[1].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[1].Value);
                    Assert.Equal("DELETE", item.UserClaims[0].UserPermissions[2].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[2].Value);

                    // Assert 'john.doe@microsoft.com' as the second user claim in the category
                    Assert.Equal("Getting Started", item.CategoryName);
                    Assert.Equal("John Doe", item.UserClaims[1].UserName);
                    Assert.Equal("john.doe@microsoft.com", item.UserClaims[1].UserPrincipalName);
                    Assert.Equal("POST", item.UserClaims[1].UserPermissions[0].Name);
                    Assert.True(item.UserClaims[1].UserPermissions[0].Value);
                    Assert.Equal("PUT", item.UserClaims[1].UserPermissions[1].Name);
                    Assert.True(item.UserClaims[1].UserPermissions[1].Value);
                    Assert.Equal("DELETE", item.UserClaims[1].UserPermissions[2].Name);
                    Assert.True(item.UserClaims[1].UserPermissions[2].Value);
                });
        }

        [Fact]
        public void ReplaceDefaultUserClaimInCategoryPolicyWithNewUserClaim()
        {
            /* Arrange */

            // Create the target category policy
            CategoryPolicy targetCategoryPolicy = new CategoryPolicy()
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

            /* Create category policies */

            // Create list to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            CategoryPolicy categoryPolicy = new CategoryPolicy()
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

            categoryPolicies.Add(categoryPolicy);

            SampleQueriesPolicies policies = new SampleQueriesPolicies()
            {
                CategoryPolicies = categoryPolicies
            };

            // Act - add new user claim for 'john.doe@microsoft.com'
            SampleQueriesPolicies updatedPolicies = SamplesPolicyService.ModifyUserClaim(policies, targetCategoryPolicy);

            // Assert
            Assert.Collection(updatedPolicies.CategoryPolicies,
                item =>
                {
                    // Assert 'john.doe@microsoft.com' as the first and only user claim in category
                    Assert.Equal("Getting Started", item.CategoryName);
                    Assert.Equal("John Doe", item.UserClaims[0].UserName);
                    Assert.Equal("john.doe@microsoft.com", item.UserClaims[0].UserPrincipalName);
                    Assert.Equal("POST", item.UserClaims[0].UserPermissions[0].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[0].Value);
                    Assert.Equal("PUT", item.UserClaims[0].UserPermissions[1].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[1].Value);
                    Assert.Equal("DELETE", item.UserClaims[0].UserPermissions[2].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[2].Value);
                });
        }

        [Fact]
        public void UpdateUserClaimInCategoryPolicyIfExists()
        {
            /* Arrange */

            // Create the target category policy
            CategoryPolicy targetCategoryPolicy = new CategoryPolicy()
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

            /* Create category policies */

            // Create list to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            CategoryPolicy categoryPolicy = new CategoryPolicy()
            {
                CategoryName = "Getting Started",
                UserClaims = new List<UserClaim>()
                {
                   new UserClaim()
                    {
                        UserName = "Jane Doe",
                        UserPrincipalName = "jane.doe@microsoft.com",
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
                    },                   
                   new UserClaim()
                    {
                        UserName = "John Doe",
                        UserPrincipalName = "john.doe@microsoft.com",
                        UserPermissions = new List<Permission>()
                        {
                            // All permissions are false for this target user claim
                            new Permission()
                            {
                                Name = HttpMethods.Post,
                                Value = false
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Put,
                                Value = false
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Delete,
                                Value = false
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

            // Act - update user claim for 'john.doe@microsoft.com'
            SampleQueriesPolicies updatedPolicies = SamplesPolicyService.ModifyUserClaim(policies, targetCategoryPolicy);

            // Assert
            Assert.Collection(updatedPolicies.CategoryPolicies,
                item =>
                {
                    // Assert 'jane.doe@microsoft.com' as the first user claim in category
                    Assert.Equal("Getting Started", item.CategoryName);
                    Assert.Equal("Jane Doe", item.UserClaims[0].UserName);
                    Assert.Equal("jane.doe@microsoft.com", item.UserClaims[0].UserPrincipalName);
                    Assert.Equal("POST", item.UserClaims[0].UserPermissions[0].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[0].Value);
                    Assert.Equal("PUT", item.UserClaims[0].UserPermissions[1].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[1].Value);
                    Assert.Equal("DELETE", item.UserClaims[0].UserPermissions[2].Name);
                    Assert.True(item.UserClaims[0].UserPermissions[2].Value);

                    /* Assert 'john.doe@microsoft.com' as the second user claim in category
                       with all permissions updated to true */
                    Assert.Equal("Getting Started", item.CategoryName);
                    Assert.Equal("John Doe", item.UserClaims[1].UserName);
                    Assert.Equal("john.doe@microsoft.com", item.UserClaims[1].UserPrincipalName);
                    Assert.Equal("POST", item.UserClaims[1].UserPermissions[0].Name);
                    Assert.True(item.UserClaims[1].UserPermissions[0].Value);
                    Assert.Equal("PUT", item.UserClaims[1].UserPermissions[1].Name);
                    Assert.True(item.UserClaims[1].UserPermissions[1].Value);
                    Assert.Equal("DELETE", item.UserClaims[1].UserPermissions[2].Name);
                    Assert.True(item.UserClaims[1].UserPermissions[2].Value);
                });
        }

        #endregion

        #region Remove User Claim Tests

        [Fact]
        public void ThrowArgumentNullExceptionIfRemoveUserClaimCategoryPolicyNameParameterIsNullOrEmpty()
        {
            /* Arrange */

            string nullCategoryPolicyName = null;
            string emptyCategoryPolicyName = "";

            string userPrincipalName = "john.doe@microsoft.com";

            // Create list to hold category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            /* Create category policies */

            CategoryPolicy categoryPolicy = new CategoryPolicy()
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

            categoryPolicies.Add(categoryPolicy);

            SampleQueriesPolicies policies = new SampleQueriesPolicies()
            {
                CategoryPolicies = categoryPolicies
            };

            /* Act and Assert */

            // Null categoryPolicyName
            Assert.Throws<ArgumentNullException>(() => 
                SamplesPolicyService.RemoveUserClaim(policies, nullCategoryPolicyName, userPrincipalName));

            // Empty categoryPolicyName
            Assert.Throws<ArgumentNullException>(() =>
                SamplesPolicyService.RemoveUserClaim(policies, emptyCategoryPolicyName, userPrincipalName));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfRemoveUserClaimPoliciesParameterIsNull()
        {
            /* Arrange */

            SampleQueriesPolicies nullPolicies = null;
            SampleQueriesPolicies emptyPolicies = new SampleQueriesPolicies();

            string categoryPolicyName = "Getting Started";
            string userPrincipalName = "john.doe@microsoft.com";            

            /* Act and Assert */

            // Null policies
            Assert.Throws<ArgumentNullException>(() => 
                SamplesPolicyService.RemoveUserClaim(nullPolicies, categoryPolicyName, userPrincipalName));

            // Empty policies
            Assert.Throws<ArgumentNullException>(() => SamplesPolicyService.RemoveUserClaim(emptyPolicies, categoryPolicyName, userPrincipalName));
        }

        [Fact]
        public void ThrowInvalidOperationExceptionIfRemoveUserClaimCategoryPolicyNameParameterNotExistsInPoliciesParameter()
        {
            /* Arrange */

            string fakeCategoryName = "Foobar"; // non-existent category
            string userPrincipalName = "john.doe@microsoft.com";

            /* Create category policies */

            // Create list to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

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
                                Value = false
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Put,
                                Value = false
                            },
                            new Permission()
                            {
                                Name = HttpMethods.Delete,
                                Value = false
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

            // Assert and Act
            Assert.Throws<InvalidOperationException>(() => SamplesPolicyService.RemoveUserClaim(policies, fakeCategoryName, userPrincipalName));
        }

        [Fact]
        public void RemoveUserClaimFromCategoryPolicy()
        {
            /* Arrange */

            string categoryPolicyName = "Getting Started";
            string userPrincipalName = "john.doe@microsoft.com";

            /* Create category policies */

            // Create list to hold the category policies
            List<CategoryPolicy> categoryPolicies = new List<CategoryPolicy>();

            CategoryPolicy categoryPolicy = new CategoryPolicy()
            {
                CategoryName = "Getting Started",
                UserClaims = new List<UserClaim>()
                {
                   new UserClaim()
                    {
                        UserName = "Jane Doe",
                        UserPrincipalName = "jane.doe@microsoft.com",
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
                    },
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

            // Act - remove the user claim for 'john.doe@microsoft.com'
            SampleQueriesPolicies updatedPolicies = SamplesPolicyService.RemoveUserClaim(policies, categoryPolicyName, userPrincipalName);

            // Assert
            Assert.Collection(updatedPolicies.CategoryPolicies,
                item =>
                {
                    // Assert 'jane.doe@microsoft.com' as the first and only user claim in category
                    Assert.Equal("Getting Started", item.CategoryName);
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
