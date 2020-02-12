using System;
using System.Collections.Generic;

namespace OpenTelemetry.Exporter.Datadog
{
    public class SpanModel
    {
        public ulong TraceId { get; set; }

        public ulong SpanId { get; set; }

        public ulong ParentId { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public string OperationName { get; set; }

        public string ServiceName { get; set; }

        public string ResourceName { get; set; }

        public string Type { get; set; }

        public bool Error { get; set; }

        public Dictionary<string, string> Tags { get; } = new Dictionary<string, string>();

        public Dictionary<string, double> Metrics { get; } = new Dictionary<string, double>();
    }
}
