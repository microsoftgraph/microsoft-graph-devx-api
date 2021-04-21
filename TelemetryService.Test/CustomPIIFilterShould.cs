// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphWebApi.Telemetry;
using Microsoft.ApplicationInsights.DataContracts;
using System;
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

        [Fact]
        public void SanitizeGUIDFromRequestUrl()
        {
            // Arrange
            var httpMethod = "GET";
            var statusCode = "200";
            var elapsed = "5000";
            var requestPath = "/permissions?requestUrl=/users/1d201493-c13f-4e36-bd06-a20d06242e6a&method=GET";
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

            var expectedPath = "/permissions?requestUrl=/users/****&method=GET";
            var expectedMessage = $"HTTP {httpMethod + expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Fact]
        public void RedactEmailFromRequestUrl()
        {
            // Arrange
            var httpMethod = "GET";
            var statusCode = "200";
            var elapsed = "5000";
            var requestPath = "openapi?url=/users?$filter(emailAddress eq 'MiriamG@M365x214355.onmicrosoft.com')";
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

            var expectedPath = "openapi?url=/users?$filter(emailAddress eq '****')";
            var expectedMessage = $"HTTP {httpMethod + expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Fact]
        public void RedactUsernameFromRequestUrl()
        {
            // Arrange
            var httpMethod = "GET";
            var statusCode = "200";
            var elapsed = "5000";
            var requestPath = "/openapi?url=/users?$filter(displayname eq 'Megan Bowen')";
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

            var expectedPath = "/openapi?url=/users?$filter(displayname eq '****')";
            var expectedMessage = $"HTTP {httpMethod + expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }

        [Fact]
        public void RedactFirstNameFromRequestUrl()
        {
            // Arrange
            var httpMethod = "GET";
            var statusCode = "200";
            var elapsed = "5000";
            var requestPath = "/openapi?url=/users?$filter(firstName eq 'Megan')";
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

            var expectedPath = "/openapi?url=/users?$filter(firstName eq '****')";
            var expectedMessage = $"HTTP {httpMethod + expectedPath} responded {statusCode} in {elapsed} ms";

            // Assert
            Assert.Equal(expectedPath, eventTelemetry.Properties["RequestPath"]);
            Assert.Equal(expectedMessage, eventTelemetry.Properties["RenderedMessage"]);
        }
    }
}
