using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Datadog.OpenTelemetry.Exporter.MessagePack;
using MessagePack;
using OpenTelemetry;

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
        _client.DefaultRequestHeaders.Add(Headers.Language, "dotnet-opentelemetry-exporter");
        _client.DefaultRequestHeaders.Add(Headers.LanguageInterpreter, ".NET");
        _client.DefaultRequestHeaders.Add(Headers.LanguageVersion, RuntimeVersion);
    }

    public async Task SendTracesAsync(IEnumerable<Span> spans)
    {
        ArgumentNullException.ThrowIfNull(spans);

        // TODO: avoid LINQ
        var traces = spans.GroupBy(span => span.TraceId).Select(g => g.ToList()).ToList();

        if (traces.Count == 0)
        {
            return;
        }

        // TODO: reuse buffer
        var bytes = MessagePackSerializer.Serialize(traces, _serializerOptions);

        using HttpContent content = new ByteArrayContent(bytes);
        content.Headers.ContentType = MediaTypeHeaderValue;
        content.Headers.Add(Headers.TraceCount, traces.Count.ToString(CultureInfo.InvariantCulture));

        var request = new HttpRequestMessage(HttpMethod.Post, _tracesEndpoint)
        {
            Content = content
        };

        // This tag will suppress the instrumentation of this specific request
        // request.Options.Set(new HttpRequestOptionsKey<bool>("otel.instrumentation.suppress"), true);

        using var scope = SuppressInstrumentationScope.Begin();

        var spanCount = traces.Sum(t => t.Count);
        Console.WriteLine($"[Exporter] Sending {traces.Count:N0} traces containing {spanCount:N0} spans total to {_tracesEndpoint}");

        try
        {

            using var responseMessage = await _client.SendAsync(request).ConfigureAwait(false);
            var responseContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

            Console.WriteLine($"[Exporter] Agent response: {responseMessage.StatusCode} {responseContent}");
            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Exporter] Failed to send traces: {e}");
        }
    }
}
