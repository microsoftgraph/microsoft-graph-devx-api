using CodeSnippetsReflection.LanguageGenerators;
using System.Net.Http;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Xunit;

namespace CodeSnippetsReflection.Test
{
    public class CSharpGeneratorShould
    {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        private readonly IEdmModel _edmModel = CsdlReader.Parse(XmlReader.Create(ServiceRootUrl + "/$metadata"));
        private const string AuthProviderPrefix = "GraphServiceClient graphClient = new GraphServiceClient( authProvider );\r\n\r\n";

        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested objects inside them.
        public void RecursivelyGeneratesNestedPasswordProfileObjectFromJson()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
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

            //Act by generating the code snippet
            var result = CSharpGenerator.GenerateCodeSnippet(snippetModel,expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var user = new User\r\n" +
                                  "{\r\n" +
                                      "\tAccountEnabled = true,\r\n" +
                                      "\tDisplayName = \"displayName-value\",\r\n" +
                                      "\tMailNickname = \"mailNickname-value\",\r\n" +
                                      "\tUserPrincipalName = \"upn-value@tenant-value.onmicrosoft.com\",\r\n" +
                                      "\tPasswordProfile = new PasswordProfile\r\n" +       //object with nested properties
                                      "\t{\r\n" +
                                          "\t\tForceChangePasswordNextSignIn = true,\r\n" + //nested object property
                                          "\t\tPassword = \"password-value\"\r\n" +         //nested object property
                                      "\t}\r\n" +
                                  "};\r\n\r\n" +

                                  "await graphClient.Users\n" +
                                      "\t.Request()\n" +
                                      "\t.AddAsync(user);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }


        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested arrays inside them.
        public void RecursivelyGeneratesNestedPhonesListFromJsonObject()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //json string with nested objects string array
            const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                          "\"businessPhones\": [\r\n    \"businessPhones-value\",\"businessPhones-value2\",\"businessPhones-value3\"\r\n  ],\r\n  " +//nested ArrayObject with 3 items
                                          "\"city\": \"city-value\"\r\n}";

            var requestPayload = new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/me")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = CSharpGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var user = new User\r\n" +
                                           "{\r\n" +
                                               "\tAccountEnabled = true,\r\n" +
                                               "\tBusinessPhones = new List<String>()\r\n" +
                                                //Array object
                                               "\t{\r\n" +
                                                   "\t\t\"businessPhones-value\",\r\n" +    //nested array item
                                                   "\t\t\"businessPhones-value2\",\r\n" +   //nested array item
                                                   "\t\t\"businessPhones-value3\"\r\n" +    //nested array item
                                               "\t},\r\n" +
                                               "\tCity = \"city-value\"\r\n" +
                                           "};\r\n\r\n" +

                                          "await graphClient.Me\n" +
                                              "\t.Request()\n" +
                                              "\t.UpdateAsync(user);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }


        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested object lists(JArray) inside them.
        public void RecursivelyGeneratesNestedRecipientListObjectsFromJson()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //json string with nested object array
            const string messageJsonObject = "{\r\n    " +
                                                 "\"subject\":\"Did you see last night's game?\",\r\n" +
                                                 "\"importance\":\"Low\",\r\n" +
                                                 "\"body\":{\r\n" +
                                                     "\"contentType\":\"HTML\",\r\n" +
                                                     "\"content\":\"They were <b>awesome</b>!\"\r\n" +
                                                "},\r\n" +
                                                 "\"toRecipients\":[\r\n" +
                                                         "{\r\n" +
                                                             "\"emailAddress\":{\r\n" +
                                                                "\"address\":\"AdeleV@contoso.onmicrosoft.com\"\r\n" +
                                                             "}\r\n" +
                                                         "},\r\n" +
                                                         "{\r\n" +
                                                             "\"emailAddress\":{\r\n" +
                                                                "\"address\":\"AdeleV@contoso.onmicrosoft.com\"\r\n" +
                                                             "}\r\n" +
                                                         "}\r\n" +
                                                     "]\r\n" +
                                             "}";

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/messages")
            {
                Content = new StringContent(messageJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = CSharpGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var message = new Message\r\n" +
                                           "{\r\n" +
                                               "\tSubject = \"Did you see last night's game?\",\r\n" +
                                               "\tImportance = Importance.Low,\r\n" +
                                               "\tBody = new ItemBody\r\n" +
                                               "\t{\r\n" +
                                                   "\t\tContentType = BodyType.Html,\r\n" +
                                                   "\t\tContent = \"They were <b>awesome</b>!\"\r\n" +
                                               "\t},\r\n" +
                                               "\tToRecipients = new List<Recipient>()\r\n" +
                                               "\t{\r\n" +
                                                   "\t\tnew Recipient\r\n" +
                                                   "\t\t{\r\n" +
                                                   "\t\t\tEmailAddress = new EmailAddress\r\n" +
                                                       "\t\t\t{\r\n" +
                                                            "\t\t\t\tAddress = \"AdeleV@contoso.onmicrosoft.com\"\r\n" +
                                                       "\t\t\t}\r\n" +
                                                       "\t\t},\r\n" +
                                                   "\t\tnew Recipient\r\n" +
                                                   "\t\t{\r\n" +
                                                       "\t\t\tEmailAddress = new EmailAddress\r\n" +
                                                       "\t\t\t{\r\n" +
                                                            "\t\t\t\tAddress = \"AdeleV@contoso.onmicrosoft.com\"\r\n" +
                                                       "\t\t\t}\r\n" +
                                                   "\t\t}\r\n" +
                                               "\t}\r\n" +
                                           "};\r\n\r\n" +

                                          "await graphClient.Me.Messages\n" +
                                              "\t.Request()\n" +
                                              "\t.AddAsync(message);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with enums separate with Or binary action
        public void GeneratesEnumTypesWithOrSeparatorIfEnumsAreMany()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //json string with nested object array
            const string messageJsonObject = "{\r\n    " +
                                                 "\"EmailAddresses\": [\r\n" +
                                                 "        \"danas@contoso.onmicrosoft.com\", \r\n" +
                                                 "        \"fannyd@contoso.onmicrosoft.com\"\r\n" +
                                                 "    ],\r\n" +
                                                 "    \"MailTipsOptions\": \"automaticReplies, mailboxFullStatus\"\r\n" +//this is an enum that should be ORed together
                                             "}";

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/getMailTips")
            {
                Content = new StringContent(messageJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = CSharpGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var emailAddresses = new List<String>()\r\n" +
                                           "{\r\n" +
                                               "\t\"danas@contoso.onmicrosoft.com\",\r\n" +
                                               "\t\"fannyd@contoso.onmicrosoft.com\"\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "var mailTipsOptions = MailTipsType.AutomaticReplies | MailTipsType.MailboxFullStatus;\r\n" + //Asserting that this OR is done
                                           "\r\n" +
                                           "await graphClient.Me\n" +
                                               "\t.GetMailTips(emailAddresses,mailTipsOptions)\n" +
                                               "\t.Request()\n" +
                                               "\t.PostAsync();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with enums separate with Or binary action
        public void GeneratesSnippetsWithCustomQueryOptions()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();

            var requestPayload = new HttpRequestMessage(HttpMethod.Get,
                "https://graph.microsoft.com/v1.0/me/calendar/calendarView?startDateTime=2017-01-01T19:00:00.0000000&endDateTime=2017-01-07T19:00:00.0000000");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = CSharpGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var queryOptions = new List<QueryOption>()\r\n" +
                                           "{\r\n" +
                                               "\tnew QueryOption(\"startDateTime\", \"2017-01-01T19:00:00.0000000\"),\r\n" +
                                               "\tnew QueryOption(\"endDateTime\", \"2017-01-07T19:00:00.0000000\")\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "var calendarView = await graphClient.Me.Calendar.CalendarView\n" +
                                               "\t.Request( queryOptions )\n" +
                                               "\t.GetAsync();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with $ref operator inside it
        public void GeneratesSnippetsWithReferenceSegmentInPath()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();

            var requestPayload = new HttpRequestMessage(HttpMethod.Delete,"https://graph.microsoft.com/v1.0/groups/{id}/owners/{id}/$ref");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = CSharpGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "await graphClient.Groups[\"{id}\"].Owners[\"{id}\"].Reference\n" +
                                        "\t.Request()\n" +
                                        "\t.DeleteAsync();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }
    }
}
