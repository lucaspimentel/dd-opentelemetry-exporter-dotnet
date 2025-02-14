// <copyright file="SpanFormatterResolver.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Datadog.OpenTelemetry.Exporter.MessagePack
{
    internal class SpanFormatterResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new SpanFormatterResolver();

        private static readonly IMessagePackFormatter<Span?> Formatter = new SpanFormatter();

        private SpanFormatterResolver()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            if (typeof(T) == typeof(Span))
            {
                return (IMessagePackFormatter<T>)Formatter;
            }

            return StandardResolver.Instance.GetFormatter<T>();
        }
    }
}
