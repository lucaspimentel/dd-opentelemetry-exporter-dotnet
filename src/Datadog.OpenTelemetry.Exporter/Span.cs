using System;
using System.Collections.Generic;

namespace Datadog.OpenTelemetry.Exporter
{
    public class Span
    {
        public ulong TraceId { get; set; }

        public ulong SpanId { get; set; }

        public ulong? ParentSpanId { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public string OperationName { get; set; }

        public string ServiceName { get; set; }

        public string ResourceName { get; set; }

        public string Type { get; set; }

        public bool Error { get; set; }

        public Dictionary<string, string> Meta { get; } = new();

        public Dictionary<string, double> Metrics { get; } = new();
    }
}
