using System;
using System.Diagnostics;
using OpenTelemetry;

namespace Datadog.OpenTelemetry.Exporter
{
    public sealed class DatadogSpanExporter : BaseExporter<Activity>
    {
        private readonly DatadogExporterOptions _options;
        private readonly SpanWriter _writer;
        private readonly string _defaultServiceName;

        public DatadogSpanExporter(DatadogExporterOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            var client = new TraceAgentClient(options.BaseEndpoint);
            _writer = new SpanWriter(client);

            _defaultServiceName = options.ServiceName ?? ProcessHelper.ProcessName;
        }

        protected override bool OnShutdown(int timeoutMilliseconds)
        {
            _writer.RequestStop();
            return base.OnShutdown(timeoutMilliseconds);
        }

        public override ExportResult Export(in Batch<Activity> batch)
        {
            using var scope = SuppressInstrumentationScope.Begin();

            try
            {
                foreach (Activity activity in batch)
                {
                    if (ShouldExport(activity))
                    {
                        Span span = ConvertToSpan(activity);
                        _writer.Add(span);
                    }
                }

                return ExportResult.Success;
            }
            catch
            {
                return ExportResult.Failure;
            }
        }

        private Span ConvertToSpan(Activity activity)
        {
            ConversionHelper.ToUInt64(activity.TraceId, out var upper, out var lower);

            var span = new Span
                       {
                           SpanId = ConversionHelper.ToUInt64(activity.SpanId),
                           TraceId = lower,
                           ParentSpanId = ConversionHelper.ToUInt64(activity.ParentSpanId),
                           Type = "custom",
                           ServiceName = _defaultServiceName,
                           OperationName = activity.OperationName,
                           StartTime = activity.StartTimeUtc,
                           Duration = activity.Duration,
                           Error = activity.Status == ActivityStatusCode.Error,
                           Meta =
                           {
                               ["span.kind"] = activity.Kind.ToString().ToLowerInvariant(),
                               ["_dd.p.tid"] = upper.ToString("x16")
                           }
                       };

            foreach (var attribute in activity.Tags)
            {
                if (attribute.Value != null)
                {
                    span.Meta[attribute.Key] = attribute.Value;
                }
            }

            span.ResourceName = $"{span.Meta["http.method"]} {activity.DisplayName}";
            return span;
        }

        private bool ShouldExport(Activity activity)
        {
            foreach (var tag in activity.Tags)
            {
                if (tag.Key == "http.url")
                {
                    return tag.Value == null || !tag.Value.StartsWith(_options.BaseEndpoint, StringComparison.OrdinalIgnoreCase);
                }
            }

            return true;
        }
    }
}
