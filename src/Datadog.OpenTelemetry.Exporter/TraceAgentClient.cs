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
            _client.DefaultRequestHeaders.Add(AgentHttpHeaderNames.LanguageInterpreter, RuntimeInformationWrapper.Name);
            _client.DefaultRequestHeaders.Add(AgentHttpHeaderNames.LanguageVersion, RuntimeInformationWrapper.ProductVersion);
        }

        public async Task SendTracesAsync(IEnumerable<Span> spans)
        {
            if (spans == null)
            {
                throw new ArgumentNullException(nameof(spans));
            }

            var traces = (from span in spans
                          group span by span.TraceId
                          into g
                          select g.ToList()).ToList();

            byte[] bytes = MessagePackSerializer.Serialize(traces, _serializerOptions);

            using (HttpContent content = new ByteArrayContent(bytes))
            {
                content.Headers.ContentType = _mediaTypeHeaderValue;
                content.Headers.Add(AgentHttpHeaderNames.TraceCount, traces.Count.ToString(CultureInfo.InvariantCulture));
                HttpResponseMessage responseMessage = await _client.PostAsync(_tracesEndpoint, content).ConfigureAwait(false);
                responseMessage.EnsureSuccessStatusCode();
            }
        }
    }
}
