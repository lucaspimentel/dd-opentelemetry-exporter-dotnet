using System;
using System.Diagnostics;

namespace Datadog.OpenTelemetry.Exporter
{
    internal static class ConversionHelper
    {
        /// <summary>
        /// Returns the number of nanoseconds that have elapsed since 1970-01-01T00:00:00.000Z.
        /// </summary>
        /// <param name="dateTimeOffset">The value to get the number of elapsed nanoseconds for.</param>
        /// <returns>The number of nanoseconds that have elapsed since 1970-01-01T00:00:00.000Z.</returns>
        public static long ToUnixTimeNanoseconds(DateTimeOffset dateTimeOffset)
        {
            return (dateTimeOffset.Ticks - TimeConstants.UnixEpochInTicks) * TimeConstants.NanoSecondsPerTick;
        }

        public static long ToNanoseconds(TimeSpan ts)
        {
            return ts.Ticks * TimeConstants.NanoSecondsPerTick;
        }

        public static ulong ToUInt64(ActivityTraceId activityTraceId)
        {
            var traceIdBytes = new byte[16];
            activityTraceId.CopyTo(traceIdBytes);
            return BitConverter.ToUInt64(traceIdBytes, 8);
        }

        public static ulong ToUInt64(ActivitySpanId activitySpanId)
        {
            var spanIdBytes = new byte[8];
            activitySpanId.CopyTo(spanIdBytes);
            return BitConverter.ToUInt64(spanIdBytes, 0);
        }
    }
}
