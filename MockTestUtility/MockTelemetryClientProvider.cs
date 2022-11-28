// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Channel;
using System.Collections.Concurrent;

namespace MockTestUtility
{
    public class MockTelemetryClientProvider
    {
        public static TelemetryClient MockTelemetryClient = InitializeMockTelemetryChannel();

        private static TelemetryClient InitializeMockTelemetryChannel()
        {
            // Application Insights TelemetryClient doesn't have an interface (and is sealed)
            // Spin -up our own homebrew mock object
            MockTelemetryChannel mockTelemetryChannel = new MockTelemetryChannel();
            TelemetryConfiguration mockTelemetryConfig = new TelemetryConfiguration
            {
                TelemetryChannel = mockTelemetryChannel,
                InstrumentationKey = Guid.NewGuid().ToString(),
            };

            TelemetryClient mockTelemetryClient = new TelemetryClient(mockTelemetryConfig);
            return mockTelemetryClient;
        }
    }

    internal class MockTelemetryChannel : ITelemetryChannel
    {
        public ConcurrentBag<ITelemetry> SentTelemtries = new ConcurrentBag<ITelemetry>();
        public bool IsFlushed
        {
            get; private set;
        }
        public bool? DeveloperMode
        {
            get; set;
        }
        public string EndpointAddress
        {
            get; set;
        }

        public void Send(ITelemetry item)
        {
            this.SentTelemtries.Add(item);
        }

        public void Flush()
        {
            this.IsFlushed = true;
        }

        public void Dispose()
        {

        }
    }
}
