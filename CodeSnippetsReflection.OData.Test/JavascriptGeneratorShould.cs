using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using CodeSnippetsReflection.OData.LanguageGenerators;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Xunit;

namespace CodeSnippetsReflection.Test
{
    public class JavascriptGeneratorShould
    {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        private readonly IEdmModel _edmModel = CsdlReader.Parse(XmlReader.Create(CommonGeneratorShould.CleanV1Metadata));
        private const string AuthProviderPrefix = "const options = {\r\n\tauthProvider,\r\n};\r\n\r\nconst client = Client.init(options);\r\n\r\n";

        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested objects inside them.
        public async Task GeneratesCorrectCreateCalendarEventJavascriptSnippetAsync()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            //json string with nested object properties
            const string userJsonObject = "{\r\n  \"subject\": \"Let's go for lunch\",\r\n" +
                                          "  \"body\": {\r\n" +
                                          "    \"contentType\": \"HTML\",\r\n" +
                                          "    \"content\": \"Does mid month work for you?\"\r\n" +
                                          "  },\r\n" +
                                          "  \"start\": {\r\n" +
                                          "      \"dateTime\": \"2019-03-15T12:00:00\",\r\n" +
                                          "      \"timeZone\": \"Pacific Standard Time\"\r\n" +
                                          "  },\r\n  " +
                                          "\"end\": {\r\n" +
                                          "      \"dateTime\": \"2019-03-15T14:00:00\",\r\n" +
                                          "      \"timeZone\": \"Pacific Standard Time\"\r\n" +
                                          "  },\r\n" +
                                          "  \"location\":{\r\n" +
                                          "      \"displayName\":\"Harry's Bar\"\r\n" +
                                          "  },\r\n" +
                                          "  \"attendees\": [\r\n" +
                                          "    {\r\n" +
                                          "      \"emailAddress\": {\r\n" +
                                          "        \"address\":\"adelev@contoso.onmicrosoft.com\",\r\n" +
                                          "        \"name\": \"Adele Vance\"\r\n" +
                                          "      },\r\n" +
                                          "      \"type\": \"required\"\r\n" +
                                          "    }\r\n" +
                                          "  ]" +
                                          "\r\n}";

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/users")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            await snippetModel.InitializeModelAsync(requestPayload);

            //Act by generating the code snippet
            var result = JavaScriptGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "const user = {\r\n" +
                                           "  subject: 'Let\\'s go for lunch',\r\n" +
                                           "  body: {\r\n" +
                                           "    contentType: 'HTML',\r\n" +
                                           "    content: 'Does mid month work for you?'\r\n" +
                                           "  },\r\n" +
                                           "  start: {\r\n" +
                                           "      dateTime: '2019-03-15T12:00:00',\r\n" +
                                           "      timeZone: 'Pacific Standard Time'\r\n" +
                                           "  },\r\n" +
                                           "  end: {\r\n" +
                                           "      dateTime: '2019-03-15T14:00:00',\r\n" +
                                           "      timeZone: 'Pacific Standard Time'\r\n" +
                                           "  },\r\n" +
                                           "  location: {\r\n" +
                                           "      displayName: 'Harry\\'s Bar'\r\n" +
                                           "  },\r\n" +
                                           "  attendees: [\r\n" +
                                           "    {\r\n" +
                                           "      emailAddress: {\r\n" +
                                           "        address: 'adelev@contoso.onmicrosoft.com',\r\n" +
                                           "        name: 'Adele Vance'\r\n" +
                                           "      },\r\n" +
                                           "      type: 'required'\r\n" +
                                           "    }\r\n" +
                                           "  ]\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "await client.api('/users')\r\n" +
                                           "\t.post(user);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested objects inside them.
        public async Task GeneratesPostRequestSnippetFromJsonObjectAsync()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            //json string with nested object properties
            const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                          "\"displayName\": \"displayName-value\",\r\n  " +
                                          "\"mailNickname\": \"mailNickname-value\",\r\n  " +
                                          "\"userPrincipalName\": \"upn-value@tenant-value.onmicrosoft.com\",\r\n " +
                                          " \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"password-value\"\r\n  }\r\n}";//nested passwordProfile Object

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/users")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            await snippetModel.InitializeModelAsync(requestPayload);

            //Act by generating the code snippet
            var result = JavaScriptGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "const user = {\r\n" +
                                           "  accountEnabled: true,\r\n" +
                                           "  displayName: 'displayName-value',\r\n" +
                                           "  mailNickname: 'mailNickname-value',\r\n" +
                                           "  userPrincipalName: 'upn-value@tenant-value.onmicrosoft.com',\r\n" +
                                           "  passwordProfile: {\r\n" +
                                           "    forceChangePasswordNextSignIn: true,\r\n" +
                                           "    password: 'password-value'\r\n" +
                                           "  }\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "await client.api('/users')\r\n" +
                                           "\t.post(user);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested arrays inside them.
        public async Task GeneratesPatchRequestSnippetFromJsonObjectAsync()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            //json string with nested objects string array
            const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                          "\"businessPhones\": [\r\n    \"businessPhones-value\",\"businessPhones-value2\",\"businessPhones-value3\"\r\n  ],\r\n  " +//nested ArrayObject with 3 items
                                          "\"city\": \"city-value\"\r\n}";

