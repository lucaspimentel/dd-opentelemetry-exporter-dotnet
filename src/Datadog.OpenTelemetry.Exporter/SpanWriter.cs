using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Datadog.OpenTelemetry.Exporter
{
    public class SpanWriter
    {
        private readonly TraceAgentClient _client;
        private readonly Task _loopTask;

        private ConcurrentBag<Span> _frontBuffer = [];
        private ConcurrentBag<Span> _backBuffer = [];
        private bool _enabled;

        public SpanWriter(TraceAgentClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _enabled = true;
            _loopTask = Task.Factory.StartNew(async () => await StartAsync().ConfigureAwait(false), TaskCreationOptions.LongRunning);
        }

        private async Task StartAsync()
        {
            while (_enabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                // switch the queued spans with a new empty collection
                _backBuffer.Clear();
                var spans = Interlocked.Exchange(ref _frontBuffer, _backBuffer);
                _backBuffer = spans;

                if (!spans.IsEmpty)
                {
                    await _client.SendTracesAsync(spans).ConfigureAwait(false);
                    spans.Clear();
                }
            }
        }

        public void Add(Span span)
        {
            _frontBuffer.Add(span);
        }

        public void RequestStop()
        {
            _enabled = false;
        }

        public async Task StopAsync()
        {
            _enabled = false;
            await _loopTask.ConfigureAwait(false);
        }
    }
}
