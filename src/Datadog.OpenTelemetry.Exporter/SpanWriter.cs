using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Datadog.OpenTelemetry.Exporter
{
    public class SpanWriter
    {
        private readonly TraceAgentClient _client;
        private readonly Task _loopTask;

        private ConcurrentBag<Span> _spans = new();
        private bool _enabled;

        public SpanWriter(TraceAgentClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _enabled = true;
            _loopTask = Task.Run(StartAsync);
        }

        private async Task StartAsync()
        {
            while (_enabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                // switch the queued spans with a new empty collection
                ConcurrentBag<Span> spans = Interlocked.Exchange(ref _spans, new ConcurrentBag<Span>());

                if (!spans.IsEmpty)
                {
                    await _client.SendTracesAsync(spans).ConfigureAwait(false);
                }
            }
        }

        public void Add(IEnumerable<Span> spans)
        {
            foreach (Span span in spans)
            {
                _spans.Add(span);
            }
        }

        public async Task StopAsync()
        {
            _enabled = false;
            await _loopTask;
        }
    }
}
