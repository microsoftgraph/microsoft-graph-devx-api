using System;
using System.Net.Http;
using System.Xml;
using CodeSnippetsReflection.LanguageGenerators;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Xunit;

namespace CodeSnippetsReflection.Test
{
    public class JavaGeneratorShould
    {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        private readonly IEdmModel _edmModel = CsdlReader.Parse(XmlReader.Create(ServiceRootUrl + "/$metadata"));
        private const string AuthProviderPrefix = "IGraphServiceClient graphClient = GraphServiceClient.builder().authenticationProvider( authProvider ).buildClient();\r\n\r\n";

        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested objects inside them.
        public void RecursivelyGeneratesNestedPasswordProfileObjectFromJson()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();
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
            var result = JavaGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "User user = new User();\r\n" +
                                           "user.accountEnabled = true;\r\n" +
                                           "user.displayName = \"displayName-value\";\r\n" +
                                           "user.mailNickname = \"mailNickname-value\";\r\n" +
                                           "user.userPrincipalName = \"upn-value@tenant-value.onmicrosoft.com\";\r\n" +
                                           "PasswordProfile passwordProfile = new PasswordProfile();\r\n" +
                                           "passwordProfile.forceChangePasswordNextSignIn = true;\r\n" +
                                           "passwordProfile.password = \"password-value\";\r\n" +
                                           "user.passwordProfile = passwordProfile;\r\n" +
                                           "\r\n" +
                                           "graphClient.users()\n" +
                                                "\t.buildRequest()\n" +
                                                "\t.post(user);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested arrays inside them.
        public void RecursivelyGeneratesNestedPhonesListFromJsonObject()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();
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
            var result = JavaGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "User user = new User();\r\n" +
                                           "user.accountEnabled = true;\r\n" +
                                           "LinkedList<String> businessPhonesList = new LinkedList<String>();\r\n" +
                                           "businessPhonesList.add(\"businessPhones-value\");\r\n" +
                                           "businessPhonesList.add(\"businessPhones-value2\");\r\n" +
                                           "businessPhonesList.add(\"businessPhones-value3\");\r\n" +
                                           "user.businessPhones = businessPhonesList;\r\n" +
                                           "user.city = \"city-value\";\r\n" +
                                           "\r\n" +
                                           "graphClient.me()\n" +
                                                "\t.buildRequest()\n" +
                                                "\t.patch(user);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested object lists(JArray) inside them.
        public void RecursivelyGeneratesNestedRecipientListObjectsFromJson()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();
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
            var result = JavaGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "Message message = new Message();\r\n" +
                                           "message.subject = \"Did you see last night's game?\";\r\n" +
                                           "message.importance = Importance.LOW;\r\n" +

                                           "ItemBody body = new ItemBody();\r\n" +
                                           "body.contentType = BodyType.HTML;\r\n" +
                                           "body.content = \"They were <b>awesome</b>!\";\r\n" +
                                           "message.body = body;\r\n" +

                                           "LinkedList<Recipient> toRecipientsList = new LinkedList<Recipient>();\r\n" +
                                           "Recipient toRecipients = new Recipient();\r\n" +
                                           "EmailAddress emailAddress = new EmailAddress();\r\n" +
                                           "emailAddress.address = \"AdeleV@contoso.onmicrosoft.com\";\r\n" +
                                           "toRecipients.emailAddress = emailAddress;\r\n" +
                                           "toRecipientsList.add(toRecipients);\r\n" +
                                           "Recipient toRecipients1 = new Recipient();\r\n" +
                                           "EmailAddress emailAddress1 = new EmailAddress();\r\n" +
                                           "emailAddress1.address = \"AdeleV@contoso.onmicrosoft.com\";\r\n" +
                                           "toRecipients1.emailAddress = emailAddress1;\r\n" +
                                           "toRecipientsList.add(toRecipients1);\r\n" +
                                           "message.toRecipients = toRecipientsList;\r\n" +
                                           "\r\n" +
                                           "graphClient.me().messages()\n" +
                                                "\t.buildRequest()\n" +
                                                "\t.post(message);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with query options present
        public void GeneratesSnippetsWithQueryOptions()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();

            var requestPayload = new HttpRequestMessage(HttpMethod.Get,
                "https://graph.microsoft.com/v1.0/me/calendar/calendarView?startDateTime=2017-01-01T19:00:00.0000000&endDateTime=2017-01-07T19:00:00.0000000");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = JavaGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "LinkedList<Option> requestOptions = new LinkedList<Option>();\r\n" +
                                           "requestOptions.add(new QueryOption(\"startDateTime\", \"2017-01-01T19:00:00.0000000\"));\r\n" + //Query Options present
                                           "requestOptions.add(new QueryOption(\"endDateTime\", \"2017-01-07T19:00:00.0000000\"));\r\n" + //Query Options present
                                           "\r\n" +
                                           "IEventCollectionPage calendarView = graphClient.me().calendar().calendarView()\n" +
                                                "\t.buildRequest( requestOptions )\n" +
                                                "\t.get();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with query options present
        public void GeneratesSnippetsWithHeaderOptions()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();

