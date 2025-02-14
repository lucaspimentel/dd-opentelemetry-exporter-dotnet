using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Datadog.OpenTelemetry.Exporter;

public static class DatadogExporterHelperExtensions
{
    /// <summary>
    /// Adds Datadog exporter to the TracerProvider.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
    /// <param name="configure">Exporter configuration options.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddDatadogExporter(
        this TracerProviderBuilder builder,
        Action<DatadogExporterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder is IDeferredTracerProviderBuilder deferredBuilder)
        {
            return deferredBuilder.Configure((sp, b) =>
            {
                var options = sp.GetService<IOptions<DatadogExporterOptions>>();
                var ddOptions = options?.Value ?? new DatadogExporterOptions();
                AddDatadogExporter(b, ddOptions, configure);
            });
        }

        return AddDatadogExporter(builder, new DatadogExporterOptions(), configure);
    }

    private static TracerProviderBuilder AddDatadogExporter(
        TracerProviderBuilder builder,
        DatadogExporterOptions options,
        Action<DatadogExporterOptions>? configure = null)
    {
        configure?.Invoke(options);
        //return builder.AddProcessor(new BatchActivityExportProcessor(new DatadogSpanExporter(options)));
        return builder.AddProcessor(new SimpleActivityExportProcessor(new DatadogSpanExporter(options)));
    }
}
