// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights.DataContracts;
using System;
using TelemetryService;
using TelemetryService.Test;
using Xunit;

namespace Telemetry.Test
{
    public class CustomPIIFilterShould
    {
        private readonly CustomPIIFilter _telemetryProcessor;

        public CustomPIIFilterShould()
        {
            _telemetryProcessor = new CustomPIIFilter(new TestProcessorNext());
        }

        [Fact]
        public void ThrowsArgumentNullExceptionWithoutNextPocessor()
        {
            Assert.Throws<ArgumentNullException>(() => new CustomPIIFilter(null));
        }

        [Theory]
        [InlineData("/permissions?requestUrl=/users/20463493-79c2-4116-b87b-a20d06242e6a&method=GET",
                    "/permissions?requestUrl=/users/****&method=GET")]
        [InlineData("/permissions?requestUrl=/me/people/9f376303-1936-44a9-b4fd-7271483525bb/drives&method=GET",
                    "/permissions?requestUrl=/me/people/****/drives&method=GET")]
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

            //var expectedPath = "/permissions?requestUrl=/users/****&method=GET";
            var expectedMessage = $"HTTP {httpMethod + expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Fact]
        public void RedactEmailFromEventTelemetry()
        {
            // Arrange
            var httpMethod = "GET";
            var statusCode = "200";
            var elapsed = "5000";
            var requestPath = "openapi?url=/users?$filter(emailAddress eq 'MiriamG@M365x214355.onmicrosoft.com')";
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

            var expectedPath = "openapi?url=/users?$filter(emailAddress eq '****')";
            var expectedMessage = $"HTTP {httpMethod} {expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Fact]
        public void RedactUsernameFromEventTelemetry()
        {
            // Arrange
            var httpMethod = "GET";
            var statusCode = "200";
            var elapsed = "5000";
            var requestPath = "/openapi?url=/users?$filter(displayName eq 'Megan Bowen')";
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

            var expectedPath = "/openapi?url=/users?$filter(displayName eq ****)";
            var expectedMessage = $"HTTP {httpMethod} {expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Fact]
        public void RedactFirstNameFromEventTelemetry()
        {
            // Arrange
            var httpMethod = "GET";
            var statusCode = "200";
            var elapsed = "5000";
            var requestPath = "/openapi?url=/users?$filter(firstName eq 'Megan')";
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

            var expectedPath = "/openapi?url=/users?$filter(firstName eq ****)";
            var expectedMessage = $"HTTP {httpMethod} {expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Theory]
        [InlineData("https://graphexplorerapi.azurewebsites.net/permissions?requestUrl=/users?$filter(displayName eq 'Megan Bowen')",
                    "https://graphexplorerapi.azurewebsites.net/permissions?requestUrl=/users?$filter(displayName eq ****)")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=displayName%20eq%20%27Meghan%27",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=displayName eq ****")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=firstName eq 'Megan'",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=firstName eq ****")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=emailAddress eq 'MiriamG@M365x214355.onmicrosoft.com'",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=emailAddress eq '****'")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/permissions?requestUrl=/users/1d201493-c13f-4e36-bd06-a20d06242e6a&method=GET",
                    "https://graphexplorerapi.azurewebsites.net/permissions?requestUrl=/users/****&method=GET")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$orderby=from/emailAddress/MiriamG@M365x214355.onmicrosoft.com",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$orderby=from/emailAddress/****")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$expand=directreports($filter=firstName eq 'mary')",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$expand=directreports($filter=firstName eq ****)")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=givenName in ('Adele', 'Alex')",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=givenName in ****")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=startswith(givenName,'Alex')",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=startswith****")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/samples/0277cf48-fd30-45fa-b2a7-a845f4f4e36c",
                    "https://graphexplorerapi.azurewebsites.net/samples/0277cf48-fd30-45fa-b2a7-a845f4f4e36c")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$search='displayName:Meghan'",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$search='****'")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$search='Meghan'",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$search='****'")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$search='5555551212'",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$search='****'")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/samples?search='hello world'",
                    "https://graphexplorerapi.azurewebsites.net/samples?search='hello world'")]

        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/me/people/e3d0513b-449e-4198-ba6f-bd97ae7cae85",
                    "https://graphexplorerapi.azurewebsites.net/openapi?url=/me/people/****")]

        public void RedactUserPropertyFromRequestTelemetry(string incomingUrl, string expectedUrl)
        {
            // Arrange
            var request = new RequestTelemetry
            {
                Url = new Uri(incomingUrl)
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
