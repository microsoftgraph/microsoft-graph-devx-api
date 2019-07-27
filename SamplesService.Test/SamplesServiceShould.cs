using GraphExplorerSamplesService;
using Newtonsoft.Json;
using System;
using Xunit;

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

            // Assert that all required fields are equal
            Assert.Collection(sampleQueriesList.SampleQueries, 
                item => 
                {
                    Assert.Equal(Guid.Parse("F1E6738D-7C9C-4DB7-B5EC-1C92DADD03CB"), item.Id);
                    Assert.Equal("Getting Started", item.Category);
                    Assert.Equal(SampleQueryModel.HttpMethods.GET, item.Method);
                    Assert.Equal("my profile", item.HumanName);
                    Assert.Equal("/v1.0/me/", item.RequestUrl);
                },
                item =>
                {
                    Assert.Equal(Guid.Parse("F1E6738D-7C9C-4DB7-B5EC-1C92DADD03CB"), item.Id);
                    Assert.Equal("Users", item.Category);
                    Assert.Equal(SampleQueryModel.HttpMethods.POST, item.Method);
                    Assert.Equal("create user", item.HumanName);
                    Assert.Equal("/v1.0/users", item.RequestUrl);
                });
        }

        [Fact]
        public void ThrowJsonReaderExceptionForInvalidJsonString()
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
        public void ThrowArgumentNullExceptionIfParameterIsNull()
        {
            // Arrange
            string nullArgument = "";

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => GraphExplorerSamplesService.SamplesService.GetSampleQueriesList(nullArgument));

        }

        [Fact]
        public void ReturnNullObjectWhenJsonFileIsEmpty()
        {
            // Arrange
            string emptyJsonFile = "{ }";

            // Act
            var sampleQueriesList = GraphExplorerSamplesService.SamplesService.GetSampleQueriesList(emptyJsonFile);

            // Assert 
            Assert.Null(sampleQueriesList.SampleQueries);
        }
    }
}
