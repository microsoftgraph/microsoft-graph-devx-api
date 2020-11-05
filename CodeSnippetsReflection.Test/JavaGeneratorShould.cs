using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
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
        public void MapCorrectTypeForGuidReturnType()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string userJsonObject = "{"
                                            + "\"keyId\": \"f0b0b335-1d71-4883-8f98-567911bfdca6\","
                                            + "\"proof\":\"eyJ0eXAiOiJ...\""
                                        + "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/applications/{id}/removeKey")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("UUID keyId", result);
        }

        [Fact]
        public void MapCorrectTypeForJsonProperties()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string userJsonObject = "{"
                                              + "\"type\": \"ColumnStacked\","
                                              + "\"sourceData\": \"A1:B1\","
                                              + "\"seriesBy\": \"Auto\""
                                            + "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/worksheets/{id|name}/charts/add")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("JsonElement sourceData = JsonParser.parseString", result);
        }

        [Fact]
        public void MapCorrectTypeForGuidProperties()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string userJsonObject = "{"
                                              + "\"principalId\": \"principalId-value\",\r\n"
                                              + "\"resourceId\": \"resourceId-value\",\r\n"
                                              + "\"appRoleId\": \"appRoleId-value\"\r\n"
                                        + "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/users/{id}/appRoleAssignments")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("UUID.fromString(\"principalId-value\");", result);
        }
        [Fact]
        public void MapCorrectTypeForBinaryProperties()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string userJsonObject = "{"
                                            +"\"@odata.type\": \"#microsoft.graph.fileAttachment\",\n\t"
                                            + "\"name\": \"menu.txt\",\n\t"
                                            + "\"contentBytes\": \"base64bWFjIGFuZCBjaGVlc2UgdG9kYXk=\"\n\t"
                                        +"}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/events/AAMkAGI1AAAt9AHjAAA=/attachments")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains(" Base64.getDecoder().decode(\"base64bW", result);
        }
        [Fact]
        public void MapCorrectTypeForDateTimeProperties()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string userJsonObject = "{"
                                            + "\"responseStatus\": {"
                                            + "\"response\": \"\","
                                            + "\"time\": \"datetime-value\""
                                            +"},"
                                        + "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/me/events/{id}")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains(" CalendarSerializer.deserialize(\"datetime-value\")", result);
        }
        [Fact]
        public void MapCorrectTypeForDoubleProperties()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string userJsonObject = "{"
                                            + "\"id\": \"id-value\","
                                            + "\"height\": 99,"
                                            + "\"left\": 99"
                                        + "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/worksheets/{id|name}/charts")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Matches(new Regex(@"\d+d"), result);
        }
        [Fact]
        public void MapCorrectTypeForLongProperties()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string userJsonObject = "{"
                                              + "\"AttachmentItem\": {"
                                                + "\"attachmentType\": \"file\","
                                                + "\"name\": \"flower\", "
                                                + "\"size\": 3483322"
                                              + "}"
                                            + "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/messages/AAMkADI5MAAIT3drCAAA=/attachments/createUploadSession")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Matches(new Regex(@"\d+L"), result);
        }
        [Fact]
        public void MapCorrectTypeForDurationProperties()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string userJsonObject = "{ "
                                          + "\"attendees\": [ "
                                            + "{ "
                                              + "\"type\": \"required\",  "
                                              + "\"emailAddress\": { "
                                                + "\"name\": \"Alex Wilbur\","
                                                + "\"address\": \"alexw@contoso.onmicrosoft.com\" "
                                              + "} "
                                            + "}"
                                          + "],  "
                                          + "\"locationConstraint\": { "
                                            + "\"isRequired\": \"false\",  "
                                            + "\"suggestLocation\": \"false\",  "
                                            + "\"locations\": [ "
                                              + "{ "
                                                + "\"resolveAvailability\": \"false\","
                                                + "\"displayName\": \"Conf room Hood\" "
                                              + "} "
                                            + "] "
                                          + "},  "
                                          + "\"timeConstraint\": {"
                                            + "\"activityDomain\":\"work\", "
                                            + "\"timeSlots\": [ "
                                              + "{ "
                                                + "\"start\": { "
                                                  + "\"dateTime\": \"2019-04-16T09:00:00\",  "
                                                  + "\"timeZone\": \"Pacific Standard Time\" "
                                                + "},  "
                                                + "\"end\": { "
                                                  + "\"dateTime\": \"2019-04-18T17:00:00\",  "
                                                  + "\"timeZone\": \"Pacific Standard Time\" "
                                                + "} "
                                              + "} "
                                            + "] "
                                          + "},  "
                                          + "\"isOrganizerOptional\": \"false\","
                                          + "\"meetingDuration\": \"PT1H\","
                                          + "\"returnSuggestionReasons\": \"true\","
                                          + "\"minimumAttendeePercentage\": \"100\""
                                        + "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/findMeetingTimes")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("DatatypeFactory.newInstance().newDuration", result);
        }
        [Fact]
        public void MapCorrectTypeForDateOnlyProperties()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string userJsonObject = "{"
                                              + "\"subject\": \"Let's go for lunch\","
                                              + "\"body\": {"
                                                + "\"contentType\": \"HTML\","
                                                + "\"content\": \"Does noon time work for you?\""
                                              + "},"
                                              + "\"start\": {"
                                                  + "\"dateTime\": \"2017-09-04T12:00:00\","
                                                  + "\"timeZone\": \"Pacific Standard Time\""
                                              + "},"
                                              + "\"end\": {"
                                                  + "\"dateTime\": \"2017-09-04T14:00:00\","
                                                  + "\"timeZone\": \"Pacific Standard Time\""
                                              + "},"
                                              + "\"recurrence\": {"
                                                + "\"pattern\": {"
                                                  + "\"type\": \"weekly\","
                                                  + "\"interval\": 1,"
                                                  + "\"daysOfWeek\": [ \"Monday\" ]"
                                                + "},"
                                                + "\"range\": {"
                                                  + "\"type\": \"endDate\","
                                                  + "\"startDate\": \"2017-09-04\","
                                                  + "\"endDate\": \"2017-12-31\""
                                                + "}"
                                              + "},"
                                              + "\"location\":{"
                                                  + "\"displayName\":\"Harry's Bar\""
                                              + "},"
                                              + "\"attendees\": ["
                                                + "{"
                                                  + "\"emailAddress\": {"
                                                    + "\"address\":\"AdeleV@contoso.onmicrosoft.com\","
                                                    + "\"name\": \"Adele Vance\""
                                                  + "},"
                                                  + "\"type\": \"required\""
                                                + "}"
                                              + "],"
                                              + "\"allowNewTimeProposals\": true"
                                            + "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/events")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("new DateOnly(", result);
        }
        [Fact]
        public void MapCorrectTypeForTimeOfDayProperties()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string userJsonObject = "{"
                                              + "\"workingHours\": {"
                                                  + "\"endTime\" : \"18:30:00.0000000\", "
                                                  + "\"daysOfWeek\": [ "
                                                      + "\"Monday\", "
                                                      + "\"Tuesday\", "
                                                      + "\"Wednesday\", "
                                                      + "\"Thursday\", "
                                                      + "\"Friday\", "
                                                      + "\"Saturday\" "
                                                  + "], "
                                                  + "\"timeZone\" : { "
                                                     + "\"@odata.type\": \"#microsoft.graph.customTimeZone\", "
                                                     + "\"bias\":-300, "
                                                     + "\"name\": \"Customized Time Zone\","
                                                     + "\"standardOffset\":{   "
                                                       + "\"time\":\"02:00:00.0000000\", "
                                                       + "\"dayOccurrence\":2, "
                                                       + "\"dayOfWeek\":\"Sunday\", "
                                                       + "\"month\":10, "
                                                       + "\"year\":0 "
                                                     + "}, "
                                                     + "\"daylightOffset\":{   "
                                                       + "\"daylightBias\":100, "
                                                       + "\"time\":\"02:00:00.0000000\", "
                                                       + "\"dayOccurrence\":4, "
                                                       + "\"dayOfWeek\":\"Sunday\", "
                                                       + "\"month\":5, "
                                                       + "\"year\":0 "
                                                     + "} "
                                                  + "} "
                                              + "}"
                                            + "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/me/mailboxSettings")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("new TimeOfDay(", result);
        }
        [Fact]
        public void HasStreamUseInputStreams()
        {
            LanguageExpressions expressions = new JavaExpressions();
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/messages/4aade2547798441eab5188a7a2436bc1/$value");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);
            Assert.Contains("InputStream", result);
        }
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
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

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
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

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
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

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
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

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
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

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
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "graphClient.groups(\"{id}\").owners(\"{id}\").reference()\n" +
                                                "\t.buildRequest()\n" +
                                                "\t.delete();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with $ref that adds/creates references with
        // a Uri as reference
        public void GeneratesSnippetsAddingReferencesToDirectoryObject()
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
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "DirectoryObject directoryObject = new DirectoryObject();\r\n" +
                                           "directoryObject.id = \"{id}\";\r\n" +

                                           "\r\n" +
                                           "graphClient.groups(\"{id}\").owners().references()\n" +
                                               "\t.buildRequest()\n" +
                                               "\t.post(directoryObject);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with $ref that adds/creates references with multiple additionalData inserted
        public void GeneratesSnippetsAddingReferencesToObjectWithExtraAdditionalData()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();
            const string messageJsonObject = "{\r\n  " +
                                             "\"@odata.id\": \"https://graph.microsoft.com/v1.0/users/{id}\" ," +
                                             "\"@odata.context\": \"https://graph.microsoft.com/v1.0/$metadata#users/$entity\"" +
                                             "\r\n}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/groups/{id}/owners/$ref")
            {
                Content = new StringContent(messageJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "DirectoryObject directoryObject = new DirectoryObject();\r\n" +
                                           "directoryObject.id = \"{id}\";\r\n" +
                                           "directoryObject.additionalDataManager().put(\"@odata.context\", new JsonPrimitive(\"https://graph.microsoft.com/v1.0/$metadata#users/$entity\"));\r\n" +
                                           "\r\n" +
                                           "graphClient.groups(\"{id}\").owners().references()\n" +
                                           "\t.buildRequest()\n" +
                                           "\t.post(directoryObject);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with $ref that adds/creates references with
        // a reference that is not a Uri
        public void GeneratesSnippetsAddingReferencesToDirectoryObjectWithNonUriReference()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();
            const string messageJsonObject = "{\r\n  \"@odata.id\": \"ExampleID\"\r\n}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/groups/{id}/owners/$ref")
            {
                Content = new StringContent(messageJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);


            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "DirectoryObject directoryObject = new DirectoryObject();\r\n" +
                                           "directoryObject.id = \"ExampleID\";\r\n" +

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
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "WorkbookRange workbookRange = graphClient.me().drive().items(\"{id}\").workbook().worksheets(\"{id|name}\")\n" +
                                           "\t.range(\"A1:B2\")\n" +//parameter has double quotes
                                           "\t.buildRequest()\n" +
                                           "\t.get();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
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
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "String address = \"Sheet1!A1:D5\";\r\n" +
                                           "\r\n" +
                                           "Boolean hasHeaders = true;\r\n" +
                                           "\r\n" +
                                           "graphClient.me().drive().items(\"{id}\").workbook().tables()\n" +
                                                "\t.add(address,hasHeaders)\n" +
                                                "\t.buildRequest()\n" +
                                                "\t.post();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This test asserts that a request with the odata.type property overrides the type from the metadata
        public void GeneratesSnippetsWithOverridenTypeInBody()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();
            const string jsonObject = "{\r\n  " +
                                      "\"@odata.type\": \"#microsoft.graph.fileAttachment\",\r\n  " + //subclass to use to override metadata superclass
                                      "\"name\": \"smile\",\r\n  " +
                                      "\"contentBytes\": \"R0lGODdhEAYEAA7\"\r\n" +
                                      "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/messages/AAMkpsDRVK/attachments")
            {
                Content = new StringContent(jsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "FileAttachment attachment = new FileAttachment();\r\n" + // Use the FileAttachment class rather than the Attachment superclass from metadata 
                                           "attachment.name = \"smile\";\r\n" +
                                           "attachment.contentBytes = Base64.getDecoder().decode(\"R0lGODdhEAYEAA7\");\r\n" +
                                           "\r\n" +
                                           "graphClient.me().messages(\"AAMkpsDRVK\").attachments()\n" +
                                           "\t.buildRequest()\n" +
                                           "\t.post(attachment);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        // This tests asserts that a type beginning with "@" character is also added to the AdditionalData bag
        public void GeneratesSnippetsWithTypesStartingWithTheAtSymbol()
        {
            //Arrange
            LanguageExpressions expressions = new JavaExpressions();
            const string jsonObject = "{\r\n" +
                                      "  \"name\": \"New Folder\",\r\n" +
                                      "  \"folder\": { },\r\n" +
                                      "  \"@microsoft.graph.conflictBehavior\": \"rename\"\r\n" + //to be added to the AdditionalData
                                      "}";
            var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/drive/root/children")
                {
                    Content = new StringContent(jsonObject)
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "DriveItem driveItem = new DriveItem();\r\n" +
                                           "driveItem.name = \"New Folder\";\r\n" +
                                           "Folder folder = new Folder();\r\n" +
                                           "driveItem.folder = folder;\r\n" +
                                           "driveItem.additionalDataManager().put(\"@microsoft.graph.conflictBehavior\", new JsonPrimitive(\"rename\"));\r\n" +
                                           "\r\n" +
                                           "graphClient.me().drive().root().children()\n" +
                                                "\t.buildRequest()\n" +
                                                "\t.post(driveItem);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }
        [Fact]
        public void EscapeQuotesForJsonPrimitives()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string jsonObject = "{"
                                      + "\"members@odata.bind\": ["
                                        + "\"https://graph.microsoft.com/v1.0/directoryObjects/{id}\","
                                        + "\"https://graph.microsoft.com/v1.0/directoryObjects/{id}\","
                                        + "\"https://graph.microsoft.com/v1.0/directoryObjects/{id}\""
                                        + "]"
                                    + "}";
            var requestPayload =
                new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/groups/{group-id}")
                {
                    Content = new StringContent(jsonObject)
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("\\\"", result);
        }
        [Fact]
        public void UseCollectionPagesWithReferencesWhenNecessary()
        {
            LanguageExpressions expressions = new JavaExpressions();
            var requestPayload =
                new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/applications/{id}/owners");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("WithReferencesPage", result);
        }
        [Fact]
        public void UseCollectionPagesWithMethodsNamesForMethods()
        {
            LanguageExpressions expressions = new JavaExpressions();
            var requestPayload =
                new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/drives/{drive-id}/items/{item-id}/getActivitiesByInterval(startDateTime='2017-01-01',endDateTime='2017-01-3',interval='day')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("DriveItemGetActivitiesByIntervalCollectionPage ", result);
        }
        [Fact]
        public void GenerateDeltaCollectionPages()
        {
            LanguageExpressions expressions = new JavaExpressions();
            var requestPayload =
                new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/groups/delta");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("GroupDeltaCollectionPage ", result);
        }
        [Fact]
        public void MapCollectionPagesProperties()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string jsonObject = "{"
                                        + "\"@odata.type\": \"#Microsoft.Graph.channel\","
                                        + "\"membershipType\": \"private\","
                                        + "\"displayName\": \"My First Private Channel\","
                                        + "\"description\": \"This is my first private channels\","
                                        + "\"members\":"
                                            + "["
                                            + "{"
                                                + "\"@odata.type\":\"#microsoft.graph.aadUserConversationMember\","
                                                + "\"user@odata.bind\":\"https://graph.microsoft.com/v1.0/users('{user_id}')\","
                                                + "\"roles\":[\"owner\"]"
                                            + "}"
                                            + "]"
                                    + "}";
            var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/teams/{group_id}/channels")
                {
                    Content = new StringContent(jsonObject)
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("channel.members = conversationMemberCollectionPage;", result);
        }

        [Fact]
        public void MapEnumSetsProperties()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string jsonObject = "{"
                                      + "\"policyViolation\": {"
                                        + "\"policyTip\": {"
                                          + "\"generalText\" : \"This item has been blocked by the administrator.\","
                                          + "\"complianceUrl\" : \"https://contoso.com/dlp-policy-page\","
                                          + "\"matchedConditionDescriptions\" : [\"Credit Card Number\"]"
                                        + "},"
                                        + "\"verdictDetails\" : \"AllowOverrideWithoutJustification,AllowFalsePositiveOverride\","
                                        + "\"dlpAction\" : \"BlockAccess\""
                                      + "}"
                                    + "}";
            var requestPayload =
                new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/teams/e1234567-e123-4276-55555-6232b0e3a89a/channels/a7654321-e321-0000-0000-123b0e3a00a/messages/19%3Aa21b0b0c05194ebc9e30000000000f61%40thread.skype")
                {
                    Content = new StringContent(jsonObject)
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("EnumSet.of", result);
        }
        [Fact]
        public void MapEnumListVariables()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string jsonObject = "{"
                                          + "\"@odata.type\": \"#microsoft.graph.call\","
                                          + "\"callbackUri\": \"https://bot.contoso.com/callback\","
                                          + "\"targets\": ["
                                            + "{"
                                              + "\"@odata.type\": \"#microsoft.graph.invitationParticipantInfo\","
                                              + "\"identity\": {"
                                                + "\"@odata.type\": \"#microsoft.graph.identitySet\","
                                                + "\"user\": {"
                                                  + "\"@odata.type\": \"#microsoft.graph.identity\","
                                                  + "\"displayName\": \"John\","
                                                  + "\"id\": \"112f7296-5fa4-42ca-bae8-6a692b15d4b8\""
                                                + "}"
                                              + "}"
                                            + "}"
                                          + "],"
                                          + "\"requestedModalities\": ["
                                            + "\"audio\""
                                          + "],"
                                          + "\"mediaConfig\": {"
                                            + "\"@odata.type\": \"#microsoft.graph.serviceHostedMediaConfig\""
                                          + "}"
                                        + "}";
            var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/communications/calls")
                {
                    Content = new StringContent(jsonObject)
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("LinkedList<Modality>", result);
        }
        [Fact]
        public void MapEnumSetVariables()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string jsonObject = "{"
                                         + "\"displayName\": \"Library Assist\","
                                         + "\"description\": \"Self help community for library\","
                                         + "\"mailNickname\": \"libassist\","
                                         + "\"partsToClone\": \"apps,tabs,settings,channels,members\","
                                         + "\"visibility\": \"public\""
                                    + "}";
            var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/teams/{id}/clone")
                {
                    Content = new StringContent(jsonObject)
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("EnumSet<ClonableTeamParts>", result);
        }
        [Fact]
        public void SnakeCaseEnumsProperly()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string jsonObject = "{"
                                      + "\"subject\": \"Let's go for lunch\","
                                      + "\"body\": {"
                                        + "\"contentType\": \"HTML\","
                                        + "\"content\": \"Does mid month work for you?\""
                                      + "},"
                                      + "\"start\": {"
                                          + "\"dateTime\": \"2019-03-15T12:00:00\","
                                          + "\"timeZone\": \"Pacific Standard Time\""
                                      + "},"
                                      + "\"end\": {"
                                          + "\"dateTime\": \"2019-03-15T14:00:00\","
                                          + "\"timeZone\": \"Pacific Standard Time\""
                                      + "},"
                                      + "\"location\":{"
                                          + "\"displayName\":\"Harry's Bar\""
                                      + "},"
                                      + "\"attendees\": ["
                                        + "{"
                                          + "\"emailAddress\": {"
                                            + "\"address\":\"adelev@contoso.onmicrosoft.com\","
                                            + "\"name\": \"Adele Vance\""
                                          + "},"
                                          + "\"type\": \"required\""
                                        + "}"
                                      + "],"
                                      + "\"transactionId\":\"7E163156-7762-4BEB-A1C6-729EA81755A7\""
                                    + "}";
            var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/calendars/AAMkAGViNDU7zAAAAAGtlAAA=/events")
                {
                    Content = new StringContent(jsonObject)
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("BodyType.HTML;", result);
        }
        [Fact]
        public void MapCorrectTypeForPrimitiveVariables()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string jsonObject = "{"
                                      + "\"index\": 3,"
                                      + "\"values\": ["
                                        + "{"
                                        + "}"
                                      + "]"
                                    + "}";

            var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/tables/{id|name}/columns/add")
                {
                    Content = new StringContent(jsonObject)
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("int index = 3;", result);
        }
        [Fact]
        public void MapCorrectTypeForCollectionsOfPrimitiveVariables()
        {
            LanguageExpressions expressions = new JavaExpressions();
            const string jsonObject = "{"
                                      + "\"addLicenses\": ["
                                        + "{"
                                          + "\"disabledPlans\": [ \"11b0131d-43c8-4bbb-b2c8-e80f9a50834a\" ],"
                                          + "\"skuId\": \"skuId-value-1\""
                                        + "},"
                                        + "{"
                                          + "\"disabledPlans\": [ \"a571ebcc-fqe0-4ca2-8c8c-7a284fd6c235\" ],"
                                          + "\"skuId\": \"skuId-value-2\""
                                        + "}"
                                      + "],"
                                      + "\"removeLicenses\": []"
                                    + "}";

            var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/groups/1ad75eeb-7e5a-4367-a493-9214d90d54d0/assignLicense")
                {
                    Content = new StringContent(jsonObject)
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains("LinkedList<UUID>", result);
        }
        [Fact]
        public void GenerateProfilePicture()
        {
            LanguageExpressions expressions = new JavaExpressions();
            using var mstream = new MemoryStream();
            using var writer = new StreamWriter(mstream);
            writer.Write("dummy content");
            writer.Flush();
            mstream.Position = 0;
            var requestPayload =
                new HttpRequestMessage(HttpMethod.Put, "https://graph.microsoft.com/v1.0/me/photo/$value")
                {
                    Content = new StreamContent(mstream)
                };
            requestPayload.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            //Act by generating the code snippet
            var result = new JavaGenerator(_edmModel).GenerateCodeSnippet(snippetModel, expressions);
            Assert.Contains("byte[]", result);
        }
    }
}
