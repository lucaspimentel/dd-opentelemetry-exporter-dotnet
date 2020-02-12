using System.Net.Http;

namespace OpenTelemetry.Exporter.Datadog
{
    public class DatadogExporterOptions
    {
        public string BaseEndpoint { get; set; } = "http://localhost:8126";
    }
}
