using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using Microsoft.OpenApi.Services;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class SnippetCodeGraphTests
    {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        private static OpenApiUrlTreeNode _v1TreeNode;

        private static string TypesSample = @"
            { 
                ""attendees"": [ 
                { 
                    ""type"": ""null"",  
                    ""emailAddress"": { 
                    ""name"": ""Alex Wilbur"",
                    ""address"": ""alexw@contoso.onmicrosoft.com"" 
                    } 
                }
                ],  
                ""locationConstraint"": { 
                ""isRequired"": false,  
                ""suggestLocation"": false,  
                ""locations"": [ 
                    { 
                    ""resolveAvailability"": false,
                    ""displayName"": ""Conf room Hood"" 
                    } 
                ] 
                },  
                ""timeConstraint"": {
                ""activityDomain"":""work"", 
                ""timeSlots"": [ 
                    { 
                    ""start"": { 
                        ""dateTime"": ""2019-04-16T09:00:00"",  
                        ""timeZone"": ""Pacific Standard Time"" 
                    },  
                    ""end"": { 
                        ""dateTime"": ""2019-04-18T17:00:00"",  
                        ""timeZone"": ""Pacific Standard Time"" 
                    } 
                    } 
                ] 
                },  
                ""isOrganizerOptional"": ""false"",
                ""meetingDuration"": ""PT1H"",
                ""returnSuggestionReasons"": ""true"",
                ""minimumAttendeePercentage"": 100
            }
            ";

        // read the file from disk
        private static async Task<OpenApiUrlTreeNode> GetV1TreeNode()
        {
            if (_v1TreeNode == null)
            {
                _v1TreeNode = await SnippetModelTests.GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml");
            }
            return _v1TreeNode;
        }

        [Fact]
        public async Task ParsesHeaders()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users");

            request.Headers.Add("Host", "graph.microsoft.com");
            request.Headers.Add("Prefer", "outlook.timezone=\"Pacific Standard Time\"");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var result = new SnippetCodeGraph(snippetModel);
            var header = result.Headers.First();

            Assert.True(result.HasHeaders());
            Assert.Single(result.Headers); // host should be ignored in headers
            Assert.Equal("outlook.timezone=\"Pacific Standard Time\"", header.Value);
            Assert.Equal("Prefer", header.Name);
            Assert.Equal(PropertyType.String, header.PropertyType);
        }

        [Fact]
        public async Task HasHeadeIsFalseWhenNoneIsInRequest()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users");
            request.Headers.Add("Host", "graph.microsoft.com");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var result = new SnippetCodeGraph(snippetModel);

            Assert.False(result.HasHeaders());
        }

        [Fact]
        public async Task ParsesParameters()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/19:4b6bed8d24574f6a9e436813cb2617d8?$select=displayName,givenName,postalCode,identities");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var result = new SnippetCodeGraph(snippetModel);
            var parameter = result.Parameters.First();

            Assert.True(result.HasParameters());
            Assert.Single(result.Parameters);

            var expectedProperty = new CodeProperty { Name = "select", Value = "displayName,givenName,postalCode,identities", PropertyType = PropertyType.String, Children = null };
            Assert.Equal(expectedProperty, parameter);

            Assert.Equal("displayName,givenName,postalCode,identities", parameter.Value);
            Assert.Equal("select", parameter.Name);
            Assert.Equal(PropertyType.String, parameter.PropertyType);

            Assert.True(result.HasParameters());
        }

        [Fact]
        public async Task ParsesQueryParametersWithSpaces()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/roleManagement/directory/roleAssignments?$filter=roleDefinitionId eq '62e90394-69f5-4237-9190-012177145e10'&$expand=principal");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var result = new SnippetCodeGraph(snippetModel);
            var parameter = result.Parameters.First();

            Assert.True(result.HasParameters());
            Assert.Equal(2, result.Parameters.Count());

            var expectedProperty1 = new CodeProperty { Name = "filter", Value = "roleDefinitionId eq '62e90394-69f5-4237-9190-012177145e10'", PropertyType = PropertyType.String, Children = null };
            Assert.Equal(expectedProperty1, result.Parameters.First());

            var expectedProperty2 = new CodeProperty { Name = "expand", Value = "principal", PropertyType = PropertyType.String, Children = null };
            Assert.Equal(expectedProperty2, result.Parameters.Skip(1).Take(1).First());

        }

        [Fact]
        public async Task HasParametersIsFalseWhenNoParamterExists()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/19:4b6bed8d24574f6a9e436813cb2617d8");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var result = new SnippetCodeGraph(snippetModel);

            Assert.False(result.HasParameters());
        }

        [Fact]
        public async Task ParsesBodyTypeBinary()
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo")
            {
                Content = new ByteArrayContent(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 })
            };
            request.Content.Headers.ContentType = new("application/octet-stream");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var result = new SnippetCodeGraph(snippetModel);

            Assert.Equal(PropertyType.Binary, result.Body.PropertyType);
        }

        [Fact]
        public async Task ParsesBodyWithoutProperContentType()
        {

            var sampleBody = @"
                {
                    ""createdDateTime"": ""2019-02-04T19:58:15.511Z""
                }
                ";

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/team-id/channels/19:4b6bed8d24574f6a9e436813cb2617d8@thread.tacv2/messages")
            {
                Content = new StringContent(sampleBody, Encoding.UTF8) // snippet missing content type
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var result = new SnippetCodeGraph(snippetModel);

            var expectedObject = new CodeProperty { Name = "MessagesPostRequestBody", Value = null, PropertyType = PropertyType.Object, Children = new List<CodeProperty>() };

            Assert.Equal(expectedObject.Name, result.Body.Name);
            Assert.Equal(expectedObject.Value, result.Body.Value);
            Assert.Equal(expectedObject.PropertyType, result.Body.PropertyType);
        }

        private CodeProperty? findProperyInSnipet(CodeProperty codeProperty, string name)
        {
            if (codeProperty.Name == name) return codeProperty;

            if (codeProperty.Children.Any())
            {
                foreach (var param in codeProperty.Children)
                {
                    if(findProperyInSnipet(param, name) is CodeProperty result) return result;
                }
            }

            return null;
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeString()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            // meetingDuration should be a string
            var property = findProperyInSnipet(snippetCodeGraph.Body, "meetingDuration");

            Assert.NotNull(property);
            Assert.Equal(PropertyType.String, property?.PropertyType);
            Assert.Equal("PT1H", property?.Value);
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeNumber()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = findProperyInSnipet(snippetCodeGraph.Body, "minimumAttendeePercentage");

            Assert.NotNull(property);
            Assert.Equal(PropertyType.Number, property?.PropertyType);
            Assert.Equal("100" , property?.Value);
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeBoolean()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = findProperyInSnipet(snippetCodeGraph.Body, "suggestLocation");

            Assert.NotNull(property);
            Assert.Equal(PropertyType.Boolean, property?.PropertyType);
            Assert.Equal("False", property?.Value);
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeObject()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = findProperyInSnipet(snippetCodeGraph.Body, "locationConstraint");

            Assert.NotNull(property);
            Assert.Equal(PropertyType.Object, property?.PropertyType);
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeArray()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = findProperyInSnipet(snippetCodeGraph.Body, "attendees");

            Assert.NotNull(property);
            Assert.Equal(PropertyType.Array, property?.PropertyType);
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeMap()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1TreeNode());
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = findProperyInSnipet(snippetCodeGraph.Body, "additionalData");

            Assert.NotNull(property);
            Assert.Equal(PropertyType.Map, property?.PropertyType);
        }
    }

}
