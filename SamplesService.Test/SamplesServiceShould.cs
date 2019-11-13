using Newtonsoft.Json;
using System;
using Xunit;
using System.Collections.Generic;
using GraphExplorerSamplesService.Models;
using Newtonsoft.Json.Linq;

namespace SamplesService.Test
{
    public class SamplesServiceShould
    {
        #region Serialize Sample Queries List Tests

        [Fact]
        public void SerializeListOfSampleQueriesIntoJsonString()
        {
            /* Arrange */

            // Create a list of three sample queries
            List<SampleQueryModel> sampleQueries = new List<SampleQueryModel>()
            {
                new SampleQueryModel()
                {
                    Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                    Category = "Getting Started",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my manager", RequestUrl = "/v1.0/me/manager",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"),
                    Category = "Users",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my direct reports",
                    RequestUrl = "/v1.0/me/directReports",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("7d5bac53-2e16-4162-b78f-7da13e77da1b"),
                    Category = "Groups",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "all groups in my organization",
                    RequestUrl = "/v1.0/groups",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/group",
                    SkipTest = false
                },
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList()
            {
                SampleQueries = sampleQueries
            };

            /* Act */

            // Get the serialized JSON string of the list of sample queries
            string newSampleQueriesJson = GraphExplorerSamplesService.Services.SamplesService.SerializeSampleQueriesList(sampleQueriesList);

            // Assert
            Assert.NotNull(newSampleQueriesJson);
        }

        [Fact]
        public void SerializeSampleQueriesListIfSampleQueriesListParameterIsEmptyCollection()
        {
            // Arrange 
            SampleQueriesList emptySampleQueriesList = new SampleQueriesList();

            // Act
            string sampleQueriesJson = GraphExplorerSamplesService.Services.SamplesService.SerializeSampleQueriesList(emptySampleQueriesList);

            // Assert
            Assert.NotNull(sampleQueriesJson);
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfSerializeSampleQueriesListSampleQueriesListParameterIsNull()
        {
            // Arrange
            SampleQueriesList nullSampleQueriesList = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.SerializeSampleQueriesList(nullSampleQueriesList));
        }

        #endregion

        #region Deserialize Sample Queries List Tests

        [Fact]
        public void DeserializeValidJsonStringIntoOrderedListOfSampleQueryModelObjects()
        {
            // Arrange - sample queries in unsorted order
            string validJsonString = @"{
                   ""SampleQueries"" :
                    [                        
                        {
                        ""id"": ""B4C08825-FD6F-4987-B3CC-14B16ACC84A5"",
                        ""category"": ""Outlook Mail"",
                        ""method"": ""GET"",
                        ""humanName"": ""my high important mail"",
                        ""requestUrl"": ""/v1.0/me/messages?$filter=importance eq 'high'"",
                        ""docLink"": ""https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_messages"",
                        ""skipTest"": false
                        },
                        {
                        ""id"": ""1E019C9D-0B90-49E1-BD4C-C587F75B2B45"",
                        ""category"": ""Groups"",
                        ""method"": ""GET"",
                        ""humanName"": ""all groups I belong to"",
                        ""requestUrl"": ""/v1.0/me/memberOf"",
                        ""docLink"": ""https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_memberof"",
                        ""skipTest"": false
                        },                        
                        {
                        ""id"": ""F1E6738D-7C9C-4DB7-B5EC-1C92DADD03CB"",
                        ""category"": ""Users"",
                        ""method"": ""POST"",
                        ""humanName"": ""create user"",
                        ""requestUrl"": ""/v1.0/users"",
                        ""docLink"": ""https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_post_users"",
                        ""headers"": [
                            {
                                ""name"": ""Content-type"",
                                ""value"": ""application/json""
                            }
                        ],
                        ""postBody"": ""{\r\n \""accountEnabled\"": true,\r\n \""city\"": \""Seattle\"",\r\n \""country\"": \""United States\"",\r\n
                        \""department\"": \""Sales & Marketing\"",\r\n\""displayName\"": \""Melissa Darrow\"",\r\n\""givenName\"": \""Melissa\"",\r\n 
                        \""jobTitle\"": \""Marketing Director\"",\r\n\""mailNickname\"": \""MelissaD\"",\r\n \""passwordPolicies\"": 
                        \""DisablePasswordExpiration\"",\r\n \""passwordProfile\"": {\r\n \""password\"": \""Test1234\"",\r\n
                        \""forceChangePasswordNextSignIn\"": false\r\n},\r\n \""officeLocation\"": \""131/1105\"",\r\n\""postalCode\"": \""98052\"",\r\n
                        \""preferredLanguage\"": \""en -US\"",\r\n \""state\"": \""WA\"",\r\n \""streetAddress\"": \""9256 Towne Center Dr., Suite 400\"",\r\n 
                        \""surname\"": \""Darrow\"",\r\n \""mobilePhone\"": \"" + 1 206 555 0110\"",\r\n \""usageLocation\"": \""US\"",\r\n \""userPrincipalName\"": 
                        \""MelissaD@{domain}\""\r\n }"",
                        ""skipTest"": false
                        },
                        {
                        ""id"": ""F1E6738D-7C9C-4DB7-B5EC-1C92DADD03CB"",
                        ""category"": ""Getting Started"",
                        ""method"": ""GET"",
                        ""humanName"": ""my profile"",
                        ""requestUrl"": ""/v1.0/me/"",
                        ""docLink"": ""https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/users"",
                        ""skipTest"": false
                        }
                    ]
            }";

