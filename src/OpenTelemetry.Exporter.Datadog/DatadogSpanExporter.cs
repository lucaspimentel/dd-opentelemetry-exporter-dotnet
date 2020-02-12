using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Export;

namespace OpenTelemetry.Exporter.Datadog
{
    public class DatadogSpanExporter : SpanExporter, IDisposable
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

        /// <summary>Shuts down exporter asynchronously.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        public override async Task ShutdownAsync(CancellationToken cancellationToken)
        {
            await _writer.StopAsync();
        }

        private SpanModel ConvertSpan(SpanData span)
        {
            var spanModel = new SpanModel
                            {
                                SpanId = Util.ToUInt64(span.Context.SpanId),
                                TraceId = Util.ToUInt64(span.Context.TraceId),
                                ParentId = Util.ToUInt64(span.ParentSpanId),
                                Type = "web",
                                ServiceName = _defaultServiceName,
                                OperationName = "web.request",
                                StartTime = span.StartTimestamp,
                                Duration = span.EndTimestamp - span.StartTimestamp,
                                Error = span.Status != Status.Ok,
                            };

            if (span.Kind != null)
            {
                spanModel.Tags["span.kind"] = span.Kind.Value.ToString().ToLowerInvariant();
            }

            foreach (var attribute in span.Attributes)
            {
                string value = attribute.Value?.ToString();

                if (!string.IsNullOrEmpty(value))
                {
                    spanModel.Tags[attribute.Key] = value;
                }
            }

            spanModel.ResourceName = $"{spanModel.Tags["http.method"]} {span.Name}";
            return spanModel;
        }

        private bool ShouldExport(SpanData span)
        {
            foreach (var attr in span.Attributes)
            {
                if (attr.Key == "http.url" && attr.Value != null && attr.Value.ToString().StartsWith(this._options.BaseEndpoint))
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

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
