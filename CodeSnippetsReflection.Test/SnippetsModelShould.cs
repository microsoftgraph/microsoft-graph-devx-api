using System.Linq;
using System.Net.Http;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Xunit;

namespace CodeSnippetsReflection.Test
{
    public class SnippetsModelShould
    {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        private readonly IEdmModel _edmModel = CsdlReader.Parse(XmlReader.Create(ServiceRootUrl + "/$metadata"));

        [Fact]
        public void PopulateExpandListField()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/drive/root?$expand=children");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

             //Assert
            Assert.Equal("children", snippetModel.ExpandFieldList.First());
        }

        [Fact]
        public void PopulateFilterListField()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users?$filter=startswith(displayName,'J')");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert
            Assert.Equal("startswith(displayName,'J')", snippetModel.FilterFieldList.First());
        }

        [Fact]
        public void PopulateOrderByListField()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users?$orderby=displayName");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert
            Assert.Equal("displayName", snippetModel.OrderByFieldList.First());
        }

        [Fact]
        public void PopulateSelectListField()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/messages?$select=from,subject");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert
            Assert.Equal("from", snippetModel.SelectFieldList[0]);
            Assert.Equal("subject", snippetModel.SelectFieldList[1]);
        }

        [Fact]
        public void PopulateSearchExpression()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/people/?$search=\"Irene McGowen\"");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert
            Assert.Equal("Irene McGowen", snippetModel.SearchExpression);
        }

        [Fact]
        public void PopulateHeadersCollection()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/people/?$search=\"Irene McGowen\"");
            requestPayload.Headers.Add("Prefer", "kenya-timezone");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert
            Assert.Equal("Irene McGowen", snippetModel.SearchExpression);
            Assert.Equal("Prefer", snippetModel.RequestHeaders.First().Key);
            Assert.Equal("kenya-timezone", snippetModel.RequestHeaders.First().Value.First());
        }

        [Fact]
        public void PopulateRequestBody()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/people/")
            {
                Content = new StringContent("This is just a test")
            };

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert that the request body is empty
            Assert.Equal("This is just a test", snippetModel.RequestBody);
        }

        [Fact]
        public void PopulateEmptyStringOnEmptyRequestBody()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/people/");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert that the request body is empty
            Assert.Equal("",snippetModel.RequestBody);
        }



    }
}