            var requestPayload = new HttpRequestMessage(HttpMethod.Get,
                "https://graph.microsoft.com/v1.0/me/events/AAMkAGIAAAoZDOFAAA=?$select=subject,body,bodyPreview,organizer,attendees,start,end,location");
            requestPayload.Headers.Add("Prefer", "outlook.timezone=\"Pacific Standard Time\"");

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = JavaGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "LinkedList<Option> requestOptions = new LinkedList<Option>();\r\n" +
                                           "requestOptions.add(new HeaderOption(\"Prefer\", \"outlook.timezone=\\\"Pacific Standard Time\\\"\"));\r\n" + //Header Options present
                                           "\r\n" +
                                           "Event event = graphClient.me().events(\"AAMkAGIAAAoZDOFAAA=\")\n" +
                                                "\t.buildRequest( requestOptions )\n" +
                                                "\t.select(\"subject,body,bodyPreview,organizer,attendees,start,end,location\")\n" +
                                                "\t.get();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with $ref operator inside it
        public void GeneratesSnippetsWithReferenceSegmentInPath()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();

            var requestPayload = new HttpRequestMessage(HttpMethod.Delete, "https://graph.microsoft.com/v1.0/groups/{id}/owners/{id}/$ref");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = JavaGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "graphClient.groups(\"{id}\").owners(\"{id}\").reference()\n" +
                                                "\t.buildRequest()\n" +
                                                "\t.delete();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with $ref that adds/creates references
        public void GeneratesSnippetsAddingReferencesToObject()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();
            const string messageJsonObject = "{\r\n  \"@odata.id\": \"https://graph.microsoft.com/v1.0/users/{id}\"\r\n}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/groups/{id}/owners/$ref")
            {
                Content = new StringContent(messageJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);


            //Act by generating the code snippet
            var result = JavaGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "DirectoryObject directoryObject = new DirectoryObject();\r\n" +
                                           "directoryObject.additionalDataManager().put(\"@odata.id\", new JsonPrimitive(\"https://graph.microsoft.com/v1.0/users/{id}\"));\r\n" +
                                           "\r\n" +
                                           "graphClient.groups(\"{id}\").owners().references()\n" +
                                               "\t.buildRequest()\n" +
                                               "\t.post(directoryObject);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can properly generate snippets for odata actions with parameters
        public void GeneratesSnippetsForOdataActionsWithParameters()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();

            var requestPayload = new HttpRequestMessage(HttpMethod.Get,
                "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/worksheets/{id|name}/range(address='A1:B2')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = JavaGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "WorkbookRange workbookRange = graphClient.me().drive().items(\"{id}\").workbook().worksheets(\"{id|name}\")\n" +
                                           "\t.range(\"A1:B2\")\n" +//parameter has double quotes
                                           "\t.buildRequest()\n" +
                                           "\t.get();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we return an error if we try to get beta snippets for Java
        public void ThrowsNotImplementedExceptionForBeta()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();

            //Act
            var requestPayload = new HttpRequestMessage(HttpMethod.Get,
                "https://graph.microsoft.com/beta/me");
            var snippetModel = new SnippetModel(requestPayload, "https://graph.microsoft.com/beta", _edmModel);

            //Assert that we do not generate snippets for java beta for now
            Assert.Throws<NotImplementedException>(() =>JavaGenerator.GenerateCodeSnippet(snippetModel, expressions));
        }

        [Fact]
        //This test asserts that a request with optional parameters is generated correctly with the order present
        //in the metadata
        public void GeneratesSnippetsWithOptionalParametersInCorrectOrder()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();
            const string jsonObject = "{\r\n  " +
                                      "\"address\": \"Sheet1!A1:D5\",\r\n" +
                                      "\"hasHeaders\": true\r\n" +
                                      "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/tables/add")
            {
                Content = new StringContent(jsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = JavaGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "String address = \"Sheet1!A1:D5\";\r\n" +
                                           "\r\n" +
                                           "boolean hasHeaders = true;\r\n" +
                                           "\r\n" +
                                           "graphClient.me().drive().items(\"{id}\").workbook().tables()\n" +
                                                "\t.add(address,hasHeaders)\n" +
                                                "\t.buildRequest()\n" +
                                                "\t.post();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }
    }
}
