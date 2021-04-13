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
    public class TelemetryProcessorShould
    {
        private readonly TelemetryProcessor _telemetryProcessor;

        public TelemetryProcessorShould()
        {
            _telemetryProcessor = new TelemetryProcessor(new TestProcessorNext());
        }

        [Fact]
        public void ThrowsArgumentNullExceptionWithoutNextPocessor()
        {
            Assert.Throws<ArgumentNullException>(() => new TelemetryProcessor(null));
        }

        [Fact]
        public void SanitizeGUIDFromRequestUrl()
        {
            // Arrange
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Url = new Uri("https://graphexplorerapi.azurewebsites.net/openapi?url=/users/1d201493-c13f-4e36-bd06-a20d06242e6a&style=GEAutocomplete");

            // Act
            var expected = "https://graphexplorerapi.azurewebsites.net/openapi?url=/users/{ID}&style=GEAutocomplete";
            var sanitizedUrl = _telemetryProcessor.SanitizeUrl(requestTelemetry.Url);

            // Assert
            Assert.Equal(expected, sanitizedUrl.ToString());
        }

        [Fact]
        public void RedactEmailFromRequestUrl()
        {
            // Arrange
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Url = new Uri("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter(emailAddress eq 'MiriamG@M365x214355.onmicrosoft.com')");

            // Act
            var expected = "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter(emailAddress eq '{redacted-email}')";
            var sanitizedUrl = _telemetryProcessor.SanitizeUrl(requestTelemetry.Url);

            // Assert
            Assert.Equal(expected, sanitizedUrl.ToString());
        }

        [Fact]
        public void RedactUsernameFromRequestUrl()
        {
            // Arrange
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Url = new Uri("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter(displayname eq 'Megan Bowen'");

            // Act
            var expected = "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter(displayname eq '{username}'";
            var sanitizedUrl = _telemetryProcessor.SanitizeUrl(requestTelemetry.Url);

            // Assert
            Assert.Equal(expected, sanitizedUrl.ToString());
        }
    }
}