            // Act
            SampleQueriesList sampleQueriesList = GraphExplorerSamplesService.Services.SamplesService.DeserializeSampleQueriesList(validJsonString);

            /* Assert that the sample queries are returned in alphabetical order of their category names (with 'Getting Started' at the top-most)
             * and with all details and count of items correct */

            Assert.True(sampleQueriesList.SampleQueries.Count == 4);
            Assert.Collection(sampleQueriesList.SampleQueries,
                item =>
                {
                    Assert.Equal(Guid.Parse("F1E6738D-7C9C-4DB7-B5EC-1C92DADD03CB"), item.Id);
                    Assert.Equal("Getting Started", item.Category);
                    Assert.Equal(SampleQueryModel.HttpMethods.GET, item.Method);
                    Assert.Equal("my profile", item.HumanName);
                    Assert.Equal("/v1.0/me/", item.RequestUrl);
                    Assert.Equal("https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/users", item.DocLink);
                    Assert.False(item.SkipTest);
                },
                item =>
                {
                    Assert.Equal(Guid.Parse("1E019C9D-0B90-49E1-BD4C-C587F75B2B45"), item.Id);
                    Assert.Equal("Groups", item.Category);
                    Assert.Equal(SampleQueryModel.HttpMethods.GET, item.Method);
                    Assert.Equal("all groups I belong to", item.HumanName);
                    Assert.Equal("/v1.0/me/memberOf", item.RequestUrl);
                    Assert.Equal("https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_memberof", item.DocLink);
                    Assert.False(item.SkipTest);
                },
                item =>
                {
                    Assert.Equal(Guid.Parse("B4C08825-FD6F-4987-B3CC-14B16ACC84A5"), item.Id);
                    Assert.Equal("Outlook Mail", item.Category);
                    Assert.Equal(SampleQueryModel.HttpMethods.GET, item.Method);
                    Assert.Equal("my high important mail", item.HumanName);
                    Assert.Equal("/v1.0/me/messages?$filter=importance eq 'high'", item.RequestUrl);
                    Assert.Equal("https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_messages", item.DocLink);
                    Assert.False(item.SkipTest);
                },
                item =>
                {
                    Assert.Equal(Guid.Parse("F1E6738D-7C9C-4DB7-B5EC-1C92DADD03CB"), item.Id);
                    Assert.Equal("Users", item.Category);
                    Assert.Equal(SampleQueryModel.HttpMethods.POST, item.Method);
                    Assert.Equal("create user", item.HumanName);
                    Assert.Equal("/v1.0/users", item.RequestUrl);
                    Assert.Equal("https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_post_users", item.DocLink);
                    Assert.NotEmpty(item.Headers);
                    Assert.NotEmpty(item.PostBody);
                    Assert.False(item.SkipTest);
                });
        }

