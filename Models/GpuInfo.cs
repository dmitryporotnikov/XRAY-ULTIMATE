using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class GpuInfo
{
    public List<GraphicsCardInfo> Adapters { get; set; } = new();
    public List<MonitorInfo> Monitors { get; set; } = new();
    public List<PropertyItem> Properties { get; set; } = new();
}

public class GraphicsCardInfo
{
    public string Name { get; set; } = string.Empty;
    public ulong AdapterRamBytes { get; set; }
    public string DriverVersion { get; set; } = string.Empty;
    public string DriverDate { get; set; } = string.Empty;
    public string VideoProcessor { get; set; } = string.Empty;
    public uint CurrentHorizontalResolution { get; set; }
    public uint CurrentVerticalResolution { get; set; }
    public uint CurrentRefreshRate { get; set; }
    public string VideoModeDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;

    public string FormattedVram => AdapterRamBytes > 0 ? Helpers.UnitFormatter.FormatBytes(AdapterRamBytes) : "Shared / System RAM";
    public string FormattedResolution => CurrentHorizontalResolution > 0 ? $"{CurrentHorizontalResolution} x {CurrentVerticalResolution} @ {CurrentRefreshRate} Hz" : "Inactive / Secondary Output";
    public string FormattedDriver => string.IsNullOrEmpty(DriverVersion) ? "Driver: Standard" : $"Driver: {DriverVersion}";
}

public class MonitorInfo
{
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public uint ScreenWidth { get; set; }
    public uint ScreenHeight { get; set; }
    public string MonitorType { get; set; } = string.Empty;
}
