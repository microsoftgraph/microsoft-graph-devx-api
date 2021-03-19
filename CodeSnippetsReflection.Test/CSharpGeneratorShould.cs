using CodeSnippetsReflection.LanguageGenerators;
using System.Net.Http;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Xunit;
using System;

namespace CodeSnippetsReflection.Test
{
    public class CSharpGeneratorShould
    {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        private const string ServiceRootUrlBeta = "https://graph.microsoft.com/beta";
        private readonly Lazy<IEdmModel> _edmModel = new Lazy<IEdmModel>(() => CsdlReader.Parse(XmlReader.Create(CommonGeneratorShould.CleanV1Metadata)));
        private readonly Lazy<IEdmModel> _edmModelBeta = new Lazy<IEdmModel>(() => CsdlReader.Parse(XmlReader.Create(CommonGeneratorShould.CleanBetaMetadata)));
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel,expressions);

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

                                  "await graphClient.Users\r\n" +
                                      "\t.Request()\r\n" +
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

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

                                          "await graphClient.Me\r\n" +
                                              "\t.Request()\r\n" +
                                              "\t.UpdateAsync(user);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        // This tests asserts that we can generate snippets where value is assigned to null and the
        // only type hint we have is the function parameter type.
        public void TypeIsInferredFromActionParametersForNonNullableType()
        {
            // Arrange
            LanguageExpressions expressions = new CSharpExpressions();

            // json string with nested objects string array
            const string rowsJsonObject = @"{
                                                ""index"": null,
                                                ""values"": [
                                                    [1, 2, 3],
                                                    [4, 5, 6]
                                                 ]
                                            }";

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/tables/{id|name}/rows/add")
            {
                Content = new StringContent(rowsJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            // Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            // Assert the snippet generated is as expected
            Assert.Contains("Int32? index = null;", result);
        }

        [Fact]
        // This tests asserts that we can generate snippets where value is assigned to null and the
        // only type hint we have is the function parameter type.
        // The difference from the other test TypeIsInferredFromActionParametersForNonNullableType is that
        // parameter type is String, so it is nullable by default and we don't need question mark following the type name.
        public void TypeIsInferredFromActionParametersForNullableType()
        {
            // Arrange
            LanguageExpressions expressions = new CSharpExpressions();

            // json string with nested objects string array
            const string rowsJsonObject = @"{
                                              ""displayName"": ""Test Printer"",
                                              ""manufacturer"": ""Test Printer Manufacturer"",
                                              ""model"": ""Test Printer Model"",
                                              ""physicalDeviceId"": null,
                                              ""hasPhysicalDevice"": false,
                                              ""certificateSigningRequest"": {
                                                            ""content"": ""{content}"",
                                                ""transportKey"": ""{sampleTransportKey}""
                                              },
                                              ""connectorId"": null
                                            }";

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/beta/print/printers/create")
            {
                Content = new StringContent(rowsJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrlBeta, _edmModelBeta.Value);

            // Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            // Assert the snippet generated is as expected
            Assert.Contains("String physicalDeviceId = null;", result);
        }

        [Fact]
        public void RequestBodyIsConvertedIntoStreamForStreamObjects()
        {
            // Arrange
            LanguageExpressions expressions = new CSharpExpressions();

            const string jsonObject = @"[
                                           {
                                            'target':'#para-id',
                                            'action':'insert',
                                            'position':'before',
                                            'content':'<img src=""image-url-or-part-name"" alt=""image-alt-text"" />'
                                          }, 
                                          {
                                            'target':'#list-id',
                                            'action':'append',
                                            'content':'<li>new-page-content</li>'
                                          }
                                        ]";

            var requestPayload = new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/me/onenote/pages/{id}/content")
            {
                Content = new StringContent(jsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            // Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            // Assert that a stream object is created
            Assert.Contains("var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(@\"[", result);

            // Assert that stream is set as a property of a full object and full object is passed in in the PATCH request.
            Assert.Contains("pages.Content = stream;", result);
            Assert.Contains("await graphClient.Me.Onenote.Pages[\"{id}\"]", result);
            Assert.DoesNotContain("await graphClient.Me.Onenote.Pages[\"{id}\"].Content", result);
            Assert.Contains("UpdateAsync(pages)", result);
        }

        [Fact]
        public void DoesNotFlattenNestedODataQueries()
        {
            // Arrange
            LanguageExpressions expressions = new CSharpExpressions();

            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/messages/{id}?$expand=singleValueExtendedProperties($filter=id eq '{id_value}')");

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            // Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains(".Expand(\"singleValueExtendedProperties($filter=id%20eq%20'%7Bid_value%7D')\")", result);
            Assert.DoesNotContain(".Filter(", result);
        }

        [Fact]
        public void GeneratesTopLevelExpandAndFilterTogether()
        {
            // Arrange
            LanguageExpressions expressions = new CSharpExpressions();

            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/messages/{id}?$expand=singleValueExtendedProperties($filter=id eq '{id_value1}')&$filter=id eq '{id_value2}'");

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            // Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains(".Expand(\"singleValueExtendedProperties($filter=id%20eq%20'%7Bid_value1%7D')\")", result);
            Assert.Contains(".Filter(\"id eq '{id_value2}'\")", result);
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

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

                                          "await graphClient.Me.Messages\r\n" +
                                              "\t.Request()\r\n" +
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var emailAddresses = new List<String>()\r\n" +
                                           "{\r\n" +
                                               "\t\"danas@contoso.onmicrosoft.com\",\r\n" +
                                               "\t\"fannyd@contoso.onmicrosoft.com\"\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "var mailTipsOptions = MailTipsType.AutomaticReplies | MailTipsType.MailboxFullStatus;\r\n" + //Asserting that this OR is done
                                           "\r\n" +
                                           "await graphClient.Me\r\n" +
                                               "\t.GetMailTips(emailAddresses,mailTipsOptions)\r\n" +
                                               "\t.Request()\r\n" +
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var queryOptions = new List<QueryOption>()\r\n" +
                                           "{\r\n" +
                                               "\tnew QueryOption(\"startDateTime\", \"2017-01-01T19:00:00.0000000\"),\r\n" +
                                               "\tnew QueryOption(\"endDateTime\", \"2017-01-07T19:00:00.0000000\")\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "var calendarView = await graphClient.Me.Calendar.CalendarView\r\n" +
                                               "\t.Request( queryOptions )\r\n" +
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "await graphClient.Groups[\"{id}\"].Owners[\"{id}\"].Reference\r\n" +
                                        "\t.Request()\r\n" +
                                        "\t.DeleteAsync();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with $ref that adds/creates references
        public void GeneratesSnippetsAddingReferencesToDirectoryObject()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            const string messageJsonObject = "{\r\n  \"@odata.id\": \"https://graph.microsoft.com/v1.0/users/{id}\"\r\n}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/groups/{id}/owners/$ref")
            {
                Content = new StringContent(messageJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);


            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var directoryObject = new DirectoryObject\r\n" +
                                           "{\r\n" +
                                            "\tId = \"{id}\"\r\n" +
                                           "};\r\n" +
                                           "\r\n" +

                                           "await graphClient.Groups[\"{id}\"].Owners.References" +
                                                "\r\n\t.Request()" +
                                                "\r\n\t.AddAsync(directoryObject);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with $ref that adds/creates references
        public void GeneratesSnippetsAddingReferencesToDirectoryObjectWithNonUriReference()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            const string messageJsonObject = "{\r\n  \"@odata.id\": \"ExampleID\"\r\n}";//non uri reference
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/groups/{id}/owners/$ref")
            {
                Content = new StringContent(messageJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);


            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var directoryObject = new DirectoryObject\r\n" +
                                           "{\r\n" +
                                           "\tId = \"ExampleID\"\r\n" +
                                           "};\r\n" +
                                           "\r\n" +

                                           "await graphClient.Groups[\"{id}\"].Owners.References" +
                                           "\r\n\t.Request()" +
                                           "\r\n\t.AddAsync(directoryObject);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with $ref that adds/creates references with multiple additionalData inserted
        public void GeneratesSnippetsAddingReferencesToObjectWithExtraAdditionalData()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            const string messageJsonObject = "{\r\n  " +
                                             "\"@odata.id\": \"https://graph.microsoft.com/v1.0/users/{id}\" ," +
                                             "\"@odata.context\": \"https://graph.microsoft.com/v1.0/$metadata#users/$entity\"" +
                                             "\r\n}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/groups/{id}/owners/$ref")
            {
                Content = new StringContent(messageJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var directoryObject = new DirectoryObject\r\n" +
                                           "{\r\n" +
                                           "\tId = \"{id}\",\r\n" +
                                           "\tAdditionalData = new Dictionary<string, object>()\r\n" +
                                           "\t{\r\n" +
                                           "\t\t{\"@odata.context\", \"https://graph.microsoft.com/v1.0/$metadata#users/$entity\"}\r\n" +
                                           "\t}\r\n" +
                                           "};\r\n" +
                                           "\r\n" +

                                           "await graphClient.Groups[\"{id}\"].Owners.References" +
                                           "\r\n\t.Request()" +
                                           "\r\n\t.AddAsync(directoryObject);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can properly generate snippets for odata actions with parameters
        public void GeneratesSnippetsForOdataActionsWithParameters()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();

            var requestPayload = new HttpRequestMessage(HttpMethod.Get,
                "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/worksheets/{id|name}/range(address='A1:B2')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var workbookRange = await graphClient.Me.Drive.Items[\"{id}\"].Workbook.Worksheets[\"{id|name}\"]\r\n" +
                                               "\t.Range(\"A1:B2\")\r\n" +//parameter has double quotes
                                               "\t.Request()\r\n" +
                                               "\t.GetAsync();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can properly generate snippets with DateTime strings present and parse them appropriately
        public void GeneratesSnippetsWithDateTimeStrings()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            const string messageJsonObject = "{\r\n" +
                                             "  \"receivedDateTime\": \"datetime-value\",\r\n" +//dateTimeOffsetObject
                                             "  \"sentDateTime\": \"datetime-value\",\r\n" + //dateTime to be parsed
                                                                                        //https://docs.microsoft.com/en-us/graph/api/resources/message?view=graph-rest-1.0#properties
                                             "  \"hasAttachments\": true,\r\n" +
                                             "  \"subject\": \"subject-value\",\r\n" +
                                             "  \"body\": {\r\n" +
                                             "    \"contentType\": \"\",\r\n" +
                                             "    \"content\": \"content-value\"\r\n" +
                                             "  },\r\n" +
                                             "  \"bodyPreview\": \"bodyPreview-value\"\r\n" +
                                             "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/mailFolders/{id}/messages")
            {
                Content = new StringContent(messageJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var message = new Message\r\n" +
                                           "{\r\n" +
                                                "\tReceivedDateTime = DateTimeOffset.Parse(\"datetime-value\"),\r\n" +
                                               "\tSentDateTime = DateTimeOffset.Parse(\"datetime-value\"),\r\n" +
                                               "\tHasAttachments = true,\r\n" +
                                               "\tSubject = \"subject-value\",\r\n" +
                                               "\tBody = new ItemBody\r\n" +
                                               "\t{\r\n" +
                                                   "\t\tContentType = BodyType.Text,\r\n" +
                                                   "\t\tContent = \"content-value\"\r\n" +
                                               "\t},\r\n" +
                                               "\tBodyPreview = \"bodyPreview-value\"\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "await graphClient.Me.MailFolders[\"{id}\"].Messages\r\n" +
                                               "\t.Request()\r\n" +
                                               "\t.AddAsync(message);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can properly generate snippets with Guid strings present and parse them appropriately
        public void GeneratesSnippetsWithGuidStrings()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            const string messageJsonObject = "{\r\n" +
                                             "  \"name\": \"name-value\",\r\n" +
                                             "  \"classId\": \"classId-value\",\r\n" + //this is GUID type according to docs here
                                                                                       //https://docs.microsoft.com/en-us/graph/api/resources/calendargroup?view=graph-rest-1.0#properties
                                             "  \"changeKey\": \"changeKey-value\"\r\n" +
                                             "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/calendarGroups")
            {
                Content = new StringContent(messageJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var calendarGroup = new CalendarGroup\r\n" +
                                           "{\r\n" +
                                           "\tName = \"name-value\",\r\n" +
                                           "\tClassId = Guid.Parse(\"classId-value\"),\r\n" +
                                           "\tChangeKey = \"changeKey-value\"\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "await graphClient.Me.CalendarGroups\r\n" +
                                           "\t.Request()\r\n" +
                                           "\t.AddAsync(calendarGroup);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can properly generate snippets with DateTime strings that should stay as strings
        public void GeneratesSnippetsWithDateTimeStringsThatShouldNotBeParsed()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            const string messageJsonObject = "{        \r\n" +
                                             "    \"schedules\": [\"adelev@contoso.onmicrosoft.com\", \"meganb@contoso.onmicrosoft.com\"],\r\n" +
                                             "    \"startTime\": {\r\n" +
                                             "        \"dateTime\": \"2019-03-15T09:00:00\",\r\n" + //this stays as a string as it is part of dateTimeZone
                                             "        \"timeZone\": \"Pacific Standard Time\"\r\n" +
                                             "    },\r\n" +
                                             "    \"endTime\": {\r\n" +
                                             "        \"dateTime\": \"2019-03-15T18:00:00\",\r\n" + //this stays as a string as it is part of dateTimeZone
                                             "        \"timeZone\": \"Pacific Standard Time\"\r\n" +
                                             "    },\r\n" +
                                             "    \"availabilityViewInterval\": \"60\"\r\n" +// integer primitive
                                             "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/calendar/getSchedule")
            {
                Content = new StringContent(messageJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var schedules = new List<String>()\r\n" +
                                           "{\r\n" +
                                               "\t\"adelev@contoso.onmicrosoft.com\",\r\n" +
                                               "\t\"meganb@contoso.onmicrosoft.com\"\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "var startTime = new DateTimeTimeZone\r\n" +
                                           "{\r\n" +
                                               "\tDateTime = \"2019-03-15T09:00:00\",\r\n" + //this stays as a string as it is part of dateTimeZone
                                               "\tTimeZone = \"Pacific Standard Time\"\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "var endTime = new DateTimeTimeZone\r\n" +
                                           "{\r\n" +
                                               "\tDateTime = \"2019-03-15T18:00:00\",\r\n" + //this stays as a string as it is part of dateTimeZone
                                               "\tTimeZone = \"Pacific Standard Time\"\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "var availabilityViewInterval = \"60\";\r\n" +
                                           "\r\n" +
                                           "await graphClient.Me.Calendar\r\n" +
                                               "\t.GetSchedule(schedules,endTime,startTime,availabilityViewInterval)\r\n" +
                                               "\t.Request()\r\n" +
                                               "\t.PostAsync();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can properly generate snippets with some property originally set to null.
        public void GeneratesSnippetsWithRecurrencePropertySetToNull()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //This request example is present at the link
            //https://docs.microsoft.com/en-us/graph/api/event-update?view=graph-rest-1.0&tabs=http#request
            const string messageJsonObject = "{\r\n" +
                                             "  \"originalStartTimeZone\": \"originalStartTimeZone-value\",\r\n" +
                                             "  \"originalEndTimeZone\": \"originalEndTimeZone-value\",\r\n" +
                                             "  \"responseStatus\": {\r\n" +
                                             "    \"response\": \"\",\r\n" +
                                             "    \"time\": \"datetime-value\"\r\n" +
                                             "  },\r\n" +
                                             "  \"recurrence\": null,\r\n" +    //property set to null in request object
                                             "  \"iCalUId\": \"iCalUId-value\",\r\n" +
                                             "  \"reminderMinutesBeforeStart\": 99,\r\n" +
                                             "  \"isReminderOn\": true\r\n" +
                                             "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/me/events/{id}")
            {
                Content = new StringContent(messageJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var @event = new Event\r\n" +
                                           "{\r\n" +
                                           "\tOriginalStartTimeZone = \"originalStartTimeZone-value\",\r\n" +
                                           "\tOriginalEndTimeZone = \"originalEndTimeZone-value\",\r\n" +
                                           "\tResponseStatus = new ResponseStatus\r\n" +
                                           "\t{\r\n" +
                                           "\t\tResponse = ResponseType.None,\r\n" +
                                           "\t\tTime = DateTimeOffset.Parse(\"datetime-value\")\r\n" +//the dateTimeOffset appropriately parsed
                                           "\t},\r\n" +
                                           "\tRecurrence = null,\r\n" +   //the property has been appropriately set to null
                                           "\tICalUId = \"iCalUId-value\",\r\n" +
                                           "\tReminderMinutesBeforeStart = 99,\r\n" +
                                           "\tIsReminderOn = true\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "await graphClient.Me.Events[\"{id}\"]\r\n" +
                                           "\t.Request()\r\n" +
                                           "\t.UpdateAsync(@event);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can properly generate snippets and List that is not a string type but of Enums
        public void GeneratesSnippetsForEnumTypedList()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //This request example is present at the link
            //https://docs.microsoft.com/en-us/graph/api/user-post-events?view=graph-rest-1.0&tabs=http#request-3
            const string messageJsonObject = "{\r\n" +
                                             "  \"subject\": \"Let's go for lunch\",\r\n" +
                                             "  \"body\": {\r\n" +
                                             "    \"contentType\": \"HTML\",\r\n" +
                                             "    \"content\": \"Does noon time work for you?\"\r\n" +
                                             "  },\r\n" +
                                             "  \"start\": {\r\n" +
                                             "      \"dateTime\": \"2017-09-04T12:00:00\",\r\n" +
                                             "      \"timeZone\": \"Pacific Standard Time\"\r\n" +
                                             "  },\r\n" +
                                             "  \"end\": {\r\n" +
                                             "      \"dateTime\": \"2017-09-04T14:00:00\",\r\n" +
                                             "      \"timeZone\": \"Pacific Standard Time\"\r\n" +
                                             "  },\r\n" +
                                             "  \"recurrence\": {\r\n" +
                                             "    \"pattern\": {\r\n" +
                                             "      \"type\": \"weekly\",\r\n" +
                                             "      \"interval\": 1,\r\n" +
                                             "      \"daysOfWeek\": [ \"Monday\" ]\r\n" +   //this is a list of enums
                                             "    },\r\n" +
                                             "    \"range\": {\r\n" +
                                             "      \"type\": \"endDate\",\r\n" +
                                             "      \"startDate\": \"2017-09-04\",\r\n" +   //Date Type
                                             "      \"endDate\": \"2017-12-31\"\r\n" +      //Date Type
                                             "    }\r\n" +
                                             "  },\r\n" +
                                             "  \"location\":{\r\n" +
                                             "      \"displayName\":\"Harry's Bar\"\r\n" +
                                             "  },\r\n" +
                                             "  \"attendees\": [\r\n" +
                                             "    {\r\n" +
                                             "      \"emailAddress\": {\r\n" +
                                             "        \"address\":\"AdeleV@contoso.onmicrosoft.com\",\r\n" +
                                             "        \"name\": \"Adele Vance\"\r\n" +
                                             "      },\r\n" +
                                             "      \"type\": \"required\"\r\n" +
                                             "    }\r\n" +
                                             "  ]\r\n" +
                                             "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/me/events/{id}")
            {
                Content = new StringContent(messageJsonObject)
            };

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var @event = new Event\r\n" +
                                           "{\r\n" +
                                               "\tSubject = \"Let's go for lunch\",\r\n" +
                                               "\tBody = new ItemBody\r\n" +
                                               "\t{\r\n" +
                                                   "\t\tContentType = BodyType.Html,\r\n" +
                                                   "\t\tContent = \"Does noon time work for you?\"\r\n" +
                                               "\t},\r\n" +
                                               "\tStart = new DateTimeTimeZone\r\n" +
                                               "\t{\r\n" +
                                                   "\t\tDateTime = \"2017-09-04T12:00:00\",\r\n" +
                                                   "\t\tTimeZone = \"Pacific Standard Time\"\r\n" +
                                               "\t},\r\n" +
                                               "\tEnd = new DateTimeTimeZone\r\n" +
                                               "\t{\r\n" +
                                                   "\t\tDateTime = \"2017-09-04T14:00:00\",\r\n" +
                                                   "\t\tTimeZone = \"Pacific Standard Time\"\r\n" +
                                               "\t},\r\n" +
                                               "\tRecurrence = new PatternedRecurrence\r\n" +
                                               "\t{\r\n" +
                                                   "\t\tPattern = new RecurrencePattern\r\n" +
                                                   "\t\t{\r\n" +
                                                       "\t\t\tType = RecurrencePatternType.Weekly,\r\n" +
                                                       "\t\t\tInterval = 1,\r\n" +
                                                       "\t\t\tDaysOfWeek = new List<DayOfWeek>()\r\n" +//list of DayOfWeek enum
                                                       "\t\t\t{\r\n" +
                                                       "\t\t\t\tDayOfWeek.Monday\r\n" +                //member of list
                                                       "\t\t\t}\r\n" +
                                                   "\t\t},\r\n" +
                                                   "\t\tRange = new RecurrenceRange\r\n" +
                                                   "\t\t{\r\n" +
                                                       "\t\t\tType = RecurrenceRangeType.EndDate,\r\n" +
                                                       "\t\t\tStartDate = new Date(2017,9,4),\r\n" +  //Date Type
                                                       "\t\t\tEndDate = new Date(2017,12,31)\r\n" +   //Date Type
                                                   "\t\t}\r\n" +
                                               "\t},\r\n" +
                                               "\tLocation = new Location\r\n" +
                                               "\t{\r\n" +
                                                    "\t\tDisplayName = \"Harry's Bar\"\r\n" +
                                               "\t},\r\n" +
                                               "\tAttendees = new List<Attendee>()\r\n" +
                                               "\t{\r\n" +
                                                   "\t\tnew Attendee\r\n" +
                                                   "\t\t{\r\n" +
                                                       "\t\t\tEmailAddress = new EmailAddress\r\n" +
                                                       "\t\t\t{\r\n" +
                                                           "\t\t\t\tAddress = \"AdeleV@contoso.onmicrosoft.com\",\r\n" +
                                                           "\t\t\t\tName = \"Adele Vance\"\r\n" +
                                                       "\t\t\t},\r\n" +
                                                       "\t\t\tType = AttendeeType.Required\r\n" +
                                                   "\t\t}\r\n" +
                                               "\t}\r\n};\r\n" +
                                           "\r\n" +
                                           "await graphClient.Me.Events[\"{id}\"]\r\n" +
                                               "\t.Request()\r\n" +
                                               "\t.UpdateAsync(@event);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This test asserts that a request with optional parameters is generated correctly with the required
        //parameters first
        public void GeneratesSnippetsWithOptionalParametersInCorrectOrder()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            const string jsonObject = "{\r\n  " +
                                      "\"address\": \"Sheet1!A1:D5\",\r\n" +
                                      "\"hasHeaders\": true\r\n" +
                                      "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/tables/add")
            {
                Content = new StringContent(jsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);
            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var address = \"Sheet1!A1:D5\";\r\n" +
                                           "\r\n" +
                                           "var hasHeaders = true;\r\n" +
                                           "\r\n" +
                                           "await graphClient.Me.Drive.Items[\"{id}\"].Workbook.Tables\r\n" +
                                           "\t.Add(hasHeaders,address)\r\n" +
                                           "\t.Request()\r\n" +
                                           "\t.PostAsync();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This test asserts that a request for a structural properties of type stream are generated in the normal url like fashion as
        // streams have the request builders generated.
        public void GeneratesSnippetsWithStructuralPropertiesOfTypeStream()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/drive/items/{item-id}/content");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var stream = await graphClient.Me.Drive.Items[\"{item-id}\"].Content\r\n" +
                                           "\t.Request()\r\n" +
                                           "\t.GetAsync();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This test asserts that a request with the odata.type property overrides the parent type from the metadata
        public void GeneratesSnippetsWithOverridenTypeInBody()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            const string jsonObject = "{\r\n  " +
                                      "\"@odata.type\": \"#microsoft.graph.fileAttachment\",\r\n  " + //subclass to use to override metadata superclass
                                      "\"name\": \"smile\",\r\n  " +
                                      "\"contentBytes\": \"R0lGODdhEAYEAA7\"\r\n" +
                                      "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/messages/AAMkpsDRVK/attachments")
            {
                Content = new StringContent(jsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);
            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var attachment = new FileAttachment\r\n" + // Use the FileAttachment class rather than the Attachment superclass from metadata
                                           "{\r\n" +
                                           "\tName = \"smile\",\r\n" +
                                           "\tContentBytes = Encoding.ASCII.GetBytes(\"R0lGODdhEAYEAA7\")\r\n" +
                                           "};\r\n" +

                                           "\r\nawait graphClient.Me.Messages[\"AAMkpsDRVK\"].Attachments\r\n" +
                                           "\t.Request()\r\n" +
                                           "\t.AddAsync(attachment);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This test asserts that a request with multiline string is escaped correctly
        public void GeneratesSnippetsWithMultilineStringEscaped()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            const string jsonObject = @"{
                ""id"": ""60860cdd-fb4d-4054-91ba-f75e04444aa6"",
                ""displayName"": ""Test world UPDATED NAME!"",
                ""descriptionForAdmins"": ""Test world"",
                ""descriptionForReviewers"": ""Test world"",
                ""scope"": {
                    ""query"": ""/groups/b7a059cb-038a-4802-8fc9-b9d1ed0cf11f/transitiveMembers"",
                    ""queryType"": ""MicrosoftGraph""
                },
                ""instanceEnumerationScope"": {
                    ""query"": ""/groups/b7a059cb-038a-4802-8fc9-b9d1ed0cf11f"",
                    ""queryType"": ""MicrosoftGraph""
                },
                ""reviewers"": [],
                ""settings"": {
                    ""mailNotificationsEnabled"": true,
                    ""reminderNotificationsEnabled"": true,
                    ""justificationRequiredOnApproval"": true,
                    ""defaultDecisionEnabled"": false,
                    ""defaultDecision"": ""None"",
                    ""instanceDurationInDays"": 3,
                    ""autoApplyDecisionsEnabled"": false,
                    ""recommendationsEnabled"": true,
                    ""recurrence"": {
                    ""pattern"": {
                        ""type"": ""weekly"",
                        ""interval"": 1
                    },
                    ""range"": {
                        ""type"": ""noEnd"",
                        ""startDate"": ""2020-09-15""
                    }
                    }
                }
            }";
            var url = "https://graph.microsoft.com/beta/identityGovernance/accessReviews/definitions/60860cdd-fb4d-4054-91ba-f75e04444aa6";
            var requestPayload = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(jsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrlBeta, _edmModelBeta.Value);
            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModelBeta.Value).GenerateCodeSnippet(snippetModel, expressions);

            const string doubleDoubleQuotedString = "\"\"id\"\": \"\"60860cdd-fb4d-4054-91ba-f75e04444aa6\"\"";
            const string stream = "new System.IO.MemoryStream(Encoding.UTF8.GetBytes(@\"{";
            //Assert the snippet generated is as expected
            Assert.Contains(doubleDoubleQuotedString, result);
            Assert.Contains(stream, result);
        }

        [Fact]
        //This test asserts that Enums are displayed Correctly in Csharp Snippets
        public void GeneratesSnippetsWithEnumsCorrectlyDisplayed()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            var url = "https://graph.microsoft.com/beta/reports/authenticationMethods/usersRegisteredByMethod(includedUserTypes='all',includedUserRoles='all')";

            var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrlBeta, _edmModelBeta.Value);
            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModelBeta.Value).GenerateCodeSnippet(snippetModel, expressions);

           
            //Assert the snippet generated is as expected
            var expected = "GraphServiceClient graphClient = new GraphServiceClient( authProvider );\r\n\r\n" +
                        "var userRegistrationMethodSummary = await graphClient.Reports.AuthenticationMethods\r\n" +
                        "\t.UsersRegisteredByMethod(IncludedUserTypes.All,IncludedUserRoles.All)\r\n" +
                        "\t.Request()\r\n" +
                        "\t.GetAsync();";
            Assert.Equal(expected, result);
        }

        [Fact]
        // This tests asserts that a type beginning with "@" character is also added to the AdditionalData bag
        public void GeneratesSnippetsWithTypesStartingWithTheAtSymbol()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            const string jsonObject = "{\r\n" +
                                      "  \"name\": \"New Folder\",\r\n" +
                                      "  \"folder\": { },\r\n" +
                                      "  \"@microsoft.graph.conflictBehavior\": \"rename\"\r\n" +//to be added to the AdditionalData
                                      "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/drive/root/children")
            {
                Content = new StringContent(jsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);
            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var driveItem = new DriveItem\r\n" +
                                           "{\r\n" +
                                                "\tName = \"New Folder\",\r\n" +
                                               "\tFolder = new Folder\r\n" +
                                               "\t{\r\n" +
                                               "\t},\r\n" +
                                               "\tAdditionalData = new Dictionary<string, object>()\r\n" +
                                               "\t{\r\n" +
                                                    "\t\t{\"@microsoft.graph.conflictBehavior\", \"rename\"}\r\n" +
                                               "\t}\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "await graphClient.Me.Drive.Root.Children\r\n" +
                                               "\t.Request()\r\n" +
                                               "\t.AddAsync(driveItem);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        // This tests asserts that a skiptoken is added as a Query option
        public void GeneratesSnippetsSkipTokenAddedAsQueryOptions()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/calendarView/delta?$skiptoken=R0usmcCM996atia_s");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);
            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModel.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var queryOptions = new List<QueryOption>()\r\n" +
                                           "{\r\n" +
                                                "\tnew QueryOption(\"$skiptoken\", \"R0usmcCM996atia_s\")" +
                                           "\r\n};\r\n" +
                                           "\r\n" +
                                           "await graphClient.Me.CalendarView\r\n" +
                                                "\t.Delta()\r\n" +
                                                "\t.Request()\r\n" +
                                                "\t.PostAsync();";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        // This tests asserts that snippets is correctly generated for other namespaces
        public void GeneratesSnippetsInOtherNamespace()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            var requestPayload = new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/beta/termStore/sets/{setId}");
            const string jsonObject = "{\r\n" +
                                      "  \"description\": \"mySet\",\r\n" +
                                      "}";
            requestPayload.Content = new StringContent(jsonObject);
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrlBeta, _edmModelBeta.Value);
            
            //Act by generating the code snippet
            var result = new CSharpGenerator(_edmModelBeta.Value).GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "var set = new Microsoft.Graph.TermStore.Set\r\n" +
                                           "{\r\n" +
                                                "\tDescription = \"mySet\"\r\n" +
                                           "};\r\n" +
                                           "\r\n" +
                                           "await graphClient.TermStore.Sets[\"{setId}\"]\r\n" +
                                                "\t.Request()\r\n" +
                                                "\t.UpdateAsync(set);";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        public void GeneratesSnippetsInOtherNamespaceWhenTypeIsSpecifiedInRequestBody()
        {
            // Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/beta/compliance/ediscovery/cases/{caseId}/legalHolds");
            const string jsonObject = @"
                {
                  ""@odata.type"": ""#microsoft.graph.ediscovery.legalHold"",
                  ""description"": ""String"",
                  ""createdBy"": {
                                ""@odata.type"": ""microsoft.graph.identitySet""
                  },
                  ""isEnabled"": ""Boolean"",
                  ""status"": ""String"",
                  ""contentQuery"": ""String"",
                  ""errors"": [
                    ""String""
                  ],
                  ""displayName"": ""String""
                }";
            requestPayload.Content = new StringContent(jsonObject);
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrlBeta, _edmModelBeta.Value);

            // Act by generating the code snippet
            var result = new CSharpGenerator(_edmModelBeta.Value).GenerateCodeSnippet(snippetModel, expressions);

            // Assert the snippet generated with fully qualified name
            Assert.Contains("new Microsoft.Graph.Ediscovery.LegalHold", result);
            Assert.DoesNotContain("new LegalHold", result);
        }

        [Fact]
        public void CreatesPlaceHolderIDForCommandLineCaller()
        {
            const bool isCommandLine = true; // Command line caller (e.g. generation pipeline for the docs)

            const string itemIdFromOriginalSnippet = "{item-id-from-original-snippet}";
            const string itemIdBasedOnTypeInformation = "{driveItem-id}";

            var expressions = new CSharpExpressions();
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/me/drive/items/{itemIdFromOriginalSnippet}/content");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            var result = new CSharpGenerator(_edmModel.Value, isCommandLine).GenerateCodeSnippet(snippetModel, expressions);

            Assert.Contains(itemIdBasedOnTypeInformation, result);
            Assert.DoesNotContain(itemIdFromOriginalSnippet, result);
        }

        [Fact]
        public void UsesIDFromHTTPSnippetForDevXAPICallers()
        {
            const bool isCommandLine = false; // DevX API caller (e.g. Graph Explorer "Code Snippets" tab)

            const string itemIdFromDevXApiSnippet = "e56b1746-25a4-4a4e-86cf-a7223fa1fee1";
            const string itemIdBasedOnTypeInformation = "{driveItem-id}";

            var expressions = new CSharpExpressions();
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/me/drive/items/{itemIdFromDevXApiSnippet}/content");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel.Value);

            var result = new CSharpGenerator(_edmModel.Value, isCommandLine).GenerateCodeSnippet(snippetModel, expressions);

            Assert.DoesNotContain(itemIdBasedOnTypeInformation, result);
            Assert.Contains(itemIdFromDevXApiSnippet, result);
        }
    }
}