        [Fact]
        public void ReplaceDefaultPasswordWithUniqueValueInPostBodyTemplateOfUsersSampleQuery()
        {
            /* Act */

            string jsonString = @"{
                   ""SampleQueries"" :
                    [                        
                        {
                        ""id"": ""F1E6738D-7C9C-4DB7-B5EC-1C92DADD03CB"",
                        ""category"": ""Users"",
                        ""method"": ""POST"",
                        ""humanName"": ""create user"",
                        ""requestUrl"": ""/v1.0/users"",
                        ""docLink"": ""https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_post_users"",
                        ""headers"": [
                            {
                                ""name"": ""Content-type"",
                                ""value"": ""application/json""
                            }
                        ],
                        ""postBody"": ""{\r\n \""accountEnabled\"": true,\r\n \""city\"": \""Seattle\"",\r\n \""country\"": \""United States\"",\r\n
                        \""department\"": \""Sales & Marketing\"",\r\n\""displayName\"": \""Melissa Darrow\"",\r\n\""givenName\"": \""Melissa\"",\r\n 
                        \""jobTitle\"": \""Marketing Director\"",\r\n\""mailNickname\"": \""MelissaD\"",\r\n \""passwordPolicies\"": 
                        \""DisablePasswordExpiration\"",\r\n \""passwordProfile\"": {\r\n \""password\"": \""Test1234\"",\r\n
                        \""forceChangePasswordNextSignIn\"": false\r\n},\r\n \""officeLocation\"": \""131/1105\"",\r\n\""postalCode\"": \""98052\"",\r\n
                        \""preferredLanguage\"": \""en -US\"",\r\n \""state\"": \""WA\"",\r\n \""streetAddress\"": \""9256 Towne Center Dr., Suite 400\"",\r\n 
                        \""surname\"": \""Darrow\"",\r\n \""mobilePhone\"": \"" + 1 206 555 0110\"",\r\n \""usageLocation\"": \""US\"",\r\n \""userPrincipalName\"": 
                        \""MelissaD@{domain}\""\r\n }"",
                        ""skipTest"": false
                        }
                    ]
            }";

            /* Extract the original password from the passwordProfile key in the postBody template of the Users sample query. */

            JObject sampleQueryObject = JObject.Parse(jsonString);

            JObject originalPostBodyObject = JObject.Parse(sampleQueryObject.SelectToken("SampleQueries[0].postBody").ToString());

            string originalPassword = (string)originalPostBodyObject["passwordProfile"]["password"];

            /* Act */

            SampleQueriesList sampleQueriesList = GraphExplorerSamplesService.Services.SamplesService.DeserializeSampleQueriesList(jsonString);

            /* Extract the newly generated password. */

            SampleQueryModel sampleQuery = sampleQueriesList.SampleQueries
                .Find(s => s.Category == "Users" && s.Method == SampleQueryModel.HttpMethods.POST);

            JObject newPostBodyObject = JObject.Parse(sampleQuery.PostBody);
            string newPassword = (string)newPostBodyObject["passwordProfile"]["password"];

