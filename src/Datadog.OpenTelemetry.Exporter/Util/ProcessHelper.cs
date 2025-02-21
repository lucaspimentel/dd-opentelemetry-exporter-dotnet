using System.Diagnostics;

namespace Datadog.OpenTelemetry.Exporter;

internal static class ProcessHelper
{
    public static long ProcessId { get; }

    public static string ProcessName { get; }

    static ProcessHelper()
    {
        using var process = Process.GetCurrentProcess();
        ProcessId = process.Id;
        ProcessName = process.ProcessName;
    }
}
