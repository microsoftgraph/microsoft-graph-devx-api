// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using UtilityService;

namespace TelemetryClientWrapper
{
    /// <summary>
    /// Provides access to a singleton instance of a <see cref="Microsoft.ApplicationInsights.TelemetryClient"/>.
    /// </summary>
    public sealed class TelemetryClientSingleton
    {
        public static TelemetryClient TelemetryClient { get; private set; }

        public TelemetryClientSingleton(TelemetryClient telemetryClient)
        {
            UtilityFunctions.CheckArgumentNull(telemetryClient, nameof(telemetryClient));

            if (TelemetryClient == null)
            {
                TelemetryClient = telemetryClient;
            }
        }
    }
}
