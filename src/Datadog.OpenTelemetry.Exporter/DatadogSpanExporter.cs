using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Datadog.OpenTelemetry.Exporter.Util;
using OpenTelemetry;

namespace Datadog.OpenTelemetry.Exporter;

public sealed class DatadogSpanExporter : BaseExporter<Activity>
{
    private readonly DatadogExporterOptions _options;
    private readonly string? _serviceName;
    private readonly SpanWriter _writer;

    public DatadogSpanExporter(DatadogExporterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _serviceName = options.ServiceName;

        var client = new TraceAgentClient(options.BaseEndpoint);
        _writer = new SpanWriter(client);
    }

    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        _writer.RequestStop();
        return base.OnShutdown(timeoutMilliseconds);
    }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        Console.WriteLine("[Exporter] Exporting batch of {0:N0} spans", batch.Count);
        var error = false;

        foreach (var activity in batch)
        {
            if (ShouldExport(activity))
            {
                try
                {
                    var span = ConvertToSpan(activity);
                    _writer.Add(span);
                }
                catch (Exception e)
                {
                    error = true;
                    Console.WriteLine("[Exporter] Error converting span: {0}", e);
                }
            }
        }

        return error ? ExportResult.Failure : ExportResult.Success;
    }

    private Span ConvertToSpan(Activity activity)
    {
        var (traceIdUpper, traceIdLower) = ConversionHelper.ToUInt64(activity.TraceId);

        var span = new Span
        {
            TraceId = traceIdLower,
            SpanId = ConversionHelper.ToUInt64(activity.SpanId),
            ParentSpanId = ConversionHelper.ToUInt64(activity.ParentSpanId),
            Type = "serverless",
            ServiceName = _serviceName,
            OperationName = activity.OperationName,
            ResourceName = activity.DisplayName,
            StartTime = activity.StartTimeUtc,
            Duration = activity.Duration,
            Error = activity.Status == ActivityStatusCode.Error,
            Meta =
            {
                ["_dd.p.tid"] = traceIdUpper.ToString("x16"),
                ["span.kind"] = activity.Kind.ToString().ToLowerInvariant(),
                ["activity_source"] = activity.Source.Name
            }
        };

        foreach (var (key, value) in activity.EnumerateTagObjects())
        {
            switch (value)
            {
                case string s:
                    switch (key)
                    {
                        case "exception.type":
                            span.Meta["error.type"] = s;
                            break;
                        case "exception.message":
                            span.Meta["error.msg"] = s;
                            break;
                        case "exception.stacktrace":
                            span.Meta["error.stack"] = s;
                            break;
                        default:
                            span.Meta[key] = s;
                            break;
                    }

                    break;
                case int i:
                    span.Metrics[key] = i;
                    break;
                case double d:
                    span.Metrics[key] = d;
                    break;
                case bool b:
                    span.Metrics[key] = b ? 1 : 0;
                    break;
            }
        }

        return span;
    }

    private bool ShouldExport(Activity activity)
    {
        foreach (var tag in activity.Tags)
        {
            if (tag.Key == "url.full")
            {
                return tag.Value == null || !tag.Value.StartsWith(_options.BaseEndpoint, StringComparison.OrdinalIgnoreCase);
            }
        }

        return true;
    }
}
