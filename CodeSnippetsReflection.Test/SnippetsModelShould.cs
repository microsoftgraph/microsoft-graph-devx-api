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
        public void PopulateExpandFieldExpression()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/drive/root?$expand=children");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

             //Assert
            Assert.Equal("children", snippetModel.ExpandFieldExpression);
        }

        [Fact]
        public void PopulateExpandFieldExpressionWithNestedQuery()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/drive/root?$expand=children($select=id,name)");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert
            Assert.Equal("children($select=id,name)", snippetModel.ExpandFieldExpression);
        }

        [Fact]
        public void PopulateExpandFieldExpressionFromMultipleQueryString()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users?$select=id,displayName,mail&$expand=extensions");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert
            Assert.Equal("extensions", snippetModel.ExpandFieldExpression);

        }

        [Fact]
        public void PopulateExpandFieldExpressionFromReversedMultipleQueryString()
        {
            //Reverse the query structure should still work
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users?$expand=extensions&$select=id,displayName,mail");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert
            Assert.Equal("extensions", snippetModel.ExpandFieldExpression);
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

        [Fact]
        public void PopulatesCustomQueryOptions()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/calendar/calendarView?startDateTime=2017-01-01T19:00:00.0000000&endDateTime=2017-01-07T19:00:00.0000000");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert that the keys and values are as expected
            Assert.Equal("startDateTime", snippetModel.CustomQueryOptions.First().Key);
            Assert.Equal("2017-01-01T19:00:00.0000000", snippetModel.CustomQueryOptions.First().Value);
            Assert.Equal("endDateTime", snippetModel.CustomQueryOptions.Last().Key);
            Assert.Equal("2017-01-07T19:00:00.0000000", snippetModel.CustomQueryOptions.Last().Value);
        }

        #region Test ResponseVariableNames
        [Fact]
        public void SetAppropriateVariableNameForPeopleEntity()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/people/");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert that the variable name is "people" for a collection
            Assert.Equal("people", snippetModel.ResponseVariableName);
        }

        [Fact]
        public void SetAppropriateVariableNameForUsersList()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert that the variable name is "users" for the users collection
            Assert.Equal("users", snippetModel.ResponseVariableName);
        }


        [Fact]
        public void SetAppropriateVariableNameForSingleUser()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users/{id|userPrincipalName}");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert that the variable name is "user" for the single user.
            Assert.Equal("user", snippetModel.ResponseVariableName);

        }

        [Fact]
        public void SetAppropriateVariableNameForChildrenItemsInDrive()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0//drives/{drive-id}/items/{item-id}/children");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert that the variable name is "children" for the collection returned
            Assert.Equal("children", snippetModel.ResponseVariableName);
        }

        [Fact]
        public void SetAppropriateVariableNameForCalendarGroups()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/calendarGroups");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert that the variable name is "calendarGroups" for the collection returned
            Assert.Equal("calendarGroups", snippetModel.ResponseVariableName);
        }

        [Fact]
        public void SetAppropriateVariableNameForEventCreate()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/events");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert that the variable name is "event" (singular) as we are making a post call to create an entity
            Assert.Equal("event", snippetModel.ResponseVariableName);
        }

        [Fact]
        public void SetsAppropriateVariableNameForEventUpdate()
        {
            //Arrange
            var requestPayload = new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/groups/{id}/events/{id}");

            //Act
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _edmModel);

            //Assert that the variable name is "event" for an event update
            Assert.Equal("event", snippetModel.ResponseVariableName);
        }

        #endregion
    }
}
