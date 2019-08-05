using GraphExplorerSamplesService;
using Newtonsoft.Json;
using System;
using Xunit;
using System.Collections.Generic;

namespace SamplesService.Test
{
    public class SamplesServiceShould
    {
        [Fact]
        public void DeserializeValidJsonStringIntoListOfSampleQueryModelObjects()
        {
            // Arrange
            string validJsonString = @"{
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
                        }
                    ]
            }";

            // Act
            var sampleQueriesList = GraphExplorerSamplesService.SamplesService.GetSampleQueriesList(validJsonString);

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
        public void ThrowJsonReaderExceptionForInvalidJsonStringDeserialization()
        {
            // Arrange
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
            Assert.Throws<JsonReaderException>(() => GraphExplorerSamplesService.SamplesService.GetSampleQueriesList(invalidJsonString));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfGetSampleQueriesListParameterIsNull()
        {
            // Arrange
            string nullArgument = "";

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => GraphExplorerSamplesService.SamplesService.GetSampleQueriesList(nullArgument));
        }

        [Fact]
        public void ReturnNullObjectWhenJsonFileIsEmptyInGetSampleQueriesListParameter()
        {
            // Arrange
            string emptyJsonFile = "{ }";

            // Act
            var sampleQueriesList = GraphExplorerSamplesService.SamplesService.GetSampleQueriesList(emptyJsonFile);

            // Assert 
            Assert.Null(sampleQueriesList.SampleQueries);
        }

        [Fact]
        public void AddSampleQueryIntoListOfSampleQueriesInHierarchicalOrderOfCategories()
        {
            // Arrange
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
                    SkipTest = false},
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList(sampleQueries);

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

            // Act
            // Add the new sample query to the list of sample queries
            SampleQueriesList newSampleQueriesList = GraphExplorerSamplesService.SamplesService.AddToSampleQueriesList
                (sampleQueriesList, ref sampleQueryModel);

            // Assert - the new sample query should be inserted at index [2] of the list of sample queries
            Assert.Equal(sampleQueryModel.Id, newSampleQueriesList.SampleQueries[2].Id);

        }

        [Fact]
        public void UpdateSampleQueryInListOfSampleQueries()
        {
            // Arrange
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
                    SkipTest = false},
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList(sampleQueries);

            /* Create a 'Users' sample query model object that will update an existing 'Users' sample query object in 
             * the list of sample queries  including all the required fields and values that are intended to be retained 
             * along with the new changes. 
             */
            SampleQueryModel sampleQueryModel = new SampleQueryModel()
            {
                Category = "Users",
                Method = SampleQueryModel.HttpMethods.GET,
                HumanName = "my direct reports",
                RequestUrl = "/v1.0/me/directReports",
                DocLink = "https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_directreports",
                SkipTest = false,
                Tip = "This item has been updated." // update
            };

            // Act
            // Update the provided sample query model into the list of sample queries
            SampleQueriesList updatedSampleQueriesList = GraphExplorerSamplesService.SamplesService.UpdateSampleQueriesList
                (sampleQueriesList, sampleQueryModel, Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b"));

            // Assert
            Assert.Equal(sampleQueryModel.Tip, updatedSampleQueriesList.SampleQueries[1].Tip);
        }        

        [Fact]
        public void RemoveSampleQueryFromListOfSampleQueries()
        {
            // Arrange
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
                    SkipTest = false},
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList(sampleQueries);

            Guid idToDelete = Guid.Parse("48b62369-3974-4783-a5c6-ae4ea2d8ae1b");

            // Act
            // Remove the User's query from the list of sample queries
            SampleQueriesList updatedSampleQueriesList = GraphExplorerSamplesService.SamplesService.RemoveSampleQuery
                (sampleQueriesList, idToDelete);

            // Assert - reference to the deleted User's sample query with the given id should be null
            Assert.Null(updatedSampleQueriesList.SampleQueries.Find(x => x.Id == idToDelete));
        }

        [Fact]
        public void SerializeListOfSamplesQueriesIntoJsonString()
        {
            // Arrange
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
                    SkipTest = false},
            };

            SampleQueriesList sampleQueriesList = new SampleQueriesList(sampleQueries);

            // Act
            // Get the serialized JSON string of the list of sample queries
            string newSampleQueriesJson = GraphExplorerSamplesService.SamplesService.SerializeSampleQueriesList(sampleQueriesList);

            // Assert
            Assert.NotNull(newSampleQueriesJson);

        }      
    }
}
