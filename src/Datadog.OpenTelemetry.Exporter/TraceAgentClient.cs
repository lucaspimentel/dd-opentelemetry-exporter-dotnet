using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Datadog.OpenTelemetry.Exporter.MessagePack;
using MessagePack;

namespace Datadog.OpenTelemetry.Exporter
{
    public class TraceAgentClient
    {
        private const string TracesPath = "/v0.4/traces";

        private readonly Uri _tracesEndpoint;
        private readonly HttpClient _client;

        public TraceAgentClient(string baseEndpoint)
        {
            _tracesEndpoint = new Uri(new Uri(baseEndpoint), TracesPath);

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add(AgentHttpHeaderNames.Language, ".NET");
            _client.DefaultRequestHeaders.Add(AgentHttpHeaderNames.LanguageInterpreter, FrameworkDescription.Instance.Name);
            _client.DefaultRequestHeaders.Add(AgentHttpHeaderNames.LanguageVersion, FrameworkDescription.Instance.ProductVersion);
        }

        public async Task SendTracesAsync(IEnumerable<Span> spanModels)
        {
            if (spanModels == null)
            {
                throw new ArgumentNullException(nameof(spanModels));
            }

            var traces = (from span in spanModels
                          group span by span.TraceId
                          into g
                          select g.ToList()).ToList();

            var serializerOptions = MessagePackSerializerOptions.Standard.WithResolver(SpanFormatterResolver.Instance);
            byte[] bytes = MessagePackSerializer.Serialize(traces, serializerOptions);

            using (HttpContent content = new ByteArrayContent(bytes))
            {
                content.Headers.Add(AgentHttpHeaderNames.TraceCount, traces.Count.ToString(CultureInfo.InvariantCulture));
                HttpResponseMessage responseMessage = await _client.PostAsync(_tracesEndpoint, content).ConfigureAwait(false);
                responseMessage.EnsureSuccessStatusCode();
            }
        }
    }
}