            var requestPayload = new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/me")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            await snippetModel.InitializeModelAsync(requestPayload);

            //Act by generating the code snippet
            var result = JavaScriptGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "const user = {\r\n  " +
                                               "accountEnabled: true,\r\n  " +
                                               "businessPhones: [\r\n" +
                                               "    'businessPhones-value','businessPhones-value2','businessPhones-value3'\r\n" +
                                               "  ],\r\n  " +
                                               "city: 'city-value'\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "await client.api('/me')\r\n\t.update(user);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }
        
        [Fact]
        //This tests asserts that we can generate snippets with query options present
        public async Task GeneratesSnippetsWithQueryOptionsAsync()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();

            var requestPayload = new HttpRequestMessage(HttpMethod.Get,
                "https://graph.microsoft.com/v1.0/me/calendar/calendarView?startDateTime=2017-01-01T19:00:00.0000000&endDateTime=2017-01-07T19:00:00.0000000");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            await snippetModel.InitializeModelAsync(requestPayload);

            //Act by generating the code snippet
            var result = JavaScriptGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "let calendarView = await client.api('/me/calendar/calendarView?startDateTime=2017-01-01T19:00:00.0000000&endDateTime=2017-01-07T19:00:00.0000000')\r\n\t.get();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        // This test asserts that we can generate snippets with @odata properties
        public async Task GenerateSnippetsWithOdataPropertiesAsync()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();

            const string directoryObj = "{\r\n  \"@odata.id\": \"https://graph.microsoft.com/v1.0/users/{id}\"\r\n}";

            var requestPayload = new HttpRequestMessage(HttpMethod.Put,
                "https://graph.microsoft.com/v1.0/users/{id}/manager/$ref")
            {
                Content = new StringContent(directoryObj)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            await snippetModel.InitializeModelAsync(requestPayload);

            //Act by generating the code snippet
            var result = JavaScriptGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "const directoryObject = {\r\n" +
                "  '@odata.id': 'https://graph.microsoft.com/v1.0/users/{id}'\r\n" + 
                "};\r\n" +
                "\r\n" +
                "await client.api('/users/{id}/manager/$ref')\r\n" +
                "\t.put(directoryObject);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        // This test asserts that single quotes inside strings are escaped
        public async Task GenerateSnippetsWithSingleQuotesInsideStringAsync()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();

            const string actionResultParts = 
                "{\r\n" +
                "  \"values\": [\r\n" +
                "    {\r\n" +
                "      \"@odata.type\": \"microsoft.graph.aadUserConversationMember\",\r\n" +
                "      \"roles\":[],\r\n" +
                "      \"user@odata.bind\": \"https://graph.microsoft.com/beta/users('18a80140-b0fb-4489-b360-2f6efaf225a0')\"\r\n" +
                "    },\r\n" +
                "    {\r\n" +
                "      \"@odata.type\": \"microsoft.graph.aadUserConversationMember\",\r\n" +
                "      \"roles\":[\"owner\"],\r\n" +
                "      \"user@odata.bind\": \"https://graph.microsoft.com/beta/users('86503198-b81b-43fe-81ee-ad45b8848ac9')\"\r\n" +
                "    }\r\n" +
                "  ]\r\n" +
                "}";


            var requestPayload = new HttpRequestMessage(HttpMethod.Post,
                "https://graph.microsoft.com/beta/teams/e4183b04-c9a2-417c-bde4-70e3ee46a6dc/members/add")
            {
                Content = new StringContent(actionResultParts)
            };

            var betaServiceRootUrl = "https://graph.microsoft.com/beta";
            var betaEdmModel = CsdlReader.Parse(XmlReader.Create(CommonGeneratorShould.CleanBetaMetadata));
            var snippetModel = new SnippetModel(requestPayload, betaServiceRootUrl, betaEdmModel);
            await snippetModel.InitializeModelAsync(requestPayload);

            //Act by generating the code snippet
            var result = JavaScriptGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet =
                "const actionResultPart = {\r\n" +
                "  values: [\r\n" +
                "    {\r\n" +
                "      '@odata.type': 'microsoft.graph.aadUserConversationMember',\r\n" +
                "      roles: [],\r\n" +
                "      'user@odata.bind': 'https://graph.microsoft.com/beta/users(\\'18a80140-b0fb-4489-b360-2f6efaf225a0\\')'\r\n" +
                "    },\r\n" +
                "    {\r\n" +
                "      '@odata.type': 'microsoft.graph.aadUserConversationMember',\r\n" +
                "      roles: ['owner'],\r\n" +
                "      'user@odata.bind': 'https://graph.microsoft.com/beta/users(\\'86503198-b81b-43fe-81ee-ad45b8848ac9\\')'\r\n" +
                "    }\r\n" +
                "  ]\r\n" +
                "};\r\n" +
                "\r\n" +
                "await client.api('/teams/e4183b04-c9a2-417c-bde4-70e3ee46a6dc/members/add')\r\n" +
                "\t.version('beta')\r\n" +
                "\t.post(actionResultPart);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }
    }
}
