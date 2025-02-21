using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Datadog.OpenTelemetry.Exporter.MessagePack;
using MessagePack;

namespace Datadog.OpenTelemetry.Exporter;

public class TraceAgentClient
{
    private static class Headers
    {
        public const string Language = "Datadog-Meta-Lang";

        /// <summary>
        /// The interpreter for the given language, e.g. ".NET Framework", ".NET Core", or ".NET".
        /// </summary>
        public const string LanguageInterpreter = "Datadog-Meta-Lang-Interpreter";

        /// <summary>
        /// The interpreter version for the given language, e.g. "8.0" for .NET 8
        /// </summary>
        public const string LanguageVersion = "Datadog-Meta-Lang-Version";

        /// <summary>
        /// The version of the tracer that generated this span.
        /// </summary>
        public const string TracerVersion = "Datadog-Meta-Tracer-Version";

        /// <summary>
        /// The number of unique traces per request.
        /// </summary>
        public const string TraceCount = "X-Datadog-Trace-Count";

        /// <summary>
        /// The id of the container where the traced application is running.
        /// </summary>
        public const string ContainerId = "Datadog-Container-ID";
    }

    private const string TracesPath = "/v0.4/traces";
    private static readonly string RuntimeVersion = Environment.Version.ToString();
    private static readonly MediaTypeHeaderValue MediaTypeHeaderValue = new("application/msgpack");

    private readonly MessagePackSerializerOptions? _serializerOptions = MessagePackSerializerOptions.Standard
                                                                                                    .WithResolver(SpanFormatterResolver.Instance)
                                                                                                    .WithOmitAssemblyVersion(true);

    private readonly Uri _tracesEndpoint;
    private readonly HttpClient _client;

    public TraceAgentClient(string baseEndpoint)
    {
        _tracesEndpoint = new Uri(new Uri(baseEndpoint), TracesPath);

        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add(Headers.Language, "dotnet");
        _client.DefaultRequestHeaders.Add(Headers.LanguageInterpreter, ".NET");
        _client.DefaultRequestHeaders.Add(Headers.LanguageVersion, RuntimeVersion);
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
        content.Headers.ContentType = MediaTypeHeaderValue;
        content.Headers.Add(Headers.TraceCount, traces.Count.ToString(CultureInfo.InvariantCulture));

        using var responseMessage = await _client.PostAsync(_tracesEndpoint, content).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();
    }
}
