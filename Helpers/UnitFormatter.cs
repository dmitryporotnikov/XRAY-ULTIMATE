using System;

namespace XRAY_ULTIMATE.Helpers;

public static class UnitFormatter
{
    public static string FormatBytes(ulong bytes)
    {
        if (bytes == 0) return "0 B";
        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
            if (counter >= suffixes.Length - 1) break;
        }
        return $"{number:n2} {suffixes[counter]}";
    }

    public static string FormatBytes(long bytes) => bytes < 0 ? "0 B" : FormatBytes((ulong)bytes);

    public static string FormatHertz(uint mhz)
    {
        if (mhz >= 1000)
            return $"{mhz / 1000.0:F2} GHz";
        return $"{mhz} MHz";
    }

    public static string FormatSpeedBps(long bps)
    {
        if (bps <= 0) return "Unknown / Disconnected";
        if (bps >= 1_000_000_000)
            return $"{bps / 1_000_000_000.0:F1} Gbps";
        if (bps >= 1_000_000)
            return $"{bps / 1_000_000.0:F1} Mbps";
        if (bps >= 1_000)
            return $"{bps / 1_000.0:F1} Kbps";
        return $"{bps} bps";
    }

    public static string FormatUptime(TimeSpan span)
    {
        if (span.TotalDays >= 1)
            return $"{(int)span.TotalDays}d {span.Hours}h {span.Minutes}m {span.Seconds}s";
        if (span.TotalHours >= 1)
            return $"{span.Hours}h {span.Minutes}m {span.Seconds}s";
        return $"{span.Minutes}m {span.Seconds}s";
    }
}
