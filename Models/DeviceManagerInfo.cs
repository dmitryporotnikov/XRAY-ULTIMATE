using System;
using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class DeviceItem
{
    public string Name { get; set; } = string.Empty;
    public string DeviceClass { get; set; } = string.Empty;
    public string FriendlyClassName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string Status { get; set; } = "OK";
    public uint ErrorCode { get; set; }
    public string StatusMessage { get; set; } = "Working properly";
    public string Service { get; set; } = string.Empty;
    public string ClassIconGlyph { get; set; } = "\uE9A9";
    public bool HasProblem { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public Microsoft.UI.Xaml.Media.Brush StatusBrush => HasProblem
        ? (ErrorCode == 22
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 163, 184)) // Disabled / Gray
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)))   // Error / Red
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 52, 211, 153));     // Normal / Green
}

public class DeviceClassGroup
{
    public string ClassName { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uE9A9";
    public int DeviceCount => Devices.Count;
    public List<DeviceItem> Devices { get; set; } = new();
    public string HeaderText => $"{FriendlyName} ({DeviceCount})";
}

public class DeviceManagerReport
{
    public int TotalDevicesCount { get; set; }
    public int ProblemDevicesCount { get; set; }
    public List<DeviceClassGroup> Groups { get; set; } = new();
    public List<DeviceItem> AllDevices { get; set; } = new();
    public List<PropertyItem> Properties { get; set; } = new();
}
