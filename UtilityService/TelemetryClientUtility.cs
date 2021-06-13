// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights;

namespace UtilityService
{
    /// <summary>
    /// Provides access to a singleton instance of a <see cref="Microsoft.ApplicationInsights.TelemetryClient"/>.
    /// </summary>
    public sealed class TelemetryClientUtility
    {
        public static TelemetryClient TelemetryClient { get; private set; }

        public TelemetryClientUtility(TelemetryClient telemetryClient)
        {
            TelemetryClient = telemetryClient;
        }
    }
}
