// <copyright file="SpanMessagePackFormatter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MessagePack;
using MessagePack.Formatters;

namespace Datadog.OpenTelemetry.Exporter.MessagePack
{
    internal class SpanFormatter : IMessagePackFormatter<Span>
    {
        // top-level spans fields
        private readonly byte[] _traceIdBytes = Encoding.UTF8.GetBytes("trace_id");
        private readonly byte[] _spanIdBytes = Encoding.UTF8.GetBytes("span_id");
        private readonly byte[] _parentIdBytes = Encoding.UTF8.GetBytes("parent_id");
        private readonly byte[] _nameBytes = Encoding.UTF8.GetBytes("name");
        private readonly byte[] _resourceBytes = Encoding.UTF8.GetBytes("resource");
        private readonly byte[] _serviceBytes = Encoding.UTF8.GetBytes("service");
        private readonly byte[] _typeBytes = Encoding.UTF8.GetBytes("type");
        private readonly byte[] _startBytes = Encoding.UTF8.GetBytes("start");
        private readonly byte[] _durationBytes = Encoding.UTF8.GetBytes("duration");
        private readonly byte[] _errorBytes = Encoding.UTF8.GetBytes("error");
        private readonly byte[] _metaBytes = Encoding.UTF8.GetBytes("meta");
        private readonly byte[] _metricsBytes = Encoding.UTF8.GetBytes("metrics");

        // special tags: process id
        private readonly byte[] _processIdNameBytes = Encoding.UTF8.GetBytes("process_id");
        private readonly int _processIdValue;

        public SpanFormatter()
        {
            using (var process = Process.GetCurrentProcess())
            {
                _processIdValue = process.Id;
            }
        }

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

            writer.WriteString(_traceIdBytes);
            writer.WriteUInt64(span.TraceId);

            writer.WriteString(_spanIdBytes);
            writer.WriteUInt64(span.SpanId);

            writer.WriteString(_nameBytes);
            writer.Write(span.OperationName);

            writer.WriteString(_resourceBytes);
            writer.Write(span.ResourceName);

            writer.WriteString(_serviceBytes);
            writer.Write(span.ServiceName);

            writer.WriteString(_typeBytes);
            writer.Write(span.Type);

            writer.WriteString(_startBytes);
            writer.WriteInt64(ConversionHelper.ToUnixTimeNanoseconds(span.StartTime));

            writer.WriteString(_durationBytes);
            writer.WriteInt64(ConversionHelper.ToNanoseconds(span.Duration));

            if (span.ParentSpanId != null)
            {
                writer.WriteString(_parentIdBytes);
                writer.WriteUInt64((ulong)span.ParentSpanId);
            }

            if (span.Error)
            {
                writer.WriteString(_errorBytes);
                writer.WriteInt32(1);
            }

            // start string tags ("meta")
            writer.WriteString(_metaBytes);
            writer.WriteMapHeader(span.Meta.Count + 1);

            foreach (KeyValuePair<string, string> meta in span.Meta)
            {
                writer.Write(meta.Key);
                writer.Write(meta.Value);
            }

            // special tags: process id
            writer.WriteString(_processIdNameBytes);
            writer.Write(_processIdValue);

            // start numeric tags ("metrics")
            writer.WriteString(_metricsBytes);
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
