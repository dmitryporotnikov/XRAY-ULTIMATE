using System;
using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class ProcessItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double MemoryWorkingSetMB { get; set; }
    public int ThreadCount { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }

    public string MemoryDisplay => $"{MemoryWorkingSetMB:F1} MB";
    public string PidDisplay => $"PID: {Id}";
    public string ThreadDisplay => $"{ThreadCount} threads";
}

public class WindowsServiceItem
{
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StartType { get; set; } = string.Empty;

    public bool IsRunning => Status.Equals("Running", StringComparison.OrdinalIgnoreCase);
    public bool CanStart => Status.Equals("Stopped", StringComparison.OrdinalIgnoreCase) || Status.Equals("Paused", StringComparison.OrdinalIgnoreCase);
    public bool CanStop => IsRunning;
    public bool CanRestart => IsRunning;

    [System.Text.Json.Serialization.JsonIgnore]
    public Microsoft.UI.Xaml.Media.Brush StatusBrush => IsRunning
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 52, 211, 153))
        : (Status.Equals("Stopped", StringComparison.OrdinalIgnoreCase)
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 163, 184))
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 251, 191, 36)));
}

public class ProcessServiceInfo
{
    public List<ProcessItem> Processes { get; set; } = new();
    public List<WindowsServiceItem> Services { get; set; } = new();
}
