using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test;

public class PythonGeneratorTests : OpenApiSnippetGeneratorTestBase
{
    private readonly PythonGenerator _generator = new();

    [Fact]
    public async Task GeneratesTheCorrectFluentApiPathAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(".me.messages.get()", result);
    }
    [Fact]
    public async Task GeneratesTheCorrectFluentApiPathForIndexedCollectionsAsync()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}"); 
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(".me.messages.by_message_id('message-id').get()", result);
    }
    [Fact]
    public async Task GeneratesTheCorrectSnippetForUsersAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(".me.get()", result);
    }
    [Fact]
    public async Task GeneratesTheSnippetHeaderAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("# To initialize your graph_client, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=python", result);
    }
    [Fact]
    public async Task GeneratesThePostMethodCallAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("post(None)", result);
    }
    [Fact]
    public async Task GeneratesThePatchMethodCallAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{message-id}}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("patch(None)", result);
    }
    [Fact]
    public async Task GeneratesThePutMethodCallAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("put()", result);
    }
    [Fact]
    public async Task GeneratesTheDeleteMethodCallAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{message-id}}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("delete()", result);
        Assert.DoesNotContain("result =", result);
    }
    [Fact]
    public async Task WritesTheRequestPayloadAsync()
    {
        const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                        "\"displayName\": \"displayName-value\",\r\n  " +
                                        "\"mailNickname\": \"mailNickname-value\",\r\n  " +
                                        "\"userPrincipalName\": \"upn-value@tenant-value.onmicrosoft.com\",\r\n " +
                                        " \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"password-value\"\r\n  }\r\n}";//nested passwordProfile Object

        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
        {
            Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = User(", result);
        Assert.Contains("account_enabled = True", result);
        Assert.Contains("display_name = \"displayName-value\"", result);
        Assert.Contains("password_profile = PasswordProfile(", result);
        Assert.Contains("force_change_password_next_sign_in = True", result);
        Assert.Contains("password = \"password-value\"", result);
    }
    [Fact]
    public async Task WritesAnIntAndFindsAnActionAsync()
    {
        const string userJsonObject = "{\r\n  \"chainId\": 10\r\n\r\n}";

        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/{{team-id}}/sendActivityNotification")
        {
            Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = SendActivityNotificationPostRequestBody(", result);
        Assert.Contains("chain_id = 10", result);
        Assert.DoesNotContain("microsoft.graph", result);
    }
    [Fact]
    public async Task GeneratesABinaryPayloadAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo") {
            Content = new ByteArrayContent(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 })
        };
        requestPayload.Content.Headers.ContentType = new ("application/octet-stream");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = BytesIO()", result);
    }
    [Fact]
    public async Task GeneratesABase64UrlPayloadAsync() {
        const string userJsonObject = "{\r\n  \"contentBytes\": \"wiubviuwbegviwubiu\"\r\n\r\n}";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/chats/{{chat-id}}/messages/{{chatMessage-id}}/hostedContents") {
            Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = ChatMessageHostedContent(", result);
        Assert.Contains("content_bytes = base64.urlsafe_b64decode(\"wiubviuwbegviwubiu\")", result);
    }
    [Fact]
    public async Task GeneratesADateTimePayloadAsync()
    {
        const string userJsonObject = "{\r\n  \"receivedDateTime\": \"2021-08-30T20:00:00:00Z\"\r\n\r\n}";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages") {
            Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = Message(", result);
        Assert.Contains("received_date_time = \"2021-08-30T20:00:00:00Z\"", result);
    }
    [Fact]
    public async Task GeneratesAnArrayPayloadInAdditionalDataAsync()
    {
        const string userJsonObject = "{\r\n  \"members@odata.bind\": [\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\"\r\n    ]\r\n}";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/groups/{{group-id}}") {
            Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = Group(", result);
        Assert.Contains("additional_data = {", result);
        Assert.Contains("\"members@odata_bind\" : [", result);
        Assert.Contains("\"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",", result);
    }
    [Fact]
    public async Task GeneratesSelectQueryParametersAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me?$select=displayName,id");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("RequestBuilderGetQueryParameters(", result);
        Assert.Contains("select = [\"displayName\",\"id\"]", result);
        Assert.Contains("RequestConfiguration(", result);
        Assert.Contains("query_parameters = query_params,", result);
    }
    [Fact]
    public async Task GeneratesCountBooleanQueryParametersAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$select=displayName,id");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("query_params = UsersRequestBuilder.UsersRequestBuilderGetQueryParameters(", result);
        Assert.Contains("count = True", result);
        Assert.Contains("select = [\"displayName\",\"id\"]", result);
        Assert.DoesNotContain("\"true\"", result);
        Assert.Contains("RequestConfiguration(", result);
        Assert.Contains("query_parameters = query_params,", result);
    }
    [Fact]
    public async Task GeneratesSkipQueryParametersAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$skip=10");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.DoesNotContain("\"10\"", result);
        Assert.Contains("skip = 10", result);
    }
    [Fact]
    public async Task GeneratesSelectExpandQueryParametersAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members($select=id,displayName)");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("expand =", result);
        Assert.Contains("members($select=id,displayName)", result);
        Assert.DoesNotContain("Select", result);
    }
    [Fact]
    public async Task GeneratesRequestHeadersAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups");
        requestPayload.Headers.Add("ConsistencyLevel", "eventual");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_configuration = RequestConfiguration()", result);
        Assert.Contains("from kiota_abstractions.base_request_configuration import RequestConfiguration", result);
        Assert.Contains("request_configuration.headers.add(\"ConsistencyLevel\", \"eventual\")", result);
    }
    [Fact]
    public async Task GeneratesFilterParametersAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$filter=Department eq 'Finance'&$orderBy=displayName&$select=id,displayName,department");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("RequestBuilderGetQueryParameters(", result);
        Assert.Contains("count = True,", result);
        Assert.Contains("filter = \"Department eq 'Finance'\",", result);
        Assert.Contains("orderby = [\"displayName\"],", result);
        Assert.Contains("select = [\"id\",\"displayName\",\"department\"],", result);
    }
    [Fact]
    public async Task GeneratesFilterParametersWithSpecialCharactersAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$filter=imAddresses/any(i:i eq 'admin@contoso.com')");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("RequestBuilderGetQueryParameters(", result);
        Assert.Contains("filter = \"imAddresses/any(i:i eq 'admin@contoso.com')\",", result);
    }
    [Fact]
    public async Task GeneratesSnippetForRequestWithDeltaAndSkipTokenAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/calendarView/delta?$skiptoken=R0usmcCM996atia_s");
        requestPayload.Headers.Add("Prefer", "odata.maxpagesize=2");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("DeltaRequestBuilderGetQueryParameters(", result);
        Assert.Contains("skiptoken = \"R0usmcCM996atia_s\",", result);
        Assert.Contains("request_configuration = RequestConfiguration(", result);
        Assert.Contains("from kiota_abstractions.base_request_configuration import RequestConfiguration", result);
        Assert.Contains("query_parameters = query_params,", result);
        Assert.Contains("request_configuration.headers.add(\"Prefer\", \"odata.maxpagesize=2\")", result);
        Assert.Contains("result = await graph_client.me.calendar_view.delta.get", result);
    }
    [Fact]
    public async Task GeneratesSnippetForRequestWithSearchQueryOptionWithANDLogicalConjunctionAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$search=\"displayName:di\" AND \"displayName:al\"");
        requestPayload.Headers.Add("ConsistencyLevel", "eventual");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("RequestBuilderGetQueryParameters(", result);
        Assert.Contains("search = \"\\\"displayName:di\\\" AND \\\"displayName:al\\\"\"", result);
        Assert.Contains("request_configuration.headers.add(\"ConsistencyLevel\", \"eventual\")", result);
    }
    [Fact]
    public async Task HandlesOdataTypeWhenGeneratingAsync()
    {
        var sampleJson = @"
            {
            ""@odata.type"": ""#microsoft.graph.socialIdentityProvider"",
            ""displayName"": ""Login with Amazon"",
            ""identityProviderType"": ""Amazon"",
            ""clientId"": ""56433757-cadd-4135-8431-2c9e3fd68ae8"",
            ""clientSecret"": ""000000000000""
            }
        ";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/identity/identityProviders"){
            Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = SocialIdentityProvider(", result);// ensure the derived type is used
        Assert.Contains("odata_type = \"#microsoft.graph.socialIdentityProvider\",", result);
    }
    [Fact]
    public async Task HandlesOdataReferenceSegmentsInUrlAsync()
    {
        var sampleJson = @"
            {
            ""@odata.id"": ""https://graph.microsoft.com/beta/users/alexd@contoso.com""
            }
        ";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/groups/id/acceptedSenders/$ref"){
            Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = ReferenceCreate(", result);
        Assert.Contains("odata_id = \"https://graph.microsoft.com/beta/users/alexd@contoso.com\"", result);
        Assert.Contains(".accepted_senders.ref.post(request_body)", result);
    }
    [Fact]
    public async Task GenerateSnippetsWithArrayNestingAsync()
    {
        var eventData = @"
            {
            ""subject"": ""Let's go for lunch"",
            ""body"": {
                ""contentType"": ""Html"",
                ""content"": ""Does noon work for you?""
            },
            ""start"": {
                ""dateTime"": ""2017-04-15T12:00:00"",
                ""timeZone"": ""Pacific Standard Time""
            },
            ""end"": {
                ""dateTime"": ""2017-04-15T14:00:00"",
                ""timeZone"": ""Pacific Standard Time""
            },
            ""location"":{
                ""displayName"": null
            },
            ""attendees"": [
            {
                ""emailAddress"": {
                    ""address"":""samanthab@contoso.onmicrosoft.com"",
                    ""name"": ""Samantha Booth""
                },
                ""type"": ""required""
            },
            {
                ""emailAddress"": {
                                ""address"":""ss@contoso.com"", ""name"": ""Sorry Sir""}, ""type"":""Optional""
            }
            ],
            ""allowNewTimeProposals"": true,
            ""transactionId"":""7E163156-7762-4BEB-A1C6-729EA81755A7""
        }";

        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/events")
        {
            Content = new StringContent(eventData, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("subject = \"Let's go for lunch\",", result);
        Assert.Contains("content_type = BodyType.Html", result);
        Assert.Contains("date_time = \"2017-04-15T12:00:00\"", result);
        Assert.Contains("display_name = None", result);
        Assert.Contains("attendees = [", result);
        Assert.Contains("address = \"samanthab@contoso.onmicrosoft.com\"", result);
        Assert.Contains("type = AttendeeType.Required", result);
        Assert.Contains("type = AttendeeType.Optional", result);
    }
    [Fact]
    public async Task GenerateFindMeetingTimeAsync()
    {
        var bodyContent = @"
        {
            ""attendees"": [
                {
                    ""emailAddress"": {
                        ""address"": ""{user-mail}"",
                        ""name"": ""Alex Darrow""
                    },
                    ""type"": ""Required""
                }
                ],
                ""timeConstraint"": {
                    ""timeSlots"": [
                    {
                        ""start"": {
                            ""dateTime"": ""2022-07-18T13:24:57.384Z"",
                            ""timeZone"": ""Pacific Standard Time""
                        },
                        ""end"": {
                            ""dateTime"": ""2022-07-25T13:24:57.384Z"",
                            ""timeZone"": ""Pacific Standard Time""
                        }
                    }
                    ]
                },
                ""locationConstraint"": {
                    ""isRequired"": ""false"",
                    ""suggestLocation"": ""true"",
                    ""locations"": [
                    {
                        ""displayName"": ""Conf Room 32/1368"",
                        ""locationEmailAddress"": ""conf32room1368@imgeek.onmicrosoft.com""
                    }
                    ]
                },
                ""meetingDuration"": ""PT1H""
        }";

        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("meeting_duration = \"PT1H\"", result);
        Assert.Contains("is_required = False,", result);
        Assert.Contains("location_email_address = \"conf32room1368@imgeek.onmicrosoft.com\",", result);
    }
    [Theory]
    [InlineData("sendMail")]
    [InlineData("microsoft.graph.sendMail")]
    public async Task FullyQualifiesActionRequestBodyTypeAsync(string sendMailString)
    {
        var bodyContent = @"{
                ""message"": {
                ""subject"": ""Meet for lunch?"",
                ""body"": {
                    ""contentType"": ""Text"",
                    ""content"": ""The new cafeteria is open.""
                },
                ""toRecipients"": [
                {
                    ""emailAddress"": {
                        ""address"": ""fannyd@contoso.onmicrosoft.com""
                    }
                }
                ],
                ""ccRecipients"": [
                {
                    ""emailAddress"": {
                        ""address"": ""danas@contoso.onmicrosoft.com""
                    }
                }
                ]
            },
            ""saveToSentItems"": ""false""
        }";

        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users/{{id}}/{sendMailString}")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("graph_client.users.by_user_id('user-id').send_mail.post(request_body)", result);
        Assert.Contains("request_body = SendMailPostRequestBody(", result);
        Assert.Contains("to_recipients = [", result);
        Assert.Contains("],", result);
    }
    [Fact]
    public async Task TypeArgumentsForListArePlacedCorrectlyAsync()
    {
        var bodyContent = @"{
            ""businessPhones"": [
                ""+1 425 555 0109""
                    ],
                ""officeLocation"": ""18/2111""
            }";

        using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/users/{{id}}")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("business_phones = [", result);
        Assert.Contains("+1 425 555 0109", result);        
    }
    [Fact]
    public async Task ModelsInNestedNamespacesAreDisambiguatedAsync()
    {
        var bodyContent = @"{
            ""id"": ""1431b9c38ee647f6a"",
            ""type"": ""externalGroup"",
        }";

        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/external/connections/contosohr/groups/31bea3d537902000/members")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("request_body = Identity(", result);
        Assert.Contains("type = IdentityType.ExternalGroup,", result);
    }    
    [Fact]
    public async Task CorrectlyGeneratesEnumMemberAsync()
    {
        var bodyContent = @"{
            ""id"": ""SHPR_eeab4fb1-20e5-48ca-ad9b-98119d94bee7"",
            ""@odata.etag"": ""1a371e53-f0a6-4327-a1ee-e3c56e4b38aa"",
            ""availability"": [
            {
                ""recurrence"": {
                    ""pattern"": {
                        ""type"": ""Weekly"",
                        ""interval"": 1
                    },
                    ""range"": {
                        ""type"": ""noEnd""
                    }
                },
                ""timeZone"": ""Pacific Standard Time"",
                ""timeSlots"": null
            }
            ]
        }";

        using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/users/871dbd5c-3a6a-4392-bfe1-042452793a50/settings/shiftPreferences")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("type = RecurrencePatternType.Weekly,", result);
        Assert.Contains("type = RecurrenceRangeType.NoEnd,", result);
    }
    [Fact]
    public async Task CorrectlyGeneratesMultipleFlagsEnumMembersAsync()
    {
        var bodyContent = @"{
            ""clientContext"": ""clientContext-value"",
            ""status"": ""notRecorDing | recording , failed""
        }";

        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/communications/calls/{{id}}/updateRecordingStatus")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("client_context = \"clientContext-value\",", result);
        Assert.Contains("status = RecordingStatus.NotRecording | RecordingStatus.Recording | RecordingStatus.Failed", result);
    }
    [Fact]
    public async Task CorrectlyOptionalRequestBodyParameterAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/{{id}}/archive");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("await graph_client.teams.by_team_id('team-id').archive.post(None)", result);
    }
    [Fact]
    public async Task CorrectlyEvaluatesDatePropertyTypeRequestBodyParameterAsync()
    {
        var bodyContent = @"{
            ""subject"": ""Let's go for lunch"",
            ""recurrence"": {
                ""range"": {
                    ""type"": ""endDate"",
                    ""startDate"": ""2017-09-04"",
                    ""endDate"": ""2017-12-31""
                }
            }
        }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/events")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("start_date = \"2017-09-04\",", result);
        Assert.Contains("end_date = \"2017-12-31\",", result);
    }
    [Fact]
    public async Task CorrectlyEvaluatesOdataActionRequestBodyParameterAsync()
    {
        var bodyContent = @"{
            ""keyCredential"": {
                    ""type"": ""AsymmetricX509Cert"",
                    ""usage"": ""Verify"",
                    ""key"": ""MIIDYDCCAki...""
                },
                ""passwordCredential"": null,
                ""proof"":""eyJ0eXAiOiJ...""
            }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/applications/{{id}}/addKey")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("request_body = AddKeyPostRequestBody(", result);
        Assert.Contains("key_credential = KeyCredential(", result);
    }
    [Fact]
    public async Task CorrectlyEvaluatesGuidInRequestBodyParameterAsync()
    {
        var bodyContent = @"{
                ""principalId"": ""cde330e5-2150-4c11-9c5b-14bfdc948c79"",
                ""resourceId"": ""8e881353-1735-45af-af21-ee1344582a4d"",
                ""appRoleId"": ""00000000-0000-0000-0000-000000000000""
            }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users/{{id}}/appRoleAssignments")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("UUID(\"cde330e5-2150-4c11-9c5b-14bfdc948c79\")", result);
        Assert.Contains("UUID(\"8e881353-1735-45af-af21-ee1344582a4d\")", result);
        Assert.Contains("UUID(\"00000000-0000-0000-0000-000000000000\")", result);
    }
    [Fact]
    public async Task DefaultsEnumIfNoneProvidedAsync()
    {
        var bodyContent = @"{
            ""subject"": ""subject-value"",
            ""body"": {
            ""contentType"": """",
            ""content"": ""content-value""
            },
            ""inferenceClassification"": ""other""
        }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{id}}")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("content_type = BodyType.Text,", result);
    }
    [Fact]
    public async Task HandlesEmptyCollectionAsync()
    {
        var bodyContent = @"{
            ""defaultUserRolePermissions"": {
            ""permissionGrantPoliciesAssigned"": []
            }
        }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/policies/authorizationPolicy")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("permission_grant_policies_assigned = [", result);
    }
    [Fact]
    public async Task CorrectlyHandlesOdataFunctionAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/delta?$select=displayName,jobTitle,mobilePhone");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("await graph_client.users.delta.get(request_configuration = request_configuration)", result);
        Assert.Contains("query_params = DeltaRequestBuilder.DeltaRequestBuilderGetQueryParameters(", result);
        Assert.Contains("select = [\"displayName\",\"jobTitle\",\"mobilePhone\"]", result);
        Assert.Contains("request_configuration = RequestConfiguration(", result);
    } 
    [Fact]
    public async Task CorrectlyHandlesDateTimeOffsetInUrlAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/reports/getUserArchivedPrintJobs(userId='{{id}}',startDateTime=<timestamp>,endDateTime=<timestamp>)");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("await graph_client.reports.get_user_archived_print_jobs_with_user_id_with_start_date_time_with_end_date_time(\"{endDateTime}\",\"{startDateTime}\",\"{userId}\").get()", result);
    }
    [Fact]
    public async Task CorrectlyHandlesNumberInUrlAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/drive/items/{{id}}/workbook/worksheets/{{id|name}}/cell(row=<row>,column=<column>)");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains(" await graph_client.drives.by_drive_id('drive-id').items.by_drive_item_id('driveItem-id').workbook.worksheets.by_workbook_worksheet_id('workbookWorksheet-id').cell_with_row_with_column(1,1).get()",result);
    }
    [Fact]
    public async Task CorrectlyHandlesDateInUrlAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/reports/getYammerGroupsActivityDetail(date='2018-03-05')");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("await graph_client.reports.get_yammer_groups_activity_detail_with_date(\"{date}\").get()", result);
    }
    [Fact]
    public async Task CorrectlyHandlesDateInUrl2Async()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/communications/callRecords/getPstnCalls(fromDateTime=2019-11-01,toDateTime=2019-12-01)");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("await graph_client.communications.call_records.microsoft_graph_call_records_get_pstn_calls_with_from_date_time_with_to_date_time(\"{fromDateTime}\",\"{toDateTime}\").get()", result);
    }
    [Fact]
    public async Task CorrectlyHandlesEnumInUrlAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/identityGovernance/appConsent/appConsentRequests/filterByCurrentUser(on='reviewer')?$filter=userConsentRequests/any(u:u/status eq 'InProgress')");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("query_params = FilterByCurrentUserWithOnRequestBuilder.FilterByCurrentUserWithOnRequestBuilderGetQueryParameters(", result);
        Assert.Contains("filter = \"userConsentRequests/any(u:u/status eq 'InProgress')\",", result);
        Assert.Contains("result = await graph_client.identity_governance.app_consent.app_consent_requests.filter_by_current_user_with_on(\"reviewer\").get(request_configuration = request_configuration)", result);
    }
    [Fact]
    public async Task GeneratesObjectsInArrayAsync() {
        var sampleJson = @"
        {
        ""addLicenses"": [
            {
            ""disabledPlans"": [ ""11b0131d-43c8-4bbb-b2c8-e80f9a50834a"" ],
            ""skuId"": ""45715bb8-13f9-4bf6-927f-ef96c102d394""
            }
        ],
        ""removeLicenses"": [ ""bea13e0c-3828-4daa-a392-28af7ff61a0f"" ]
        }
        ";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/assignLicense"){
            Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = AssignLicensePostRequestBody(", result);
        Assert.Contains("disabled_plans = [", result);
        Assert.Contains("remove_licenses = [", result);
        Assert.Contains("UUID(\"bea13e0c-3828-4daa-a392-28af7ff61a0f\"),", result);
    }
    [Fact]
    public async Task GeneratesCorrectCollectionTypeAndDerivedInstancesAsync() {
        var sampleJson = @"{
            ""message"": {
            ""subject"": ""Meet for lunch?"",
            ""body"": {
                ""contentType"": ""Text"",
                ""content"": ""The new cafeteria is open.""
            },
            ""toRecipients"": [
                {
                ""emailAddress"": {
                    ""address"": ""meganb@contoso.onmicrosoft.com""
                }
                }
            ],
            ""attachments"": [
                {
                ""@odata.type"": ""#microsoft.graph.fileAttachment"",
                ""name"": ""attachment.txt"",
                ""contentType"": ""text/plain"",
                ""contentBytes"": ""SGVsbG8gV29ybGQh""
                }
            ]
            }
        }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/sendMail"){
            Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = SendMailPostRequestBody(", result);
        Assert.Contains("attachments = [", result);// Collection defines Base type
        Assert.Contains("FileAttachment(", result);// Individual items are derived types
        Assert.Contains("content_bytes = base64.urlsafe_b64decode(\"SGVsbG8gV29ybGQh\"),", result);
    }
    [Fact]
    public async Task GeneratesPropertiesWithSpecialCharactersAsync() {
        var sampleJson = @"{
            ""@odata.type"": ""#microsoft.graph.managedIOSLobApp"",
            ""displayName"": ""Display Name value"",
            ""description"": ""Description value"",
            ""publisher"": ""Publisher value"",
            ""largeIcon"": {
            ""@odata.type"": ""microsoft.graph.mimeContent"",
            ""type"": ""Type value"",
            ""value"": ""dmFsdWU=""
            },
            ""isFeatured"": true,
            ""privacyInformationUrl"": ""https://example.com/privacyInformationUrl/"",
            ""informationUrl"": ""https://example.com/informationUrl/"",
            ""owner"": ""Owner value"",
            ""developer"": ""Developer value"",
            ""notes"": ""Notes value"",
            ""uploadState"": 11,
            ""publishingState"": ""processing"",
            ""isAssigned"": true,
            ""roleScopeTagIds"": [
            ""Role Scope Tag Ids value""
            ],
            ""dependentAppCount"": 1,
            ""supersedingAppCount"": 3,
            ""supersededAppCount"": 2,
            ""appAvailability"": ""lineOfBusiness"",
            ""version"": ""Version value"",
            ""committedContentVersion"": ""Committed Content Version value"",
            ""fileName"": ""File Name value"",
            ""size"": 4,
            ""bundleId"": ""Bundle Id value"",
            ""applicableDeviceType"": {
            ""@odata.type"": ""microsoft.graph.iosDeviceType"",
            ""iPad"": true,
            ""iPhoneAndIPod"": true
            },
            ""minimumSupportedOperatingSystem"": {
            ""@odata.type"": ""microsoft.graph.iosMinimumOperatingSystem"",
            ""v8_0"": true,
            ""v9_0"": true,
            ""v10_0"": true,
            ""v11_0"": true,
            ""v12_0"": true,
            ""v13_0"": true,
            ""v14_0"": true,
            ""v15_0"": true,
            ""v16_0"": true
            },
            ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-08:00"",
            ""versionNumber"": ""Version Number value"",
            ""buildNumber"": ""Build Number value"",
            ""identityVersion"": ""Identity Version value""
        }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootBetaUrl}/deviceAppManagement/mobileApps/{{mobileAppId}}"){
            Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = ManagedIOSLobApp(", result);
        Assert.Contains("odata_type = \"#microsoft.graph.managedIOSLobApp\",", result);
        Assert.Contains("value = base64.urlsafe_b64decode(\"dmFsdWU=\"),", result);
        Assert.Contains("v8_0 = True,", result);
        Assert.Contains("\"v16_0\" : True,", result);
    }
    [Fact]
    public async Task GeneratesCorrectTypeInCollectionInitializerAsync() {
        var sampleJson = @"{
            ""workflow"":{
                ""category"": ""joiner"",
                ""description"": ""Configure new hire tasks for onboarding employees on their first day"",
                ""displayName"": ""Global onboard new hire employee"",
                ""isEnabled"": true,
                ""isSchedulingEnabled"": false,
                ""executionConditions"": {
                    ""@odata.type"": ""#microsoft.graph.identityGovernance.triggerAndScopeBasedConditions"",
                    ""scope"": {
                        ""@odata.type"": ""#microsoft.graph.identityGovernance.ruleBasedSubjectSet"",
                        ""rule"": ""(department eq 'Marketing')""
                    },
                    ""trigger"": {
                        ""@odata.type"": ""#microsoft.graph.identityGovernance.timeBasedAttributeTrigger"",
                        ""timeBasedAttribute"": ""employeeHireDate"",
                        ""offsetInDays"": 1
                    }
                },
                ""tasks"": [
                    {
                        ""continueOnError"": false,
                        ""description"": ""Enable user account in the directory"",
                        ""displayName"": ""Enable User Account"",
                        ""isEnabled"": true,
                        ""taskDefinitionId"": ""6fc52c9d-398b-4305-9763-15f42c1676fc"",
                        ""arguments"": []
                    },
                    {
                        ""continueOnError"": false,
                        ""description"": ""Send welcome email to new hire"",
                        ""displayName"": ""Send Welcome Email"",
                        ""isEnabled"": true,
                        ""taskDefinitionId"": ""70b29d51-b59a-4773-9280-8841dfd3f2ea"",
                        ""arguments"": []
                    }
                ]
            }
        }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/identityGovernance/lifecycleWorkflows/workflows/{{workflowId}}/createNewVersion"){
            Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = CreateNewVersionPostRequestBody(", result);
        Assert.Contains("category = LifecycleWorkflowCategory.Joiner,", result);
        Assert.Contains("scope = RuleBasedSubjectSet(", result);
        Assert.Contains("tasks = [", result);
        Assert.Contains("Task(", result);
        Assert.Contains("from msgraph.generated.models.lifecycle_workflow_category import LifecycleWorkflowCategory", result);
    } 
    [Fact]
    public async Task CorrectlyHandlesTypeFromInUrlAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/mailFolders/?includehiddenfolders=true");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("query_params = MailFoldersRequestBuilder.MailFoldersRequestBuilderGetQueryParameters(", result);
        Assert.Contains("include_hidden_folders = \"true\"", result);
        Assert.Contains("request_configuration = RequestConfiguration(", result);
    }
    [Fact]
    public async Task MatchesPathWithPathParameterAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/drive/items/{{id}}/workbook/worksheets/{{id|name}}/range(address='A1:B2')");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("result = await graph_client.drives.by_drive_id('drive-id').items.by_drive_item_id('driveItem-id').workbook.worksheets.by_workbook_worksheet_id('workbookWorksheet-id').range_with_address(\"{address}\").get()", result);
    }   
    [Fact]
    public async Task MatchesPathAlternateKeysAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/applications(appId='46e6adf4-a9cf-4b60-9390-0ba6fb00bf6b')?$select=id,appId,displayName,requiredResourceAccess");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("result = await graph_client.applications_with_app_id(\"{appId}\").get(request_configuration = request_configuration)", result);
    }      
    [Fact]
    public async Task GeneratesCorrectLongPathsAsync()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/{{user-id}}/messages"); 
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("users.by_user_id('user-id').messages.get()", result);
    }
    [Fact]
    public async Task GeneratesObjectInitializationWithCallToSettersAsync()
    {
        const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                      "\"displayName\": \"displayName-value\",\r\n  " +
                                      "\"otherField\": \"NotField\",\r\n  " +
                                      "\"mailNickname\": \"mailNickname-value\",\r\n  " +
                                      "\"userPrincipalName\": \"upn-value@tenant-value.onmicrosoft.com\",\r\n " +
                                      " \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"password-value\"\r\n, \"added\": \"somethingWeird\" }\r\n, \"papa\": [3,4,5,6, true, false, \"yoda\"]}";

        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users/?$select=displayName,mailNickName")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("select = [\"displayName\",\"mailNickName\"],", result);
        Assert.Contains("account_enabled = True", result);
        Assert.Contains("from msgraph import GraphServiceClient", result);
    }
    [Fact]
    public async Task IncludesRequestBodyClassNameAsync()
    {
        const string payloadBody =
            "{\r\n  \"passwordCredential\": {\r\n    \"displayName\": \"Password friendly name\"\r\n  }\r\n}";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}/applications/{{id}}/addPassword")
            {
                Content = new StringContent(payloadBody, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = AddPasswordPostRequestBody(", result);
        Assert.Contains("from msgraph_beta.generated.models.password_credential import PasswordCredential", result);
        Assert.Contains("from msgraph_beta import GraphServiceClient", result);
    }
    [Fact]
    public async Task FindsPathItemsWithDifferentCasingAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/directory/deleteditems/microsoft.graph.group");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(".directory.deleted_items.graph_group.get()", result);
    }
    [Fact]
    public async Task DoesntFailOnTerminalSlashAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/me/messages/AAMkADYAAAImV_jAAA=/?$expand=microsoft.graph.eventMessage/event");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(
            ".me.messages.by_message_id('message-id').get(request_configuration = request_configuration)",
            result);
    }
    [Fact]
    public async Task GeneratesComplicatedObjectsWithNestingAsync()
    {
        const string userJsonObject =
            "{\r\n  \"message\": {\r\n    " +
            "\"subject\": \"Meet for lunch?\",\r\n    " +
            "\"body\": {\r\n      \"contentType\": \"Text\",\r\n      " +
            "\"content\": \"The new cafeteria is open.\"\r\n    },\r\n    " +
            "\"toRecipients\": [\r\n      {\r\n        " +
            "\"emailAddress\": {\r\n         " +
            " \"address\": \"fannyd@contoso.onmicrosoft.com\"\r\n        }\r\n      },\r\n        {\r\n        " +
            "\"emailAddress\": {\r\n                      \"address\": \"jose@con'stoso.onmicrosoft.com\"\r\n        }\r\n      }\r\n    ],\r\n   " +
            " \"ccRecipients\": [\r\n      {\r\n        \"emailAddress\": {\r\n          " +
            "\"address\": null\r\n        }\r\n      }\r\n    ]\r\n," +
            "\"categories\": [\"one\", \"category\", \"away\", null]  },\r\n  \"saveToSentItems\": false\r\n}";

            using var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/sendMail")
                {
                    Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("cc_recipients = [", result);
            Assert.Contains("Recipient(", result);
            Assert.Contains("body = ItemBody(", result);
    }
    [Fact]
    public async Task GeneratesDeleteRequestAsync()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{id}}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(".me.messages.by_message_id('message-id').delete()", result);

    }
    [Fact]
    public async Task GenerateForRequestBodyCornerCaseAsync()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages/{{id}}/createReply")
            {
                Content = new StringContent("{\"field\":\"Nothinkg to be done\"}", Encoding.UTF8, "application/json")
            };
       var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
       await snippetModel.InitializeModelAsync(requestPayload);
       var result = _generator.GenerateCodeSnippet(snippetModel);
       Assert.Contains("request_body = CreateReplyPostRequestBody(", result);
    }
    [Fact]
    public async Task GenerateForRefRequestsAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/applications/{{application-id}}/tokenIssuancePolicies/$ref");

        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains(".token_issuance_policies.ref.get()", result);
    }
    [Fact]
    public async Task GenerateForPostBodyWithEnumsAsync()
    {
        var body = "{\"state\": \"active\", \"serviceIdentifier\":\"id\", \"catalogId\":\"id\"}";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}/users/{{user%2Did}}/usageRights")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = UsageRight(", result);
        Assert.Contains("state = UsageRightState.Active,", result);
    }
    [Fact]
    public async Task GenerateComplexBodyNameAsync()
    {
        var url = "/devices/{id}/registeredUsers/$ref";
        
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent("{\"field\":\"Nothing to be done\"}", Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = ReferenceCreate(", result); 
        Assert.Contains("additional_data = {", result); 
        Assert.Contains("\"field\" : \"Nothing to be done\"", result); 

    }
    [Fact]
    public async Task GenerateFluentApiPathCornerCaseAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/activities/recent");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(".me.activities.recent.get()", result);
    }
    [Fact]
    public async Task GenerateWithODataTypeAndODataIdAsync()
    {
        var url = "/communications/calls/{id}/answer";
        var bodyContent = @"
            {
              ""callbackUri"": ""callbackUri-value"",
                    ""mediaConfig"": {
                        ""@odata.type"": ""#microsoft.graph.appHostedMediaConfig"",
                        ""blob"": ""<Media Session Configuration Blob>""
                    },
                    ""acceptedModalities"": [
                    ""audio""
                        ],
                    ""callOptions"": {
                        ""@odata.type"": ""#microsoft.graph.incomingCallOptions"",
                        ""isContentSharingNotificationEnabled"": true
                    },
                    ""participantCapacity"": 200
                }
            ";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}{url}")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("media_config = AppHostedMediaConfig(", result);
        Assert.Contains("odata_type = \"#microsoft.graph.appHostedMediaConfig\",", result);
        Assert.Contains("odata_type = \"#microsoft.graph.incomingCallOptions\",", result);
    }
    [Fact]
    public async Task GenerateWithValidRequestBodyAsync()
    {
        var url = "/groups/{id}/acceptedSenders/$ref";
        
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent("{\"@odata.id\":\"https://graph.microsoft.com/v1.0/users/alexd@contoso.com\"}", Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body = ReferenceCreate(", result);
        Assert.Contains("odata_id = \"https://graph.microsoft.com/v1.0/users/alexd@contoso.com\",", result);

    }
    
    [Fact]
    public async Task GenerateWithMoreMapsWithArrayOfObjectsAsync()
    {
        const string url = "/identityGovernance/lifecycleWorkflows/workflows/{workflowId}/createNewVersion";
        const string body = @"
            {
                ""category"": ""joiner"",
                    ""description"": ""Configure new hire tasks for onboarding employees on their first day"",
                    ""displayName"": ""custom email marketing API test"",
                    ""isEnabled"": true,
                    ""isSchedulingEnabled"": false,
                    ""executionConditions"": {
                        ""@odata.type"": ""#microsoft.graph.identityGovernance.triggerAndScopeBasedConditions"",
                        ""scope"": {
                            ""@odata.type"": ""#microsoft.graph.identityGovernance.ruleBasedSubjectSet"",
                            ""rule"": ""(department eq 'Marketing')""
                        },
                        ""trigger"": {
                            ""@odata.type"": ""#microsoft.graph.identityGovernance.timeBasedAttributeTrigger"",
                            ""timeBasedAttribute"": ""employeeHireDate"",
                            ""offsetInDays"": 0
                        }
                    },
                    ""tasks"": [
                    {
                        ""continueOnError"": false,
                        ""description"": ""Enable user account in the directory"",
                        ""displayName"": ""Enable User Account"",
                        ""isEnabled"": true,
                        ""taskDefinitionId"": ""6fc52c9d-398b-4305-9763-15f42c1676fc"",
                        ""arguments"": []
                    },
                    {
                        ""continueOnError"": false,
                        ""description"": ""Send welcome email to new hire"",
                        ""displayName"": ""Send Welcome Email"",
                        ""isEnabled"": true,
                        ""taskDefinitionId"": ""70b29d51-b59a-4773-9280-8841dfd3f2ea"",
                        ""arguments"": [
                        {
                            ""name"": ""cc"",
                            ""value"": ""1baa57fa-3c4e-4526-ba5a-db47a9df95f0""
                        },
                        {
                            ""name"": ""customSubject"",
                            ""value"": ""Welcome to the organization {{userDisplayName}}!""
                        },
                        {
                            ""name"": ""customBody"",
                            ""value"": ""Welcome to our organization {{userGivenName}}!""
                        },
                        {
                            ""name"": ""locale"",
                            ""value"": ""en-us""
                        }
                        ]
                    }
                    ]
            }";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("\"execution_conditions\" : {", result);
        Assert.Contains("\"trigger\" : {", result);
        Assert.Contains("\"scope\" : {", result);
        Assert.Contains("\"trigger\" : {", result);
    }

    [Fact]
    public async Task GeneratesCorrectRequestBuilderNameForIndexedCollectionsAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/{{user-id}}?$select=ext55gb1l09_msLearnCourses");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("query_params = UserItemRequestBuilder.UserItemRequestBuilderGetQueryParameters(", result);
        Assert.Contains("request_configuration = RequestConfiguration(", result);
    }
}
