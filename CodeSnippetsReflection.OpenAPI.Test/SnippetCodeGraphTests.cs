using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class SnippetCodeGraphTests : OpenApiSnippetGeneratorTestBase
    {
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
        [Fact]
        public async Task SkipParametersAreIntegersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$skip=10");

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = new SnippetCodeGraph(snippetModel);

            Assert.True(result.HasParameters());
            var param = result.Parameters.First();
            Assert.Equal("skip", param.Name);
            Assert.Equal("10", param.Value);
        }

        [Fact]
        public async Task CountParameterIsBooleanAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$select=displayName,id");

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = new SnippetCodeGraph(snippetModel);

            Assert.True(result.HasParameters());
            var paramCount = result.Parameters.Count();
            Assert.Equal(2, paramCount);

            var param = result.Parameters.First();

            Assert.Equal("count", param.Name);
            Assert.Equal(PropertyType.Boolean, param.PropertyType);
            Assert.Equal("true", param.Value);
        }

        [Fact]
        public async Task ArrayParametersSplitOnExternalCommasAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members($select=id,displayName),teams($select=id,displayName)");

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = new SnippetCodeGraph(snippetModel);

            Assert.True(result.HasParameters());
            Assert.Single(result.Parameters);

            var param = result.Parameters.First();

            Assert.Equal("expand", param.Name);
            Assert.Equal(PropertyType.Array, param.PropertyType);
            Assert.Equal(2, param.Children.Count);

            var expectedProperty1 = new CodeProperty { Value = "members($select=id,displayName)", PropertyType = PropertyType.String };
            Assert.Equal(param.Children.First(), expectedProperty1);

            var expectedProperty2 = new CodeProperty { Value = "teams($select=id,displayName)", PropertyType = PropertyType.String };
            Assert.Equal(param.Children.Skip(1).First(), expectedProperty2);
        }

        [Fact]
        public async Task ParsesGuidPropertyAsync()
        {
            const string userJsonObject = "{\r\n  \"keyId\": \"f0b0b335-1d71-4883-8f98-567911bfdca6\"\r\n\r\n}";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/applications/{{id}}/removeKey")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = FindPropertyInSnippet(snippetCodeGraph.Body, "keyId").Value;

            Assert.Equal(PropertyType.Guid, property.PropertyType);
            Assert.Equal("f0b0b335-1d71-4883-8f98-567911bfdca6", property.Value);
        }

        [Fact]
        public async Task ParsesInt32PropertyAsync()
        {
            const string userJsonObject = "{\r\n  \"maxCandidates\": 23\r\n\r\n}";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = FindPropertyInSnippet(snippetCodeGraph.Body, "maxCandidates").Value;

            Assert.Equal(PropertyType.Int32, property.PropertyType);
            Assert.Equal("23", property.Value);
        }

        [Fact]
        public async Task ParsesInt64PropertyAsync()
        {
            const string userJsonObject = "{\r\n  \"chainId\": 10\r\n\r\n}";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/{{teamId}}/sendActivityNotification")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = FindPropertyInSnippet(snippetCodeGraph.Body, "chainId").Value;

            Assert.Equal(PropertyType.Int64, property.PropertyType);
            Assert.Equal("10", property.Value);
        }

        [Fact]
        public async Task ParsesDoublePropertyAsync()
        {
            const string userJsonObject = "{\r\n  \"minimumAttendeePercentage\": 10\r\n\r\n}";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = FindPropertyInSnippet(snippetCodeGraph.Body, "minimumAttendeePercentage").Value;

            Assert.Equal(PropertyType.Double, property.PropertyType);
            Assert.Equal("10", property.Value);
        }

        [Fact]
        public async Task ParsesHeadersAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users");

            request.Headers.Add("Host", "graph.microsoft.com");
            request.Headers.Add("Prefer", "outlook.timezone=\"Pacific Standard Time\"");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var result = new SnippetCodeGraph(snippetModel);
            var header = result.Headers.First();

            Assert.True(result.HasHeaders());
            Assert.Single(result.Headers); // host should be ignored in headers
            Assert.Equal("outlook.timezone=\"Pacific Standard Time\"", header.Value);
            Assert.Equal("Prefer", header.Name);
            Assert.Equal(PropertyType.String, header.PropertyType);
        }

        [Fact]
        public async Task ParsesParametersWithExpressionsCorrectlyAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$filter=groupTypes/any(c:c eq 'Unified')");
            request.Headers.Add("Host", "graph.microsoft.com");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var result = new SnippetCodeGraph(snippetModel);

            Assert.True(result.HasParameters());
            var param = result.Parameters.First();
            Assert.Equal("filter",param.Name);
            Assert.Equal("groupTypes/any(c:c eq 'Unified')",param.Value);
        }
        
        [Fact]
        public async Task ParsesParametersWithExpressionsCorrectlyComplexAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$filter=mailEnabled eq false and securityEnabled eq true and NOT(groupTypes/any(s:s eq 'Unified')) and membershipRuleProcessingState eq 'On'&$count=true&$select=id,membershipRule,membershipRuleProcessingState");
            request.Headers.Add("Host", "graph.microsoft.com");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var result = new SnippetCodeGraph(snippetModel);

            Assert.True(result.HasParameters());
            var parameters = result.Parameters.ToList();
            Assert.Equal("filter",parameters[0].Name);
            Assert.Equal("mailEnabled eq false and securityEnabled eq true and NOT(groupTypes/any(s:s eq 'Unified')) and membershipRuleProcessingState eq 'On'",parameters[0].Value);
            Assert.Equal("count",parameters[1].Name);
            Assert.Equal("true",parameters[1].Value);
            Assert.Equal("select",parameters[2].Name);
            Assert.Equal(PropertyType.Array,parameters[2].PropertyType);
            Assert.Equal("id",parameters[2].Children[0].Value);
            Assert.Equal("membershipRule",parameters[2].Children[1].Value);
            Assert.Equal("membershipRuleProcessingState",parameters[2].Children[2].Value);
        }

        [Fact]
        public async Task HasHeadersIsFalseWhenNoneIsInRequestAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users");
            request.Headers.Add("Host", "graph.microsoft.com");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var result = new SnippetCodeGraph(snippetModel);

            Assert.False(result.HasHeaders());
        }

        [Fact]
        public async Task ParsesParametersAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/19:4b6bed8d24574f6a9e436813cb2617d8?$select=displayName,givenName,postalCode,identities");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var result = new SnippetCodeGraph(snippetModel);
            var parameter = result.Parameters.First();

            Assert.True(result.HasParameters());
            Assert.Single(result.Parameters);

            // creates an array of nested properties
            var children = new List<CodeProperty>();
            children.Add(new CodeProperty { Value = "displayName", PropertyType = PropertyType.String });
            children.Add(new CodeProperty { Value = "givenName", PropertyType = PropertyType.String });
            children.Add(new CodeProperty { Value = "postalCode", PropertyType = PropertyType.String });
            children.Add(new CodeProperty { Value = "identities", PropertyType = PropertyType.String });

            Assert.Equal(children[0], parameter.Children[0]);
            Assert.Equal(children[1], parameter.Children[1]);
            Assert.Equal(children[2], parameter.Children[2]);
            Assert.Equal(children[3], parameter.Children[3]);

            Assert.Equal("select", parameter.Name);
            Assert.Equal(PropertyType.Array, parameter.PropertyType);

        }

        [Fact]
        public async Task ParsesQueryParametersWithSpacesAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/roleManagement/directory/roleAssignments?$filter=roleDefinitionId eq '62e90394-69f5-4237-9190-012177145e10'&$expand=principal");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var result = new SnippetCodeGraph(snippetModel);

            Assert.True(result.HasParameters());
            Assert.Equal(2, result.Parameters.Count());

            var expectedProperty1 = new CodeProperty { Name = "filter" , Value = "roleDefinitionId eq '62e90394-69f5-4237-9190-012177145e10'", PropertyType = PropertyType.String, Children = new List<CodeProperty>()};
            var actualParam1 = result.Parameters.First();

            Assert.Equal(expectedProperty1.Name, actualParam1.Name);
            Assert.Equal(expectedProperty1.Value, actualParam1.Value);
            Assert.Equal(expectedProperty1.PropertyType, actualParam1.PropertyType);
            Assert.Equal(expectedProperty1.Children.Count, actualParam1.Children.Count);

            var expectedProperty2 = new CodeProperty { Value = "principal", PropertyType = PropertyType.String};
            var actualParam2 = result.Parameters.Skip(1).First();

            Assert.Equal("expand", actualParam2.Name);
            Assert.Equal(expectedProperty2, actualParam2.Children[0]);
        }

        [Fact]
        public async Task HasParametersIsFalseWhenNoParameterExistsAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/19:4b6bed8d24574f6a9e436813cb2617d8");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var result = new SnippetCodeGraph(snippetModel);

            Assert.False(result.HasParameters());
        }

        [Fact]
        public async Task ParsesBodyTypeBinaryAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo")
            {
                Content = new ByteArrayContent(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 })
            };
            request.Content.Headers.ContentType = new("application/octet-stream");

            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var result = new SnippetCodeGraph(snippetModel);

            Assert.Equal(PropertyType.Binary, result.Body.PropertyType);
        }

        [Fact]
        public async Task ParsesBodyWithoutProperContentTypeAsync()
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
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var result = new SnippetCodeGraph(snippetModel);

            var expectedObject = new CodeProperty { Name = "MessagesPostRequestBody", Value = null, PropertyType = PropertyType.Object, Children = new List<CodeProperty>() };

            Assert.Equal(expectedObject.Name, result.Body.Name);
            Assert.Equal(expectedObject.Value, result.Body.Value);
            Assert.Equal(expectedObject.PropertyType, result.Body.PropertyType);
        }

        private CodeProperty? FindPropertyInSnippet(CodeProperty codeProperty, string name)
        {
            if (codeProperty.Name == name) return codeProperty;

            if (codeProperty.Children is null || codeProperty.Children.Count == 0) return null;
            foreach (var param in codeProperty.Children)
            {
                if(FindPropertyInSnippet(param, name) is CodeProperty result) return result;
            }

            return null;
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeStringAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            // meetingDuration should be a string
            var property = FindPropertyInSnippet(snippetCodeGraph.Body, "meetingDuration").Value;

            Assert.Equal(PropertyType.String, property.PropertyType);
            Assert.Equal("PT1H", property.Value);
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeNumberAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = FindPropertyInSnippet(snippetCodeGraph.Body, "minimumAttendeePercentage").Value;

            Assert.Equal(PropertyType.Int32, property.PropertyType);
            Assert.Equal("100" , property.Value);
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeBooleanAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = FindPropertyInSnippet(snippetCodeGraph.Body, "suggestLocation").Value;

            Assert.Equal(PropertyType.Boolean, property.PropertyType);
            Assert.Equal("False", property.Value);
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeObjectAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = FindPropertyInSnippet(snippetCodeGraph.Body, "locationConstraint").Value;

            Assert.Equal(PropertyType.Object, property.PropertyType);
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeArrayAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = FindPropertyInSnippet(snippetCodeGraph.Body, "attendees").Value;

            Assert.Equal(PropertyType.Array, property.PropertyType);
        }

        [Fact]
        public async Task ParsesBodyPropertyTypeMapAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(TypesSample, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(request, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(request);
            var snippetCodeGraph = new SnippetCodeGraph(snippetModel);

            var property = FindPropertyInSnippet(snippetCodeGraph.Body, "additionalData").Value;

            Assert.Equal(PropertyType.Map, property.PropertyType);
        }
    }

}
