using CodeSnippetsReflection.LanguageGenerators;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Xml;
using Microsoft.OData.UriParser;
using Xunit;

namespace CodeSnippetsReflection.Test
{
    public class CommonGeneratorShould
    {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        public const string CleanV1Metadata = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/clean_v10_metadata/cleanMetadataWithDescriptionsv1.0.xml";
        public const string CleanBetaMetadata = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/clean_beta_metadata/cleanMetadataWithDescriptionsbeta.xml";
        private readonly IEdmModel _edmModel = CsdlReader.Parse(XmlReader.Create(CleanV1Metadata));

        #region Test GenerateQuerySection
        [Fact]
        public void GenerateQuerySection_ShouldReturnEmptyStringIfQueryListIsEmpty()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/drive/root");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateJavascriptSelectExpression()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users/{id}?$select=displayName,givenName,postalCode");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\r\n\t.select('displayName,givenName,postalCode')", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateJavascriptFilterExpression()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users?$filter=startswith(givenName, 'J')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\r\n\t.filter('startswith(givenName, 'J')')", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateJavascriptSearchExpression()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/people/?$search=\"Irene McGowen\"");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\r\n\t.search('Irene McGowen')", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateJavascriptSkipExpression()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/events?$skip=20");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\r\n\t.skip(20)", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateJavascriptTopExpression()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/events?$top=5");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\r\n\t.top(5)", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateJavascriptRequestHeaderExpression()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users/{id}");
            requestPayload.Headers.Add("Prefer", "kenya-timezone");

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\r\n\t.header('Prefer','kenya-timezone')", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateJavascriptRequestHeaderExpressionWithEscapedDoubleQuotes()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/events");
            requestPayload.Headers.Add("Prefer", "outlook.timezone=\"Pacific Standard Time\"");

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\r\n\t.header('Prefer','outlook.timezone=\"Pacific Standard Time\"')", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateCSharpSelectExpression()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users/{id}?$select=displayName,givenName,postalCode");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\n\t.Select(\"displayName,givenName,postalCode\")", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateCSharpFilterExpression()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users?$filter=startswith(givenName, 'J')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\n\t.Filter(\"startswith(givenName, 'J')\")", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateCSharpSearchExpression()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/people/?$search=\"Irene McGowen\"");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\n\t.Search(\"Irene McGowen\")", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateCSharpSkipExpression()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/events?$skip=20");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\n\t.Skip(20)", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateCSharpTopExpression()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/events?$top=5");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\n\t.Top(5)", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateCSharpRequestHeaderExpression()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users/{id}");
            requestPayload.Headers.Add("Prefer", "kenya-timezone");

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\n\t.Header(\"Prefer\",\"kenya-timezone\")", result);
        }

        [Fact]
        public void GenerateQuerySection_ShouldReturnAppropriateCSharpRequestHeaderExpressionWithEscapedDoubleQuotes()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            //no query present
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/events");
            requestPayload.Headers.Add("Prefer", "outlook.timezone=\"Pacific Standard Time\"");

            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var result = CommonGenerator.GenerateQuerySection(snippetModel, expressions);

            //Assert string is empty
            Assert.Equal("\n\t.Header(\"Prefer\",\"outlook.timezone=\\\"Pacific Standard Time\\\"\")", result);
        }
        #endregion

        #region Test GetEdmTypeFromIdentifier
        [Fact]
        public void GetClassNameFromIdentifier_ShouldReturnRootIdentifierOnFirstSearch()
        {
            //Arrange
            List<string> path = new List<string>
            {
                "people"//last item so search for classname
            };

            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/people");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var commonGenerator = new CommonGenerator(_edmModel);
            var result = commonGenerator.GetEdmTypeFromIdentifier(snippetModel.Segments.Last(), path);

            //Assert
            Assert.Equal("microsoft.graph.person", result.ToString());
        }

        [Fact]
        public void GetClassNameFromIdentifier_ShouldReturnParameterTypeForActionOrFunction()
        {
            //Arrange
            List<string> path = new List<string>
            {
                "message"//last item so search for classname
            };

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/sendMail");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var commonGenerator = new CommonGenerator(_edmModel);
            var result = commonGenerator.GetEdmTypeFromIdentifier(snippetModel.Segments.Last(), path);

            //Assert
            Assert.Equal("microsoft.graph.message", result.ToString());
        }

        [Fact]
        public void GetClassNameFromIdentifier_ShouldSearchForOneLevelNestedType()
        {
            //Arrange
            List<string> path = new List<string>
            {
                "messages",
                "toRecipients"//under the message entity there is a toRecipient entity
            };

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var commonGenerator = new CommonGenerator(_edmModel);
            var result = commonGenerator.GetEdmTypeFromIdentifier(snippetModel.Segments.Last(), path);

            //Assert
            Assert.Equal("microsoft.graph.recipient", result.ToString());

        }

        [Fact]
        public void GetClassNameFromIdentifier_ShouldFindSubclassProperties()
        {
            // Arrange
            List<string> path = new List<string>
            {
                "fileAttachment",
                "contentBytes"
            };

            // Act
            var commonGenerator = new CommonGenerator(_edmModel);
            var type = _edmModel.FindDeclaredType("microsoft.graph.attachment");
            var (contentBytesType, _) = commonGenerator.SearchForEdmType(type, path);

            // Assert
            Assert.Equal("Edm.Binary", contentBytesType.FullTypeName());
        }

        [Fact]
        public void GetClassNameFromIdentifier_ShouldSearchForOneLevelNestedType_2()
        {
            //Arrange
            List<string> path = new List<string>
            {
                "messages",
                "body"//under the message entity there is a toRecipient entity
            };

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var commonGenerator = new CommonGenerator(_edmModel);
            var result = commonGenerator.GetEdmTypeFromIdentifier(snippetModel.Segments.Last(), path);

            //Assert
            Assert.Equal("microsoft.graph.itemBody", result.ToString());

        }

        [Fact]
        public void GetClassNameFromIdentifier_ShouldSearchForTwoLevelNestedType()
        {
            //Arrange
            List<string> path = new List<string>
            {
                "messages",
                "toRecipients",//under the message entity there is a toRecipient entity
                "emailAddress"//under the toRecipient there is an email address property
            };

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var commonGenerator = new CommonGenerator(_edmModel);
            var result = commonGenerator.GetEdmTypeFromIdentifier(snippetModel.Segments.Last(), path);

            //Assert
            Assert.Equal("microsoft.graph.emailAddress", result.ToString());
        }

        [Fact]
        public void GetClassNameFromIdentifier_ShouldSearchForTwoLevelNestedType_2()
        {
            //Arrange
            List<string> path = new List<string>
            {
                "events",
                "attendees",//under the message entity there is a toRecipient entity
                "emailAddress"//under the toRecipient there is an email address property
            };

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/events");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Act
            var commonGenerator = new CommonGenerator(_edmModel);
            var result = commonGenerator.GetEdmTypeFromIdentifier(snippetModel.Segments.Last(), path);

            //Assert
            Assert.Equal("microsoft.graph.emailAddress", result.ToString());
        }
        #endregion

        #region Test EnsureVariableNameIsNotReserved
        [Fact]
        public void EnsureVariableNameIsNotReserved_AppendsUnderscoreOnJavascriptKeywords()
        {
            //Arrange
            LanguageExpressions expressions = new JavascriptExpressions();
            var keyword = "transient";
            //Act
            var result = CommonGenerator.EnsureVariableNameIsNotReserved(keyword, expressions);

            //Assert
            Assert.Equal("_transient", result);
        }

        [Fact]
        public void EnsureVariableNameIsNotReserved_AppendsTheAtSignOnCsharpKeywords()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            var keyword = "event";
            //Act
            var result = CommonGenerator.EnsureVariableNameIsNotReserved(keyword, expressions);

            //Assert
            Assert.Equal("@event", result);
        }

        [Fact]
        public void EnsureVariableNameIsNotReserved_DoesNotModifyVariableNamesIfNotReserved()
        {
            //Arrange
            LanguageExpressions expressions = new CSharpExpressions();
            var keyword = "people";
            //Act
            var result = CommonGenerator.EnsureVariableNameIsNotReserved(keyword, expressions);

            //Assert
            Assert.Equal("people", result);
        }
        #endregion

        #region Test GetParameterListFromOperationSegment

        [Fact]
        public void GetParameterListFromOperationSegment_ShouldReturnStringWithDoubleQuotesForOdataActionParameter()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/worksheets/{id|name}/range(address='A1:B2')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            var operationSegment = snippetModel.Segments.Last() as OperationSegment;

            //Act
            var result = CommonGenerator.GetParameterListFromOperationSegment(operationSegment, snippetModel);

            //Assert the string parameter is now double quoted
            Assert.Equal("\"A1:B2\"", result.First());
        }

        [Fact]
        public void GetParameterListFromOperationSegment_ShouldReturnParameterListOrderedByOptionality()
        {
            //Arrange
            const string jsonObject = "{\r\n  " +
                                      "\"address\": \"Sheet1!A1:D5\",\r\n" +
                                      "\"hasHeaders\": true\r\n" +
                                      "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/tables/add")
            {
                Content = new StringContent(jsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            var operationSegment = snippetModel.Segments.Last() as OperationSegment;

            //Act
            var result = CommonGenerator.GetParameterListFromOperationSegment(operationSegment, snippetModel).ToList();

            //Assert the parameters are ordered as expected
            Assert.Equal("hasHeaders", result[0]);
            Assert.Equal("address", result[1]);
        }

        [Fact]
        public void GetParameterListFromOperationSegment_ShouldReturnParameterListOrderedByMetadataReference()
        {
            //Arrange
            const string jsonObject = "{\r\n  " +
                                        "\"address\": \"Sheet1!A1:D5\",\r\n" +
                                        "\"hasHeaders\": true\r\n" +
                                      "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/tables/add")
            {
                Content = new StringContent(jsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            var operationSegment = snippetModel.Segments.Last() as OperationSegment;

            //Act
            var result = CommonGenerator.GetParameterListFromOperationSegment(operationSegment, snippetModel,"",false).ToList();

            //Assert the parameters are ordered as expected
            Assert.Equal("address", result[0]);
            Assert.Equal("hasHeaders", result[1]);
        }

        [Fact]
        public void GetParameterListFromOperationSegment_ShouldSetOptionalUnprovidedParameterToNull()
        {
            //Arrange
            const string jsonObject = "{\r\n  " +
                                      "\"hasHeaders\": true\r\n" +//we have not provided the optional address parameter
                                      "}";
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/drive/items/{id}/workbook/tables/add")
            {
                Content = new StringContent(jsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);
            var operationSegment = snippetModel.Segments.Last() as OperationSegment;

            //Act
            var result = CommonGenerator.GetParameterListFromOperationSegment(operationSegment, snippetModel).ToList();

            //Assert the parameters are ordered as expected
            Assert.Equal("hasHeaders", result[0]);
            Assert.Equal("null", result[1]);
        }

        #endregion
    }
}
