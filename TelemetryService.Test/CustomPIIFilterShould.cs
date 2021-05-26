// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphExplorerPermissionsService.Interfaces;
using Microsoft.ApplicationInsights.DataContracts;
using MockTestUtility;
using System;
using TelemetryService;
using TelemetryService.Test;
using Xunit;

namespace Telemetry.Test
{
    public class CustomPIIFilterShould
    {
        private readonly CustomPIIFilter _telemetryProcessor;
        private readonly IPermissionsStore _permissionsStore;

        public CustomPIIFilterShould()
        {
            _permissionsStore = PermissionStoreFactoryMock.GetPermissionStore(".\\TestFiles\\Permissions\\appsettings.json");
            _telemetryProcessor = new CustomPIIFilter(new TestProcessorNext(), _permissionsStore);
        }

        [Fact]
        public void ThrowsArgumentNullExceptionIfNextPocessorArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new CustomPIIFilter(next: null, permissionsStore: _permissionsStore));
        }

        [Fact]
        public void ThrowsArgumentNullExceptionIfPermissionsStoreArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new CustomPIIFilter(next: _telemetryProcessor, permissionsStore: null));
        }

        [Theory]
        [InlineData("/permissions?requestUrl=/groups/20463493-79c2-4116-b87b-a20d06242e6a&method=GET",
                    "/permissions?requestUrl=/groups/{id}&method=GET")]
        [InlineData("/permissions?requestUrl=/me/people/9f376303-1936-44a9-b4fd-7271483525bb/drives&method=GET",
                    "/permissions?requestUrl=/me/people/****/drives&method=GET")]
        [InlineData("/permissions?requestUrl=/users/V+iW+VgtM0m2IPytHq76gA==/drives&method=GET",
                    "/permissions?requestUrl=/users/{id}/drives&method=GET")]
        public void SanitizeGUIDFromEventTelemetry(string requestPath, string expectedPath)
        {
            // Arrange
            var httpMethod = "GET";
            var statusCode = "200";
            var elapsed = "5000";
            var renderedMessage = $"HTTP {httpMethod + requestPath} responded {statusCode} in {elapsed} ms";

            var eventTelemetry = new EventTelemetry();
            eventTelemetry.Properties.Add("RequestPath", requestPath);
            eventTelemetry.Properties.Add("RequestMethod", httpMethod);
            eventTelemetry.Properties.Add("StatusCode", statusCode);
            eventTelemetry.Properties.Add("RenderedMessage", renderedMessage);

            // Act
            if (eventTelemetry.Properties.ContainsKey("RequestPath") && eventTelemetry.Properties.ContainsKey("RenderedMessage"))
            {
                _telemetryProcessor.Process(eventTelemetry);
            }

            var expectedMessage = $"HTTP {httpMethod + expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Theory]
        [InlineData("openapi?url=/foobar?$filter(emailAddress eq 'MiriamG@M365x214355.onmicrosoft.com')",
                    "openapi?url=/foobar?$filter(emailAddress eq ****)")]
        [InlineData("openapi?url=/users?$filter(emailAddress eq 'MiriamG@M365x214355.onmicrosoft.com')",
                    "openapi?url=/users")]
        public void RedactEmailFromEventTelemetry(string requestPath, string expectedPath)
        {
            // Arrange
            var httpMethod = "GET";
            var statusCode = "200";
            var elapsed = "5000";
            var renderedMessage = $"HTTP {httpMethod} {requestPath} responded {statusCode} in {elapsed} ms";

            var eventTelemetry = new EventTelemetry();
            eventTelemetry.Properties.Add("RequestPath", requestPath);
            eventTelemetry.Properties.Add("RequestMethod", httpMethod);
            eventTelemetry.Properties.Add("StatusCode", statusCode);
            eventTelemetry.Properties.Add("RenderedMessage", renderedMessage);

            // Act
            if (eventTelemetry.Properties.ContainsKey("RequestPath") && eventTelemetry.Properties.ContainsKey("RenderedMessage"))
            {
                _telemetryProcessor.Process(eventTelemetry);
            }

            var expectedMessage = $"HTTP {httpMethod} {expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Theory]
        [InlineData("/permissions?requestUrl=/foobar?$filter(displayName eq 'Megan Bowen')",
                    "/permissions?requestUrl=/foobar?$filter(displayName eq ****)")]
        [InlineData("/permissions?requestUrl=/users?$filter(displayName eq 'Megan Bowen')",
                    "/permissions?requestUrl=/users")]
        public void RedactUsernameFromEventTelemetry(string requestPath, string expectedPath)
        {
            // Arrange
            var httpMethod = "GET";
            var statusCode = "200";
            var elapsed = "5000";
            var renderedMessage = $"HTTP {httpMethod} {requestPath} responded {statusCode} in {elapsed} ms";

            var eventTelemetry = new EventTelemetry();
            eventTelemetry.Properties.Add("RequestPath", requestPath);
            eventTelemetry.Properties.Add("RequestMethod", httpMethod);
            eventTelemetry.Properties.Add("StatusCode", statusCode);
            eventTelemetry.Properties.Add("RenderedMessage", renderedMessage);

            // Act
            if (eventTelemetry.Properties.ContainsKey("RequestPath") && eventTelemetry.Properties.ContainsKey("RenderedMessage"))
            {
                _telemetryProcessor.Process(eventTelemetry);
            }

            var expectedMessage = $"HTTP {httpMethod} {expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Theory]
        [InlineData("/openapi?url=/foobar?$filter(firstName eq 'Megan')", "/openapi?url=/foobar?$filter(firstName eq ****)")]
        [InlineData("/openapi?url=/users?$filter(firstName eq 'Megan')", "/openapi?url=/users")]
        public void RedactFirstNameFromEventTelemetry(string requestPath, string expectedPath)
        {
            // Arrange
            var httpMethod = "GET";
            var statusCode = "200";
            var elapsed = "5000";
            var renderedMessage = $"HTTP {httpMethod} {requestPath} responded {statusCode} in {elapsed} ms";

            var eventTelemetry = new EventTelemetry();
            eventTelemetry.Properties.Add("RequestPath", requestPath);
            eventTelemetry.Properties.Add("RequestMethod", httpMethod);
            eventTelemetry.Properties.Add("StatusCode", statusCode);
            eventTelemetry.Properties.Add("RenderedMessage", renderedMessage);

            // Act
            if (eventTelemetry.Properties.ContainsKey("RequestPath") && eventTelemetry.Properties.ContainsKey("RenderedMessage"))
            {
                _telemetryProcessor.Process(eventTelemetry);
            }

            var expectedMessage = $"HTTP {httpMethod} {expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Theory]

        #region Paths available in the UriTemplateMatcher table

        [InlineData("/openapi?url=/users?$filter=emailAddress eq 'MiriamG@M365x214355.onmicrosoft.com'",
                    "/openapi?url=/users")]

        [InlineData("/permissions?requestUrl=/users/1d201493-c13f-4e36-bd06-a20d06242e6a/calendar/events&method=GET",
                    "/permissions?requestUrl=/users/{id}/calendar/events&method=GET")]

        [InlineData("/openapi?url=/me/messages/123456/attachments?$search='5555551212'",
                    "/openapi?url=/me/messages/{id}/attachments")]
        #endregion

        #region Non-Graph paths / paths not available in the UriTemplateMatcher table

        [InlineData("/permissions?requestUrl=/abc?$filter(displayName eQ 'Megan Bowen')",
                    "/permissions?requestUrl=/abc?$filter(displayName eQ ****)")]

        [InlineData("/openapi?url=/xyz?$filter=displayName%20Eq%20%27Meghan%27",
                    "/openapi?url=/xyz?$filter=displayName Eq ****")]

        [InlineData("/openapi?url=/randomPath?$orderby=from/emailAddress/MiriamG@M365x214355.onmicrosoft.com",
                    "/openapi?url=/randomPath?$orderby=from/emailAddress/****")]

        [InlineData("/openapi?url=/students?$filter=givenName in ('Adele', 'Alex')",
                    "/openapi?url=/students?$filter=givenName in ****")]

        [InlineData("/openapi?url=/students?$filter=startswith(displayName, 'a')&$count=true&$top=1&$orderby=displayName",
                    "/openapi?url=/students?$filter=startswith****&$count=true&$top=1&$orderby=displayName")]

        [InlineData("/openapi?url=/foobar?$search='Meghan'",
                    "/openapi?url=/foobar?$search=****")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/xyz?$filter=testProperty EQ 'arbitraryPropertyData'",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/xyz?$filter=testProperty EQ ****")]

        #endregion

        #region Paths not requiring sanitization

        [InlineData("/samples/0277cf48-fd30-45fa-b2a7-a845f4f4e36c",
                    "/samples/0277cf48-fd30-45fa-b2a7-a845f4f4e36c")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/samples?search='hello world'",
                    "https://graphexplorerapi.azurewebsites.net/samples?search='hello world'")]

        #endregion

        public void SanitizeODataQueryOptionsFromRequestTelemetry(string incomingUrl, string expectedUrl)
        {
            // Arrange
            var request = new RequestTelemetry
            {
                Url = new Uri(incomingUrl, UriKind.RelativeOrAbsolute)
            };

            // Act
            _telemetryProcessor.Process(request);

            // Assert
            Assert.Equal(expectedUrl, request.Url.ToString());
        }

        [Theory]
        [InlineData("Fetching 'DelegatedWork' permissions for url '/users/9f376303-1936-44a9-b4fd-7271483525bb/drives' and method 'GET'",
            "Fetching 'DelegatedWork' permissions for url '/users/****/drives' and method 'GET'")]
        [InlineData("Fetching 'DelegatedWork' permissions for url '/users?$expand=directreports($filter=firstName eq 'mary')' and method 'GET'",
            "Fetching 'DelegatedWork' permissions for url '/users?$expand=directreports($filter=firstName eq ****)' and method 'GET'")]
        public void RedactPIIFromTraceTelemetry(string incomingMsg, string expectedMsg)
        {
            // Arrange
            var trace = new TraceTelemetry
            {
                Message = incomingMsg
            };

            // Act
            _telemetryProcessor.Process(trace);

            // Assert
            Assert.Equal(expectedMsg, trace.Message);
        }
    }
}
