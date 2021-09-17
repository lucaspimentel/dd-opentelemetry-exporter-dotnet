// <copyright file="SpanMessagePackFormatter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>


using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;
using MessagePack.Formatters;

namespace Datadog.OpenTelemetry.Exporter.MessagePack
{
    internal class SpanMessagePackFormatter : IMessagePackFormatter<Span>
    {
        private static readonly byte[] TraceIdBytes = Encoding.UTF8.GetBytes("trace_id");
        private static readonly byte[] SpanIdBytes = Encoding.UTF8.GetBytes("span_id");
        private static readonly byte[] NameBytes = Encoding.UTF8.GetBytes("name");
        private static readonly byte[] ResourceBytes = Encoding.UTF8.GetBytes("resource");
        private static readonly byte[] ServiceBytes = Encoding.UTF8.GetBytes("service");
        private static readonly byte[] TypeBytes = Encoding.UTF8.GetBytes("type");
        private static readonly byte[] StartBytes = Encoding.UTF8.GetBytes("start");
        private static readonly byte[] DurationBytes = Encoding.UTF8.GetBytes("duration");
        private static readonly byte[] ParentIdBytes = Encoding.UTF8.GetBytes("parent_id");
        private static readonly byte[] ErrorBytes = Encoding.UTF8.GetBytes("error");
        private static readonly byte[] MetaBytes = Encoding.UTF8.GetBytes("meta");

        private static readonly byte[] MetricsBytes = Encoding.UTF8.GetBytes("metrics");
        // private static readonly byte[] OriginBytes = Encoding.UTF8.GetBytes(Trace.Tags.Origin);
        // private static readonly byte[] RuntimeIdBytes = Encoding.UTF8.GetBytes(Trace.Tags.RuntimeId);
        // private static readonly byte[] RuntimeIdValueBytes = Encoding.UTF8.GetBytes(Tracer.RuntimeId);

        public void Serialize(ref MessagePackWriter writer, Span span, MessagePackSerializerOptions options)
        {
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

            writer.WriteString(TraceIdBytes);
            writer.WriteUInt64(span.TraceId);

            writer.WriteString(SpanIdBytes);
            writer.WriteUInt64(span.SpanId);

            writer.WriteString(NameBytes);
            writer.Write(span.OperationName);

            writer.WriteString(ResourceBytes);
            writer.Write(span.ResourceName);

            writer.WriteString(ServiceBytes);
            writer.Write(span.ServiceName);

            writer.WriteString(TypeBytes);
            writer.Write(span.Type);

            writer.WriteString(StartBytes);
            writer.WriteInt64(ConversionHelper.ToUnixTimeNanoseconds(span.StartTime));

            writer.WriteString(DurationBytes);
            writer.WriteInt64(ConversionHelper.ToNanoseconds(span.Duration));

            if (span.ParentSpanId != null)
            {
                writer.WriteString(ParentIdBytes);
                writer.WriteUInt64((ulong)span.ParentSpanId);
            }

            if (span.Error)
            {
                writer.WriteString(ErrorBytes);
                writer.WriteInt8(1);
            }

            writer.WriteString(MetaBytes);
            writer.WriteMapHeader(span.Meta.Count);

            foreach (KeyValuePair<string, string> meta in span.Meta)
            {
                writer.Write(meta.Key);
                writer.Write(meta.Value);
            }

            writer.WriteString(MetricsBytes);
            writer.WriteMapHeader(span.Metrics.Count);

            foreach (KeyValuePair<string, double> metric in span.Metrics)
            {
                writer.Write(metric.Key);
                writer.Write(metric.Value);
            }
        }

        public Span Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
