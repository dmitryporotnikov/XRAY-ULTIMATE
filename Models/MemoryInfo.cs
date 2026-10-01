using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class MemoryInfo
{
    public ulong TotalPhysicalBytes { get; set; }
    public ulong AvailablePhysicalBytes { get; set; }
    public ulong UsedPhysicalBytes => TotalPhysicalBytes > AvailablePhysicalBytes ? TotalPhysicalBytes - AvailablePhysicalBytes : 0;
    public double MemoryLoadPercent { get; set; }

    public ulong TotalPageFileBytes { get; set; }
    public ulong AvailablePageFileBytes { get; set; }
    public ulong UsedPageFileBytes => TotalPageFileBytes > AvailablePageFileBytes ? TotalPageFileBytes - AvailablePageFileBytes : 0;

    public ulong TotalVirtualBytes { get; set; }
    public ulong AvailableVirtualBytes { get; set; }

    public List<PhysicalMemoryStick> MemorySticks { get; set; } = new();
    public List<PropertyItem> Properties { get; set; } = new();
}

public class PhysicalMemoryStick
{
    public string BankLabel { get; set; } = string.Empty;
    public string DeviceLocator { get; set; } = string.Empty;
    public ulong CapacityBytes { get; set; }
    public uint SpeedMHz { get; set; }
    public uint ConfiguredClockSpeedMHz { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string MemoryType { get; set; } = string.Empty;
    public string FormFactor { get; set; } = string.Empty;
    public ushort DataWidth { get; set; }

    public string FormattedCapacity => Helpers.UnitFormatter.FormatBytes(CapacityBytes);
    public string FormattedSpeed => ConfiguredClockSpeedMHz > 0 ? $"{ConfiguredClockSpeedMHz} MHz" : (SpeedMHz > 0 ? $"{SpeedMHz} MHz" : "Unknown");
    public string FormattedRatedSpeed => SpeedMHz > 0 ? $"{SpeedMHz} MHz (Rated)" : "";
}
