using Microsoft.ApplicationInsights;

namespace TelemetryClientFactory
{
    public sealed class TelemetryClientSingleton
    {
        public static TelemetryClient TelemetryClient { get; private set; }

        public TelemetryClientSingleton(TelemetryClient telemetryClient)
        {
            TelemetryClient = telemetryClient;
        }
    }
}
