using System;
using System.Collections.Generic;

namespace Datadog.OpenTelemetry.Exporter;

public class Span
{
    public ulong TraceId { get; init; }

    public ulong SpanId { get; init; }

    public ulong? ParentSpanId { get; init; }

    public DateTimeOffset StartTime { get; init; }

    public TimeSpan Duration { get; init; }

    public string? OperationName { get; init; }

    public string? ServiceName { get; init; }

    public string? ResourceName { get; init; }

    public string? Type { get; init; }

    public bool Error { get; init; }

    public Dictionary<string, string> Meta { get; } = new();

    public Dictionary<string, double> Metrics { get; } = new();
}
