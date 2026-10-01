using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class CpuInfo
{
    public string ProcessorName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public int CoreCount { get; set; }
    public int LogicalProcessorCount { get; set; }
    public uint BaseClockSpeedMHz { get; set; }
    public uint MaxClockSpeedMHz { get; set; }
    public string SocketDesignation { get; set; } = string.Empty;
    public string Stepping { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public string CurrentVoltage { get; set; } = string.Empty;

    public uint L1DataCacheKB { get; set; }
    public uint L1InstructionCacheKB { get; set; }
    public uint L2CacheKB { get; set; }
    public uint L3CacheKB { get; set; }

    public bool VirtualizationFirmwareEnabled { get; set; }

    public List<CpuFeatureItem> SupportedFeatures { get; set; } = new();
    public List<PropertyItem> Properties { get; set; } = new();
}

public class CpuFeatureItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSupported { get; set; }

    public string Glyph => IsSupported ? "\uE73E" : "\uE711";

    [System.Text.Json.Serialization.JsonIgnore]
    public Microsoft.UI.Xaml.Media.Brush StatusBrush => IsSupported
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 52, 211, 153))
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139));

    public CpuFeatureItem() { }

    public CpuFeatureItem(string name, string description, bool isSupported)
    {
        Name = name;
        Description = description;
        IsSupported = isSupported;
    }
}
