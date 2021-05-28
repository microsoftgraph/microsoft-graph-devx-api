// -------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

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
        private const string ConfigFilePath = ".\\TestFiles\\Permissions\\appsettings.json";

        public CustomPIIFilterShould()
        {
            _permissionsStore = PermissionStoreFactoryMock.GetPermissionStore(ConfigFilePath);
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
        // Valid query param & existing path in UriTemplateMatcher table
        [InlineData("/permissions?requestUrl=/groups/12345&method=GET",
                    "/permissions?requestUrl=/groups/{id}&method=GET")]
        // Non-existent path in UriTemplateMatcher table
        [InlineData("/permissions?requestUrl=/me/people/12345/drives&method=GET",
                    "/permissions?requestUrl=/me/people/****/drives&method=GET")]
        public void RedactNumberFromEventTelemetry(string requestPath, string expectedPath)
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
            _telemetryProcessor.Process(eventTelemetry);
            var expectedMessage = $"HTTP {httpMethod + expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Theory]
        // Valid query param & existing path in UriTemplateMatcher table
        [InlineData("/permissions?requestUrl=/groups/20463493-79c2-4116-b87b-a20d06242e6a&method=GET",
                    "/permissions?requestUrl=/groups/{id}&method=GET")]
        [InlineData("/permissions?requestUrl=/users/V+iW+VgtM0m2IPytHq76gA==/drives&method=GET",
                    "/permissions?requestUrl=/users/{id}/drives&method=GET")]
        // Non-existent path in UriTemplateMatcher table
        [InlineData("/permissions?requestUrl=/me/people/9f376303-1936-44a9-b4fd-7271483525bb/drives&method=GET",
                    "/permissions?requestUrl=/me/people/****/drives&method=GET")]
        public void RedactGUIDFromEventTelemetry(string requestPath, string expectedPath)
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
            _telemetryProcessor.Process(eventTelemetry);
            var expectedMessage = $"HTTP {httpMethod + expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Theory]
        // Non-existent path in UriTemplateMatcher table
        [InlineData("/openapi?url=/foobar?$filter(emailAddress eq 'MiriamG@M365x214355.onmicrosoft.com')",
                    "/openapi?url=/foobar")]
        // Valid query param & existing path in UriTemplateMatcher table
        [InlineData("/openapi?url=/users?$filter(emailAddress eq 'MiriamG@M365x214355.onmicrosoft.com')",
                    "/openapi?url=/users")]
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
            _telemetryProcessor.Process(eventTelemetry);
            var expectedMessage = $"HTTP {httpMethod} {expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Theory]
        // Non-existent path in UriTemplateMatcher table
        [InlineData("/permissions?requestUrl=/foobar?$filter(displayName eq 'Megan Bowen')",
                    "/permissions?requestUrl=/foobar")]
        // Valid query param & existing path in UriTemplateMatcher table
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
            _telemetryProcessor.Process(eventTelemetry);
            var expectedMessage = $"HTTP {httpMethod} {expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Theory]
        // Non-existent path in UriTemplateMatcher table
        [InlineData("/openapi?url=/foobar?$filter(firstName eq 'Megan')",
                    "/openapi?url=/foobar")]
        // Valid query param & existing path in UriTemplateMatcher table
        [InlineData("/openapi?url=/users?$filter(firstName eq 'Megan')",
                    "/openapi?url=/users")]
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
            _telemetryProcessor.Process(eventTelemetry);
            var expectedMessage = $"HTTP {httpMethod} {expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Theory]

        #region Paths available in the UriTemplateMatcher table

        [InlineData("/openapi?url=/users?$filter=emailAddress eq 'MiriamG@M365x214355.onmicrosoft.com'",
                    "/openapi?url=/users")]

        [InlineData("/permissions?requestUrl=/users('MeganB@M365x214355.onmicrosoft.com')",
                    "/permissions?requestUrl=/users/{id}")]

        [InlineData("/permissions?requestUrl=/users/1d201493-c13f-4e36-bd06-a20d06242e6a/calendar/events&method=GET",
                    "/permissions?requestUrl=/users/{id}/calendar/events&method=GET")]

        [InlineData("/openapi?url=/me/messages/123456/attachments?$search='5555551212'&openApiVersion=2&graphVersion=v1.0&format=yaml&style=Plain",
                    "/openapi?url=/me/messages/{id}/attachments&openApiVersion=2&graphVersion=v1.0&format=yaml&style=Plain")]
        #endregion

        #region Paths not available in the UriTemplateMatcher table

        [InlineData("/openapi?style=PowerShell&url=/randomPath?$orderby=from/emailAddress/MiriamG@M365x214355.onmicrosoft.com",
                    "/openapi?style=PowerShell&url=/randomPath")]

        [InlineData("/openapi?url=/students?$filter=givenName in ('Adele', 'Alex')&graphVersion=beta",
                    "/openapi?url=/students&graphVersion=beta")]

        [InlineData("/permissions?requesturl=/foobar?$search='Meghan'",
                    "/permissions?requesturl=/foobar")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/xyz?$filter=testProperty EQ 'arbitraryPropertyData'",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/xyz")]

        [InlineData("permissions?requestUrl=/me/people/9f376303-1936-44a9-b4fd-7271483525bb/drives&method=GET",
                    "permissions?requestUrl=/me/people/****/drives&method=GET")]

        [InlineData("/permissions?requestUrl=/me/people/12345/drives&method=GET",
                    "/permissions?requestUrl=/me/people/****/drives&method=GET")]

        [InlineData("/permissions?requesturl=/students('MeganB@M365x214355.onmicrosoft.com')",
                    "/permissions?requesturl=/students/'****'")]

        [InlineData("/permissions?requesturl=/students('MeganB@M365x214355.onmicrosoft.com')/classes",
                    "/permissions?requesturl=/students/'****'/classes")]

        #endregion

        #region Paths not requiring sanitization

        [InlineData("/samples/0277cf48-fd30-45fa-b2a7-a845f4f4e36c",
                    "/samples/0277cf48-fd30-45fa-b2a7-a845f4f4e36c")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/samples?search='hello world'",
                    "https://graphexplorerapi.azurewebsites.net/samples?search='hello world'")]

        #endregion

        public void SanitizeRequestTelemetry(string incomingUrl, string expectedUrl)
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
