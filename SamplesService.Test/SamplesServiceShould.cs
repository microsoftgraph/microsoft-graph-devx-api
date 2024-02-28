// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using Xunit;
using System.Collections.Generic;
using SamplesService.Models;
using System.Text.Json;

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
            string newSampleQueriesJson = Services.SamplesService.SerializeSampleQueriesList(sampleQueriesList);

            // Assert
            Assert.NotNull(newSampleQueriesJson);
        }

        [Fact]
        public void SerializeSampleQueriesListIfSampleQueriesListParameterIsEmptyCollection()
        {
            // Arrange
            SampleQueriesList emptySampleQueriesList = new SampleQueriesList();

            // Act
            string sampleQueriesJson = Services.SamplesService.SerializeSampleQueriesList(emptySampleQueriesList);

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
                Services.SamplesService.SerializeSampleQueriesList(nullSampleQueriesList));
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
                            ""postBody"": ""{\r\n \""accountEnabled\"": true,\r\n \""city\"": \""Seattle\"",\r\n \""country\"": \""United States\"",\r\n \""department\"": \""Sales & Marketing\"",\r\n \""displayName\"": \""Melissa Darrow\"",\r\n \""givenName\"": \""Melissa\"",\r\n \""jobTitle\"": \""Marketing Director\"",\r\n \""mailNickname\"": \""MelissaD\"",\r\n \""passwordPolicies\"": \""DisablePasswordExpiration\"",\r\n \""passwordProfile\"": {\r\n \""password\"": \""Test1234\"",\r\n \""forceChangePasswordNextSignIn\"": false\r\n},\r\n \""officeLocation\"": \""131/1105\"",\r\n \""postalCode\"": \""98052\"",\r\n \""preferredLanguage\"": \""en-US\"",\r\n \""state\"": \""WA\"",\r\n \""streetAddress\"": \""9256 Towne Center Dr., Suite 400\"",\r\n \""surname\"": \""Darrow\"",\r\n \""mobilePhone\"": \"" + 1 206 555 0110\"",\r\n \""usageLocation\"": \""US\"",\r\n \""userPrincipalName\"": \""MelissaD@{domain}\""\r\n }"",
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
            SampleQueriesList sampleQueriesList = Services.SamplesService.DeserializeSampleQueriesList(validJsonString, true);

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
        public void ThrowJsonExceptionIfDeserializeSampleQueriesListJsonStringParameterIsInvalidJsonString()
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
            Assert.Throws<JsonException>(() =>
                Services.SamplesService.DeserializeSampleQueriesList(invalidJsonString));
        }

        [Fact]
        public void ReturnNullIfDeserializeSampleQueriesListJsonStringParameterIsNull()
        {
            // Arrange
            string nullArgument = null;

            // Act and Assert
            SampleQueriesList sampleQueriesList = Services.SamplesService.DeserializeSampleQueriesList(nullArgument);

            Assert.Null(sampleQueriesList);
        }

        [Fact]
        public void ReturnEmptyCollectionWhenJsonFileIsEmptyInDeserializeSampleQueriesListJsonStringParameter()
        {
            // Arrange
            string emptyJsonFileContents = "{ }";

            // Act
            SampleQueriesList sampleQueriesList = Services.SamplesService.DeserializeSampleQueriesList(emptyJsonFileContents);

            // Assert
            Assert.Empty(sampleQueriesList.SampleQueries);
        }

        #endregion
    }
}
