using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Datadog.OpenTelemetry.Exporter.MessagePack;
using MessagePack;

namespace Datadog.OpenTelemetry.Exporter
{
    public class TraceAgentClient
    {
        private static readonly string RuntimeVersion = Environment.Version.ToString();
        private const string TracesPath = "/v0.4/traces";

        private readonly MessagePackSerializerOptions? _serializerOptions = MessagePackSerializerOptions.Standard
                                                                                                        .WithResolver(SpanFormatterResolver.Instance)
                                                                                                        .WithOmitAssemblyVersion(true);

        private readonly MediaTypeHeaderValue _mediaTypeHeaderValue = new("application/msgpack");
        private readonly Uri _tracesEndpoint;
        private readonly HttpClient _client;

        public TraceAgentClient(string baseEndpoint)
        {
            _tracesEndpoint = new Uri(new Uri(baseEndpoint), TracesPath);

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add(AgentHttpHeaderNames.Language, "dotnet");
            _client.DefaultRequestHeaders.Add(AgentHttpHeaderNames.LanguageInterpreter, ".NET");
            _client.DefaultRequestHeaders.Add(AgentHttpHeaderNames.LanguageVersion, RuntimeVersion);
        }

        public async Task SendTracesAsync(IEnumerable<Span> spans)
        {
            ArgumentNullException.ThrowIfNull(spans);

            var traces = spans.GroupBy(span => span.TraceId).Select(g => g.ToList()).ToList();

            if (traces.Count == 0)
            {
                return;
            }

            var bytes = MessagePackSerializer.Serialize(traces, _serializerOptions);

            using HttpContent content = new ByteArrayContent(bytes);
            content.Headers.ContentType = _mediaTypeHeaderValue;
            content.Headers.Add(AgentHttpHeaderNames.TraceCount, traces.Count.ToString(CultureInfo.InvariantCulture));

            using var responseMessage = await _client.PostAsync(_tracesEndpoint, content).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();
        }
    }
}
