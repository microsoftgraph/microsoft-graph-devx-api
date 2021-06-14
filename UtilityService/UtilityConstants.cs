// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights;

namespace UtilityService
{
    /// <summary>
    /// Provides commonly used reusable constants.
    /// </summary>
    public static class UtilityConstants
    {
        public const string TelemetryPropertyKey_Count = "Count";
        public const string TelemetryPropertyKey_Permissions = "Permissions";
        public const string TelemetryPropertyKey_Samples = "Samples";

        private static readonly object _telemetryClientSetLock = new();
        private static TelemetryClient _telemetryClient;
        public static TelemetryClient TelemetryClient
        {
            set
            {
                lock (_telemetryClientSetLock)
                {
                    if (_telemetryClient == null)
                    {
                        _telemetryClient = value;
                    }
                }
            }
        }
    }
}
