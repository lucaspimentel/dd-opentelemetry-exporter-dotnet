// <copyright file="SpanMessagePackFormatter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>


using System;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;

namespace Datadog.OpenTelemetry.Exporter.MessagePack;

internal class SpanFormatter : IMessagePackFormatter<Span?>
{
    public void Serialize(ref MessagePackWriter writer, Span? span, MessagePackSerializerOptions options)
    {
        if (span is null)
        {
            writer.WriteNil();
            return;
        }

        var len = 10;

        if (span.ParentSpanId != null)
        {
            len++;
        }

        if (span.Error)
        {
            len++;
        }

        writer.WriteMapHeader(len);

        writer.WriteString("trace_id"u8);
        writer.WriteUInt64(span.TraceId);

        writer.WriteString("span_id"u8);
        writer.WriteUInt64(span.SpanId);

        writer.WriteString("name"u8);
        writer.Write(span.OperationName);

        writer.WriteString("resource"u8);
        writer.Write(span.ResourceName);

        writer.WriteString("service"u8);
        writer.Write(span.ServiceName);

        writer.WriteString("type"u8);
        writer.Write(span.Type);

        writer.WriteString("start"u8);
        writer.WriteInt64(ConversionHelper.ToUnixTimeNanoseconds(span.StartTime));

        writer.WriteString("duration"u8);
        writer.WriteInt64(ConversionHelper.ToNanoseconds(span.Duration));

        if (span.ParentSpanId != null)
        {
            writer.WriteString("parent_id"u8);
            writer.WriteUInt64((ulong)span.ParentSpanId);
        }

        if (span.Error)
        {
            writer.WriteString("error"u8);
            writer.WriteInt32(1);
        }

        // start string tags ("meta")
        writer.WriteString("meta"u8);
        writer.WriteMapHeader(span.Meta.Count);

        foreach (KeyValuePair<string, string> meta in span.Meta)
        {
            writer.Write(meta.Key);
            writer.Write(meta.Value);
        }

        // start numeric tags ("metrics")
        writer.WriteString("metrics"u8);
        writer.WriteMapHeader(span.Metrics.Count + 1);

        foreach (KeyValuePair<string, double> metric in span.Metrics)
        {
            writer.Write(metric.Key);
            writer.Write(metric.Value);
        }

        // special tag: process id
        writer.WriteString("process_id"u8);
        writer.Write((double)ProcessHelper.ProcessId);
    }

    public Span Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
