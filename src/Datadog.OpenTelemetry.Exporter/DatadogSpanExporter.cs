using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry;

namespace Datadog.OpenTelemetry.Exporter
{
    public class DatadogSpanExporter : BaseExporter<Activity>, IDisposable
    {
        private readonly DatadogExporterOptions _options;
        private readonly SpanWriter _writer;
        private readonly string _defaultServiceName;

        public DatadogSpanExporter(DatadogExporterOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            var client = new TraceAgentClient(new Uri(options.BaseEndpoint));
            _writer = new SpanWriter(client);

            _defaultServiceName = Assembly.GetEntryAssembly()?.GetName().Name ??
                                  Process.GetCurrentProcess().ProcessName;
        }

        protected override bool OnShutdown(int timeoutMilliseconds)
        {
            return base.OnShutdown(timeoutMilliseconds);
        }

        /// <summary>Exports batch of spans asynchronously.</summary>
        /// <param name="batch">Batch of spans to export.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of export.</returns>
        public override Task<ExportResult> ExportAsync(IEnumerable<SpanData> batch, CancellationToken cancellationToken)
        {
            try
            {
                var spans = batch.Where(ShouldExport)
                                 .Select(ConvertSpan)
                                 .ToList();

                _writer.Add(spans);
                return Task.FromResult(ExportResult.Success);
            }
            catch
            {
                return Task.FromResult(ExportResult.FailedNotRetryable);
            }
        }

        private Span ConvertSpan(Activity activity)
        {
            var spanModel = new Span
                            {
                                SpanId = Util.ToUInt64(activity.SpanId),
                                TraceId = Util.ToUInt64(activity.TraceId),
                                ParentId = Util.ToUInt64(activity.ParentSpanId),
                                Type = "web",
                                ServiceName = _defaultServiceName,
                                OperationName = "web.request",
                                StartTime = activity.StartTimestamp,
                                Duration = activity.EndTimestamp - activity.StartTimestamp,
                                Error = !activity.Status.IsOk
                            };

            if (activity.Kind != null)
            {
                spanModel.Tags["span.kind"] = activity.Kind.Value.ToString().ToLowerInvariant();
            }

            foreach (var attribute in activity.Attributes)
            {
                string value = attribute.Value?.ToString();

                if (!string.IsNullOrEmpty(value))
                {
                    spanModel.Tags[attribute.Key] = value;
                }
            }

            spanModel.ResourceName = $"{spanModel.Tags["http.method"]} {activity.Name}";
            return spanModel;
        }

        private bool ShouldExport(Activity activity)
        {
            foreach ((string key, string? value) in activity.Tags)
            {
                if (key == "http.url" && value?.StartsWith(_options.BaseEndpoint) == true)
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.ShutdownAsync(CancellationToken.None)
                    .ContinueWith(_ => { })
                    .Wait();
            }
        }

        public override ExportResult Export(in Batch<Activity> batch)
        {

        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
