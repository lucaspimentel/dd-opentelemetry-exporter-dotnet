using System;
using System.Diagnostics;

namespace Datadog.OpenTelemetry.Exporter.Util;

internal static class ConversionHelper
{
    private const long NanoSecondsPerTick = 1000000 / TimeSpan.TicksPerMillisecond;

    private const long UnixEpochInTicks = 621355968000000000; // = DateTimeOffset.FromUnixTimeMilliseconds(0).Ticks

    /// <summary>
    /// Returns the number of nanoseconds that have elapsed since 1970-01-01T00:00:00.000Z.
    /// </summary>
    /// <param name="dateTimeOffset">The value to get the number of elapsed nanoseconds for.</param>
    /// <returns>The number of nanoseconds that have elapsed since 1970-01-01T00:00:00.000Z.</returns>
    public static long ToUnixTimeNanoseconds(DateTimeOffset dateTimeOffset)
    {
        return (dateTimeOffset.Ticks - UnixEpochInTicks) * NanoSecondsPerTick;
    }

    public static long ToNanoseconds(TimeSpan ts)
    {
        return ts.Ticks * NanoSecondsPerTick;
    }

    public static unsafe void ToUInt64(ActivityTraceId activityTraceId, out ulong upper, out ulong lower)
    {
        Span<byte> traceIdBytes = stackalloc byte[16];
        activityTraceId.CopyTo(traceIdBytes);
        upper = BitConverter.ToUInt64(traceIdBytes[..8]);
        lower = BitConverter.ToUInt64(traceIdBytes[8..]);
    }

    public static ulong ToUInt64(ActivitySpanId activitySpanId)
    {
        Span<byte> spanIdBytes = stackalloc byte[8];
        activitySpanId.CopyTo(spanIdBytes);
        return BitConverter.ToUInt64(spanIdBytes);
    }
}
