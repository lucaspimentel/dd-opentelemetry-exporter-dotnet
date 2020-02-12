using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTelemetry.Exporter.Datadog
{
    public class SpanWriter
    {
        private readonly TraceAgentClient _client;
        private readonly Task _loopTask;

        private ConcurrentBag<SpanModel> _spans = new ConcurrentBag<SpanModel>();
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
                ConcurrentBag<SpanModel> spans = Interlocked.Exchange(ref _spans, new ConcurrentBag<SpanModel>());

                if (!spans.IsEmpty)
                {
                    await _client.SendTracesAsync(spans).ConfigureAwait(false);
                }
            }
        }

        public void Add(IEnumerable<SpanModel> spans)
        {
            foreach (SpanModel span in spans)
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
