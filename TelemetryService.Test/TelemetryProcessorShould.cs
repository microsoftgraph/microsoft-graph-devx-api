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
            _telemetryProcessor.Process(requestTelemetry);
            var expected = "https://graphexplorerapi.azurewebsites.net/openapi?url=/users/{ID}&style=GEAutocomplete";

            // Assert
            var containsID = requestTelemetry.Url.ToString().Contains("{ID}");
            
            Assert.True(containsID);
            Assert.Equal(expected, requestTelemetry.Url.ToString());
        }

        [Fact]
        public void RedactEmailFromRequestUrl()
        {
            // Arrange
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Url = new Uri("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter(emailAddress eq 'MiriamG@M365x214355.onmicrosoft.com')");

            // Act
            _telemetryProcessor.Process(requestTelemetry);
            var expected = "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter(emailAddress eq '{redacted-email}')";
            

            // Assert
            Assert.Equal(expected, requestTelemetry.Url.ToString());
        }

        [Fact]
        public void RedactUsernameFromRequestUrl()
        {
            // Arrange
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Url = new Uri("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter(displayname eq 'Megan Bowen'");

            // Act
            _telemetryProcessor.Process(requestTelemetry);
            var expected = "https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter(displayname eq '{username}'";

            // Assert
            Assert.Equal(expected, requestTelemetry.Url.ToString());
        }
    }
}