            // Assert that the newly generated password is of type Guid and is unique
            Assert.Collection(sampleQueriesList.SampleQueries,                
                item =>
                {
                    Assert.IsType<Guid>(Guid.Parse(newPassword));
                    Assert.NotEqual(originalPassword, newPassword);
                });
        }

        [Fact]
        public void ThrowJsonReaderExceptionIfDeserializeSampleQueriesListJsonStringParameterIsInvalidJsonString()
        {
            /* Arrange */

            // JSON string missing a closing brace '}' and comma separator ',' after the first object definition
            string invalidJsonString = @"{
                   ""SampleQueries"" :
                    [
                        {
                        ""id"": ""F1E6738D-7C9C-4DB7-B5EC-1C92DADD03CB"",
                        ""category"": ""Getting Started"",
                        ""method"": ""GET"",
                        ""humanName"": ""my profile"",
                        ""requestUrl"": ""/v1.0/me/"",
                        ""docLink"": ""https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/users"",
                        ""skipTest"": false
                        
                        {
                        ""id"": ""F1E6738D-7C9C-4DB7-B5EC-1C92DADD03CB"",
                        ""category"": ""Users"",
                        ""method"": ""POST"",
                        ""humanName"": ""create user"",
                        ""requestUrl"": ""/v1.0/users"",
                        ""docLink"": ""https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_post_users"",
                        ""headers"": [
                            {
                                ""name"": ""Content-type"",
                                ""value"": ""application/json""
                            }
                        ],
                        ""postBody"": ""{\r\n \""accountEnabled\"": true,\r\n \""city\"": \""Seattle\"",\r\n \""country\"": \""United States\"",\r\n
                        \""department\"": \""Sales & Marketing\"",\r\n\""displayName\"": \""Melissa Darrow\"",\r\n\""givenName\"": \""Melissa\"",\r\n 
                        \""jobTitle\"": \""Marketing Director\"",\r\n\""mailNickname\"": \""MelissaD\"",\r\n \""passwordPolicies\"": 
                        \""DisablePasswordExpiration\"",\r\n \""passwordProfile\"": {\r\n \""password\"": \""Test1234\"",\r\n
                        \""forceChangePasswordNextSignIn\"": false\r\n},\r\n \""officeLocation\"": \""131/1105\"",\r\n\""postalCode\"": \""98052\"",\r\n
                        \""preferredLanguage\"": \""en -US\"",\r\n \""state\"": \""WA\"",\r\n \""streetAddress\"": \""9256 Towne Center Dr., Suite 400\"",\r\n 
                        \""surname\"": \""Darrow\"",\r\n \""mobilePhone\"": \"" + 1 206 555 0110\"",\r\n \""usageLocation\"": \""US\"",\r\n \""userPrincipalName\"": 
                        \""MelissaD@{domain}\""\r\n }"",
                        ""skipTest"": false
                        }
                    ]
            }";

            // Act and Assert
            Assert.Throws<JsonReaderException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.DeserializeSampleQueriesList(invalidJsonString));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfDeserializeSampleQueriesListJsonStringParameterIsNull()
        {
            // Arrange
            string nullArgument = "";

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.DeserializeSampleQueriesList(nullArgument));
        }

        [Fact]
        public void ReturnEmptyCollectionWhenJsonFileIsEmptyInDeserializeSampleQueriesListJsonStringParameter()
        {
            // Arrange
            string emptyJsonFileContents = "{ }";

            // Act                        
            SampleQueriesList sampleQueriesList = GraphExplorerSamplesService.Services.SamplesService.DeserializeSampleQueriesList(emptyJsonFileContents);

            // Assert
            Assert.Empty(sampleQueriesList.SampleQueries);
        }

        #endregion

        #region Update Sample Queries List Tests

        [Fact]
        public void UpdateSampleQueryInListOfSampleQueries()
        {
            /* Arrange */

            // Create a list of sample queries
            List<SampleQueryModel> sampleQueries = new List<SampleQueryModel>()
            {
                new SampleQueryModel()
                {
                    Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                    Category = "Getting Started",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my manager", RequestUrl = "/v1.0/me/manager",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"),
                    Category = "Users",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my direct reports",
                    RequestUrl = "/v1.0/me/directReports",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("7d5bac53-2e16-4162-b78f-7da13e77da1b"),
                    Category = "Groups",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "all groups in my organization",
                    RequestUrl = "/v1.0/groups",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/group",
                    SkipTest = false
                }
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList()
            {
                SampleQueries = sampleQueries
            };

            /* Create a 'Users' sample query model object that will update an existing 'Users' 
             * sample query object in the list of sample queries. 
             */
            SampleQueryModel sampleQueryModel = new SampleQueryModel()
            {
                Category = "Users",
                Method = SampleQueryModel.HttpMethods.GET,
                HumanName = "my direct reports",
                RequestUrl = "/v1.0/me/directReports",
                DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                SkipTest = false,
                Tip = "This item has been updated." // update field
            };

            Guid id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b");

            /* Act */

            // Update the provided sample query model into the list of sample queries
            SampleQueriesList updatedSampleQueriesList = GraphExplorerSamplesService.Services.SamplesService.UpdateSampleQueriesList
                (sampleQueriesList, sampleQueryModel, id);

            // Assert - item on index[1] has an updated property value
            Assert.Equal(sampleQueryModel.Tip, updatedSampleQueriesList.SampleQueries[1].Tip);
        }

        [Fact]
        public void ThrowInvalidOperationExceptionIfUpdateSampleQueriesListSampleQueryIdNotFoundInCollection()
        {
            /* Arrange */

            // Create a list of sample queries
            List<SampleQueryModel> sampleQueries = new List<SampleQueryModel>()
            {
                new SampleQueryModel()
                {
                    Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                    Category = "Getting Started",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my manager", RequestUrl = "/v1.0/me/manager",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"),
                    Category = "Users",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my direct reports",
                    RequestUrl = "/v1.0/me/directReports",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("7d5bac53-2e16-4162-b78f-7da13e77da1b"),
                    Category = "Groups",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "all groups in my organization",
                    RequestUrl = "/v1.0/groups",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/group",
                    SkipTest = false
                }
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList()
            {
                SampleQueries = sampleQueries
            };

            /* Create a 'Users' sample query model object that should update an existing 'Users' 
             * sample query object in the list of sample queries. 
             */
            SampleQueryModel sampleQueryModel = new SampleQueryModel()
            {
                Category = "Users",
                Method = SampleQueryModel.HttpMethods.GET,
                HumanName = "my direct reports",
                RequestUrl = "/v1.0/me/directReports",
                DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                SkipTest = false,
                Tip = "This item has been updated." // updated field
            };

            Guid id = Guid.Parse("5484add6-d4be-4560-82ef-31ef738775e8"); // non-existent id

            /* Act and Assert */

            // Attempt to update the provided sample query model into the list of sample queries with a non existent id
            Assert.Throws<InvalidOperationException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.UpdateSampleQueriesList
                (sampleQueriesList, sampleQueryModel, id));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfUpdateSampleQueriesListSampleQueryModelParameterIsNull()
        {
            /* Arrange */

            // Create list of sample queries
            List<SampleQueryModel> sampleQueries = new List<SampleQueryModel>()
            {
                new SampleQueryModel()
                {
                    Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                    Category = "Getting Started",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my manager", RequestUrl = "/v1.0/me/manager",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"),
                    Category = "Users",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my direct reports",
                    RequestUrl = "/v1.0/me/directReports",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("7d5bac53-2e16-4162-b78f-7da13e77da1b"),
                    Category = "Groups",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "all groups in my organization",
                    RequestUrl = "/v1.0/groups",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/group",
                    SkipTest = false
                }
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList()
            {
                SampleQueries = sampleQueries
            };

            SampleQueryModel sampleQueryModel = null;

            Guid id = Guid.NewGuid();

            /* Act and Assert */

            // Null sample query model
            Assert.Throws<ArgumentNullException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.UpdateSampleQueriesList(sampleQueriesList, sampleQueryModel, id));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfUpdateSampleQueriesListSampleQueryIdParameterIsEmpty()
        {
            /* Arrange */

            // Create list of sample queries
            List<SampleQueryModel> sampleQueries = new List<SampleQueryModel>()
            {
                new SampleQueryModel()
                {
                    Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                    Category = "Getting Started",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my manager", RequestUrl = "/v1.0/me/manager",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"),
                    Category = "Users",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my direct reports",
                    RequestUrl = "/v1.0/me/directReports",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                    SkipTest = false
                }
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList()
            {
                SampleQueries = sampleQueries
            }; 

            SampleQueryModel sampleQueryModel = new SampleQueryModel()
            {
                Id = Guid.Parse("7d5bac53-2e16-4162-b78f-7da13e77da1b"),
                Category = "Groups",
                Method = SampleQueryModel.HttpMethods.GET,
                HumanName = "all groups in my organization",
                RequestUrl = "/v1.0/groups",
                DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/group",
                SkipTest = false
            };

            Guid id = Guid.Empty;

            /* Act and Assert */

            // Empty sample query id
            Assert.Throws<ArgumentNullException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.UpdateSampleQueriesList(sampleQueriesList, sampleQueryModel, id));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfUpdateSampleQueriesListSampleQueriesListParameterIsNullOrEmptyCollection()
        {
            /* Arrange */

            SampleQueriesList nullSampleQueriesList = null;

            SampleQueriesList emptySampleQueriesList = new SampleQueriesList();

            SampleQueryModel sampleQueryModel = new SampleQueryModel()
            {
                Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                Category = "Getting Started",
                Method = SampleQueryModel.HttpMethods.GET,
                HumanName = "my manager",
                RequestUrl = "/v1.0/me/manager",
                DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                SkipTest = false
            };

            Guid id = Guid.NewGuid();

            /* Act and Assert */

            // Null sample queries list
            Assert.Throws<ArgumentNullException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.UpdateSampleQueriesList(nullSampleQueriesList, sampleQueryModel, id));

            // Empty sample queries list
            Assert.Throws<ArgumentNullException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.UpdateSampleQueriesList(emptySampleQueriesList, sampleQueryModel, id));
        }

        #endregion

        #region Add To Sample Queries List Tests

        [Fact]
        public void AddSampleQueryIntoEmptyListOfSampleQueries()
        {
            /* Arrange */

            SampleQueriesList sampleQueriesList = new SampleQueriesList();

            SampleQueryModel sampleQueryModel = new SampleQueryModel()
            {
                Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                Category = "Getting Started",
                Method = SampleQueryModel.HttpMethods.GET,
                HumanName = "my manager",
                RequestUrl = "/v1.0/me/manager",
                DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                SkipTest = false
            };

            /* Act */

            // Add the new sample query to the empty list of sample queries
            sampleQueriesList = GraphExplorerSamplesService.Services.SamplesService.AddToSampleQueriesList
                (sampleQueriesList, sampleQueryModel);

            // Assert
            Assert.NotEmpty(sampleQueriesList.SampleQueries);
        }
               
        [Fact]
        public void AddSampleQueryIntoListOfSampleQueriesInHierarchicalOrderOfCategories()
        {
            /* Arrange */

            // Create a list of sample queries
            List<SampleQueryModel> sampleQueries = new List<SampleQueryModel>()
            {
                new SampleQueryModel()
                {
                    Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                    Category = "Getting Started",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my manager", RequestUrl = "/v1.0/me/manager",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"),
                    Category = "Users",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my direct reports",
                    RequestUrl = "/v1.0/me/directReports",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("7d5bac53-2e16-4162-b78f-7da13e77da1b"),
                    Category = "Groups",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "all groups in my organization",
                    RequestUrl = "/v1.0/groups",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/group",
                    SkipTest = false},
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList()
            {
                SampleQueries = sampleQueries
            };

            /* Create a new 'Users' sample query model object that will be inserted into  
             * the list of sample queries in a hierarchical order of categories.
             */
            SampleQueryModel sampleQueryModel = new SampleQueryModel()
            {
                Category = "Users",
                Method = SampleQueryModel.HttpMethods.GET,
                HumanName = "all users in the organization",
                RequestUrl = "/v1.0/users",
                DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/users",
                SkipTest = false,
            };

            /* Act */

            // Assign a new Id to the new sample query
            sampleQueryModel.Id = Guid.NewGuid();

            // Add the new sample query to the list of sample queries
            SampleQueriesList newSampleQueriesList = GraphExplorerSamplesService.Services.SamplesService.AddToSampleQueriesList
                (sampleQueriesList, sampleQueryModel);

            // Assert - the new sample query should be inserted at index[2] of the list of sample queries
            Assert.Equal(sampleQueryModel.Id, newSampleQueriesList.SampleQueries[2].Id);
        }

        [Fact]
        public void AddSampleQueryToTopOfListOfSampleQueriesIfHighestRankedSampleQueryCategory()
        {
            /* Arrange */

            // Create a list of sample queries
            List<SampleQueryModel> sampleQueries = new List<SampleQueryModel>()
            {               
                new SampleQueryModel()
                {
                    Id = Guid.Parse("7d5bac53-2e16-4162-b78f-7da13e77da1b"),
                    Category = "Groups",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "all groups in my organization",
                    RequestUrl = "/v1.0/groups",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/group",
                    SkipTest = false
                },
                 new SampleQueryModel()
                {
                    Id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"),
                    Category = "Outlook Mail",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my high important mail",
                    RequestUrl = "/v1.0/me/messages?$filter=importance eq 'high'",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_messages",
                    SkipTest = false
                }
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList()
            {
                SampleQueries = sampleQueries
            };

            /* Create a new 'Users' sample query model object that will be inserted into  
             * the list of sample queries as the highest ranked sample category
             */
            SampleQueryModel sampleQueryModel = new SampleQueryModel()
            {
                Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                Category = "Users",
                Method = SampleQueryModel.HttpMethods.GET,
                HumanName = "my manager",
                RequestUrl = "/v1.0/me/manager",
                DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                SkipTest = false
            };

            /* Act */

            // Assign a new Id to the new sample query
            sampleQueryModel.Id = Guid.NewGuid();

            // Add the new sample query to the list of sample queries
            SampleQueriesList newSampleQueriesList = GraphExplorerSamplesService.Services.SamplesService.AddToSampleQueriesList
                (sampleQueriesList, sampleQueryModel);

            // Assert - the new sample query should be inserted at index[0] of the list of sample queries
            Assert.Equal(sampleQueryModel.Id, newSampleQueriesList.SampleQueries[0].Id);
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfAddToSampleQueriesListSampleQueriesListParameterIsNull()
        {
            /* Arrange */

            SampleQueriesList nullSampleQueriesList = null;
            
            SampleQueryModel sampleQueryModel = new SampleQueryModel()
            {
                Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                Category = "Getting Started",
                Method = SampleQueryModel.HttpMethods.GET,
                HumanName = "my manager",
                RequestUrl = "/v1.0/me/manager",
                DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                SkipTest = false
            };
            
            /* Act and Assert */

            // Assign a new Id to the new sample query
            sampleQueryModel.Id = Guid.NewGuid();

            // Null sample queries list
            Assert.Throws<ArgumentNullException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.AddToSampleQueriesList(nullSampleQueriesList, sampleQueryModel));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfAddToSampleQueriesListSampleQueryModelParameterIsNull()
        {
            /* Arrange */

            // Create list of sample queries
            List<SampleQueryModel> sampleQueries = new List<SampleQueryModel>()
            {
                new SampleQueryModel()
                {
                    Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                    Category = "Getting Started",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my manager", RequestUrl = "/v1.0/me/manager",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"),
                    Category = "Users",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my direct reports",
                    RequestUrl = "/v1.0/me/directReports",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("7d5bac53-2e16-4162-b78f-7da13e77da1b"),
                    Category = "Groups",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "all groups in my organization",
                    RequestUrl = "/v1.0/groups",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/group",
                    SkipTest = false
                }
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList()
            {
                SampleQueries = sampleQueries
            };

            SampleQueryModel sampleQueryModel = null;
            
            /* Act and Assert */
            
            // Null sample query model
            Assert.Throws<ArgumentNullException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.AddToSampleQueriesList(sampleQueriesList, sampleQueryModel));
        }

        #endregion

        #region Remove Sample Query Tests

        [Fact]
        public void RemoveSampleQueryFromListOfSampleQueries()
        {
            /* Arrange */

            // Create a list of sample queries
            List<SampleQueryModel> sampleQueries = new List<SampleQueryModel>()
            {
                new SampleQueryModel()
                {
                    Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                    Category = "Getting Started",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my manager", RequestUrl = "/v1.0/me/manager",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"),
                    Category = "Users",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my direct reports",
                    RequestUrl = "/v1.0/me/directReports",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("7d5bac53-2e16-4162-b78f-7da13e77da1b"),
                    Category = "Groups",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "all groups in my organization",
                    RequestUrl = "/v1.0/groups",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/group",
                    SkipTest = false},
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList()
            {
                SampleQueries = sampleQueries
            };

            Guid idToDelete = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"); // id of sample query to delete from list of sample queries

            /* Act */

            // Remove the User's query from the list of sample queries
            SampleQueriesList updatedSampleQueriesList = GraphExplorerSamplesService.Services.SamplesService.RemoveSampleQuery
                (sampleQueriesList, idToDelete);

            // Assert - reference to the deleted User's sample query with the given id should be null
            Assert.Null(updatedSampleQueriesList.SampleQueries.Find(x => x.Id == idToDelete));
        }

        [Fact]
        public void ThrowInvalidOperationExceptionIfRemoveSampleQuerySampleQueryIdNotFoundInCollection()
        {
            /* Arrange */

            // Create a list of sample queries
            List<SampleQueryModel> sampleQueries = new List<SampleQueryModel>()
            {
                new SampleQueryModel()
                {
                    Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                    Category = "Getting Started",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my manager", RequestUrl = "/v1.0/me/manager",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"),
                    Category = "Users",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my direct reports",
                    RequestUrl = "/v1.0/me/directReports",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("7d5bac53-2e16-4162-b78f-7da13e77da1b"),
                    Category = "Groups",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "all groups in my organization",
                    RequestUrl = "/v1.0/groups",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/group",
                    SkipTest = false
                }
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList()
            {
                SampleQueries = sampleQueries
            };

            Guid id = Guid.Parse("e6819ecc-f5aa-4792-ac86-c25234383513"); // Non-existent id

            /* Act and Assert */

            // Attempt to remove a sample query from the list of sample queries with a non-existent id
            Assert.Throws<InvalidOperationException>(() =>
                GraphExplorerSamplesService.Services.SamplesService.RemoveSampleQuery
                (sampleQueriesList, id));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfRemoveSampleQuerySampleQueriesListParameterIsNullOrEmptyCollection()
        {
            /* Arrange */

            SampleQueriesList nullSampleQueriesList = null;

            SampleQueriesList emptySampleQueriesList = new SampleQueriesList();

            Guid id = Guid.Parse("9fd2bcca-6597-4fd8-a6de-0119a0b94e20");

            /* Act and Assert */

            // Null sample queries list
            Assert.Throws<ArgumentNullException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.RemoveSampleQuery(nullSampleQueriesList, id));

            // Empty sample queries list
            Assert.Throws<ArgumentNullException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.RemoveSampleQuery(emptySampleQueriesList, id));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfRemoveSampleQuerySampleQueryIdParameterIsEmpty()
        {
            /* Arrange */

            // Create list of sample queries
            List<SampleQueryModel> sampleQueries = new List<SampleQueryModel>()
            {
                new SampleQueryModel()
                {
                    Id = Guid.Parse("3482cc10-f2be-40fc-bcdb-d3ac35f3e4c3"),
                    Category = "Getting Started",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my manager", RequestUrl = "/v1.0/me/manager",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_manager",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"),
                    Category = "Users",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "my direct reports",
                    RequestUrl = "/v1.0/me/directReports",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                    SkipTest = false
                },
                new SampleQueryModel()
                {
                    Id = Guid.Parse("7d5bac53-2e16-4162-b78f-7da13e77da1b"),
                    Category = "Groups",
                    Method = SampleQueryModel.HttpMethods.GET,
                    HumanName = "all groups in my organization",
                    RequestUrl = "/v1.0/groups",
                    DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/group",
                    SkipTest = false
                }
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList()
            {
                SampleQueries = sampleQueries
            };

            Guid id = Guid.Empty;

            /* Act and Assert */

            // Empty sample query Id
            Assert.Throws<ArgumentNullException>(() => 
                GraphExplorerSamplesService.Services.SamplesService.RemoveSampleQuery(sampleQueriesList, id));
        }

        #endregion
    }
}
