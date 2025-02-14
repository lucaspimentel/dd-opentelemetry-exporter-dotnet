namespace Datadog.OpenTelemetry.Exporter;

public class DatadogExporterOptions
{
    public string? ServiceName { get; set; }

    public string BaseEndpoint { get; set; } = "http://localhost:8126";
}
