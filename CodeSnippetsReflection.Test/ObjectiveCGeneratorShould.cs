using CodeSnippetsReflection.LanguageGenerators;
using System.Net.Http;
using System.Text;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Xunit;

namespace CodeSnippetsReflection.Test
{
    public class ObjectiveCGeneratorShould
    {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        private readonly IEdmModel _edmModel = CsdlReader.Parse(XmlReader.Create(ServiceRootUrl + "/$metadata"));
        private const string AuthProviderPrefix = "MSHTTPClient *httpClient = [MSClientFactory createHTTPClientWithAuthenticationProvider:authenticationProvider];\r\n\r\n" +
                                                  "NSString *MSGraphBaseURL = @\"https://graph.microsoft.com/v1.0/\";\r\n";

        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested objects inside them.
        public void RecursivelyGeneratesNestedPasswordProfileObjectFromJson()
        {
            //Arrange
            LanguageExpressions expressions = new ObjectiveCExpressions();
            //json string with nested object properties
            const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                          "\"displayName\": \"displayName-value\",\r\n  " +
                                          "\"mailNickname\": \"mailNickname-value\",\r\n  " +
                                          "\"userPrincipalName\": \"upn-value@tenant-value.onmicrosoft.com\",\r\n " +
                                          " \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"password-value\"\r\n  }\r\n}";//nested passwordProfile Object

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/users")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = ObjectiveCGenerator.GenerateCodeSnippet(snippetModel,expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "NSMutableURLRequest *urlRequest = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:[MSGraphBaseURL stringByAppendingString:@\"/users\"]]];\r\n" +
                                           "[urlRequest setHTTPMethod:@\"POST\"];\r\n" +
                                           "[urlRequest setValue:@\"application/json\" forHTTPHeaderField:@\"Content-Type\"];\r\n" +
                                           "\r\n" +
                                           "MSGraphUser *user = [[MSGraphUser alloc] init];\r\n" +
                                           "[user setAccountEnabled: true];\r\n" +
                                           "[user setDisplayName:@\"displayName-value\"];\r\n" +
                                           "[user setMailNickname:@\"mailNickname-value\"];\r\n" +
                                           "[user setUserPrincipalName:@\"upn-value@tenant-value.onmicrosoft.com\"];\r\n" +
                                           "MSGraphPasswordProfile *passwordProfile = [[MSGraphPasswordProfile alloc] init];\r\n" +
                                           "[passwordProfile setForceChangePasswordNextSignIn: true];\r\n" +
                                           "[passwordProfile setPassword:@\"password-value\"];\r\n" +
                                           "[user setPasswordProfile:passwordProfile];\r\n" +
                                           "\r\n" +
                                           "NSError *error;\r\n" +
                                           "NSData *userData = [user getSerializedDataWithError:&error];\r\n" +
                                           "[urlRequest setHTTPBody:userData];\r\n" +
                                           "\r\n" +
                                           "MSURLSessionDataTask *meDataTask = [httpClient dataTaskWithRequest:urlRequest \r\n" +
                                                "\tcompletionHandler: ^(NSData *data, NSURLResponse *response, NSError *nserror) {\r\n\r\n" +
                                                    "\t\t//Request Completed\r\n\r\n" +
                                           "}];" +
                                           "\r\n\r\n" +
                                           "[meDataTask execute];";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }


        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested arrays inside them.
        public void RecursivelyGeneratesNestedPhonesListFromJsonObject()
        {
            //Arrange
            LanguageExpressions expressions = new ObjectiveCExpressions();
            //json string with nested objects string array
            const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                          "\"businessPhones\": [\r\n    \"businessPhones-value\",\"businessPhones-value2\",\"businessPhones-value3\"\r\n  ],\r\n  " +//nested ArrayObject with 3 items
                                          "\"city\": \"city-value\"\r\n}";

            var requestPayload = new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/me")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = ObjectiveCGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "NSMutableURLRequest *urlRequest = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:[MSGraphBaseURL stringByAppendingString:@\"/me\"]]];\r\n" +
                                           "[urlRequest setHTTPMethod:@\"PATCH\"];\r\n[urlRequest setValue:@\"application/json\" forHTTPHeaderField:@\"Content-Type\"];\r\n" +
                                           "\r\n" +
                                           "MSGraphUser *user = [[MSGraphUser alloc] init];\r\n" +
                                           "[user setAccountEnabled: true];\r\n" +
                                           "NSMutableArray *businessPhonesList = [[NSMutableArray alloc] init];\r\n" +
                                           "[businessPhonesList addObject: @\"businessPhones-value\"];\r\n" +
                                           "[businessPhonesList addObject: @\"businessPhones-value2\"];\r\n" +
                                           "[businessPhonesList addObject: @\"businessPhones-value3\"];\r\n" +
                                           "[user setBusinessPhones:businessPhonesList];\r\n" +
                                           "[user setCity:@\"city-value\"];\r\n" +
                                           "\r\n" +
                                           "NSError *error;\r\n" +
                                           "NSData *userData = [user getSerializedDataWithError:&error];\r\n" +
                                           "[urlRequest setHTTPBody:userData];\r\n" +
                                           "\r\n" +
                                           "MSURLSessionDataTask *meDataTask = [httpClient dataTaskWithRequest:urlRequest \r\n" +
                                                "\tcompletionHandler: ^(NSData *data, NSURLResponse *response, NSError *nserror) {\r\n\r\n" +
                                                    "\t\t//Request Completed\r\n\r\n" +
                                           "}];\r\n" +
                                           "\r\n" +
                                           "[meDataTask execute];";
            
            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }


        [Fact]
        //This tests asserts that we can generate snippets from json objects with nested object lists(JArray) inside them.
        public void RecursivelyGeneratesNestedRecipientListObjectsFromJson()
        {
            //Arrange
            LanguageExpressions expressions = new ObjectiveCExpressions();
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
                Content = new StringContent(messageJsonObject,Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = ObjectiveCGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "NSMutableURLRequest *urlRequest = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:[MSGraphBaseURL stringByAppendingString:@\"/me/messages\"]]];\r\n" +
                                           "[urlRequest setHTTPMethod:@\"POST\"];\r\n" +
                                           "[urlRequest setValue:@\"application/json\" forHTTPHeaderField:@\"Content-Type\"];\r\n" +
                                           "\r\n" +
                                           "MSGraphMessage *message = [[MSGraphMessage alloc] init];\r\n" +
                                           "[message setSubject:@\"Did you see last night's game?\"];\r\n" +
                                           "[message setImportance: [MSGraphImportance low]];\r\n" +
                                           "MSGraphItemBody *body = [[MSGraphItemBody alloc] init];\r\n" +
                                           "[body setContentType: [MSGraphBodyType html]];\r\n" +
                                           "[body setContent:@\"They were <b>awesome</b>!\"];\r\n" +
                                           "[message setBody:body];\r\nNSMutableArray *toRecipientsList = [[NSMutableArray alloc] init];\r\n" +
                                           "MSGraphRecipient *toRecipients = [[MSGraphRecipient alloc] init];\r\n" +
                                           "MSGraphEmailAddress *emailAddress = [[MSGraphEmailAddress alloc] init];\r\n" +
                                           "[emailAddress setAddress:@\"AdeleV@contoso.onmicrosoft.com\"];\r\n" +
                                           "[toRecipients setEmailAddress:emailAddress];\r\n" +
                                           "[toRecipientsList addObject: toRecipients];\r\n" +
                                           "MSGraphRecipient *toRecipients = [[MSGraphRecipient alloc] init];\r\n" +
                                           "MSGraphEmailAddress *emailAddress = [[MSGraphEmailAddress alloc] init];\r\n" +
                                           "[emailAddress setAddress:@\"AdeleV@contoso.onmicrosoft.com\"];\r\n" +
                                           "[toRecipients setEmailAddress:emailAddress];\r\n" +
                                           "[toRecipientsList addObject: toRecipients];\r\n" +
                                           "[message setToRecipients:toRecipientsList];\r\n" +
                                           "\r\n" +
                                           "NSError *error;\r\n" +
                                           "NSData *messageData = [message getSerializedDataWithError:&error];\r\n" +
                                           "[urlRequest setHTTPBody:messageData];\r\n" +
                                           "\r\n" +
                                           "MSURLSessionDataTask *meDataTask = [httpClient dataTaskWithRequest:urlRequest \r\n" +
                                                "\tcompletionHandler: ^(NSData *data, NSURLResponse *response, NSError *nserror) {\r\n\r\n" +
                                                    "\t\t//Request Completed\r\n\r\n" +
                                           "}];\r\n" +
                                           "\r\n" +
                                           "[meDataTask execute];";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests asserts that we can generate snippets with enums separate with Or binary action
        public void GeneratesSnippetsForOdataActionsBySerializingIntoDictionary()
        {
            //Arrange
            LanguageExpressions expressions = new ObjectiveCExpressions();
            //json string with nested object array
            const string messageJsonObject = "{\r\n    " +
                                                 "\"EmailAddresses\": [\r\n" +
                                                 "        \"danas@contoso.onmicrosoft.com\", \r\n" +
                                                 "        \"fannyd@contoso.onmicrosoft.com\"\r\n" +
                                                 "    ],\r\n" +
                                                 "    \"MailTipsOptions\": \"automaticReplies\"\r\n" +
                                             "}";

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/getMailTips")
            {
                Content = new StringContent(messageJsonObject,Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = ObjectiveCGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "NSMutableURLRequest *urlRequest = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:[MSGraphBaseURL stringByAppendingString:@\"/me/getMailTips\"]]];\r\n" +
                                           "[urlRequest setHTTPMethod:@\"POST\"];\r\n[urlRequest setValue:@\"application/json\" forHTTPHeaderField:@\"Content-Type\"];\r\n" +
                                           "\r\n" +
                                           "NSMutableDictionary *payloadDictionary = [[NSMutableDictionary alloc] init];\r\n" +
                                           "\r\n" +
                                           "NSMutableArray *emailAddressesList = [[NSMutableArray alloc] init];\r\n" +
                                           "[emailAddressesList addObject: @\"danas@contoso.onmicrosoft.com\"];\r\n" +
                                           "[emailAddressesList addObject: @\"fannyd@contoso.onmicrosoft.com\"];\r\n" +
                                           "payloadDictionary[@\"EmailAddresses\"] = emailAddressesList;\r\n" +
                                           "\r\n" +
                                           "MSGraphMailTipsType *mailTipsOptions = [MSGraphMailTipsType automaticReplies];\r\n" +
                                           "payloadDictionary[@\"MailTipsOptions\"] = mailTipsOptions;\r\n" +
                                           "\r\n" +
                                           "NSData *data = [NSJSONSerialization dataWithJSONObject:payloadDictionary options:kNilOptions error:&error];\r\n" +
                                           "[urlRequest setHTTPBody:data];\r\n" +
                                           "\r\n" +
                                           "MSURLSessionDataTask *meDataTask = [httpClient dataTaskWithRequest:urlRequest \r\n" +
                                                "\tcompletionHandler: ^(NSData *data, NSURLResponse *response, NSError *nserror) {\r\n\r\n" +
                                                    "\t\t//Request Completed\r\n\r\n" +
                                           "}];\r\n" +
                                           "\r\n" +
                                           "[meDataTask execute];";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

        [Fact]
        //This tests that snippets that fetch collections are deserialized to MSCollection Instances
        public void GeneratesSnippetsFetchingCollectionToMsCollectionInstances()
        {
            //Arrange
            LanguageExpressions expressions = new ObjectiveCExpressions();

            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/drive/root/children");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act by generating the code snippet
            var result = ObjectiveCGenerator.GenerateCodeSnippet(snippetModel, expressions);

            //Assert code snippet string matches expectation
            const string expectedSnippet = "NSMutableURLRequest *urlRequest = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:[MSGraphBaseURL stringByAppendingString:@\"/me/drive/root/children\"]]];\r\n" +
                                           "[urlRequest setHTTPMethod:@\"GET\"];\r\n\r\nMSURLSessionDataTask *meDataTask = [httpClient dataTaskWithRequest:urlRequest \r\n" +
                                                "\tcompletionHandler: ^(NSData *data, NSURLResponse *response, NSError *nserror) {\r\n" +
                                                "\r\n" +
                                                    "\t\tNSError *jsonError = nil;\r\n" +
                                                    "\t\tMSCollection *collection = [[MSCollection alloc] initWithData:data error:&jsonError];\r\n" + //deserialize the data to a MSCollection
                                                    "\t\tMSGraphDriveItem *driveItem = [[MSGraphDriveItem alloc] initWithDictionary:[[collection value] objectAtIndex: 0] error:&nserror];\r\n" + //get an single element in the collection
                                           "\r\n" +
                                           "}];" +
                                           "\r\n" +
                                           "\r\n" +
                                           "[meDataTask execute];";

            //Assert the snippet generated is as expected
            Assert.Equal(AuthProviderPrefix + expectedSnippet, result);
        }

    }
}
