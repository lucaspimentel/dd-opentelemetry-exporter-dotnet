using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MsgPack.Serialization;

namespace Datadog.OpenTelemetry.Exporter
{
    public class TraceAgentClient
    {
        private const string TracesPath = "/v0.4/traces";

        private readonly Uri _tracesEndpoint;
        private readonly HttpClient _client;

        public TraceAgentClient(Uri baseEndpoint)
        {
            _tracesEndpoint = new Uri(baseEndpoint, TracesPath);

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

            int traceIdCount = traces.Count;

            using (var content = new MsgPackContent<List<List<Span>>>(traces, SerializationContext))
            {
                content.Headers.Add(AgentHttpHeaderNames.TraceCount, traceIdCount.ToString(CultureInfo.InvariantCulture));

                HttpResponseMessage responseMessage = await _client.PostAsync(_tracesEndpoint, content)
                                                                   .ConfigureAwait(false);

                responseMessage.EnsureSuccessStatusCode();
            }
        }
    }
}
