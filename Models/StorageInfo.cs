using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class StorageInfo
{
    public List<PhysicalDiskInfo> PhysicalDisks { get; set; } = new();
    public List<VolumeInfo> Volumes { get; set; } = new();
    public List<PropertyItem> Properties { get; set; } = new();
}

public class PhysicalDiskInfo
{
    public string Model { get; set; } = string.Empty;
    public string InterfaceType { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public ulong SizeBytes { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public uint Partitions { get; set; }
    public string Status { get; set; } = string.Empty;
    public string FirmwareRevision { get; set; } = string.Empty;

    public string FormattedSize => Helpers.UnitFormatter.FormatBytes(SizeBytes);
}

public class VolumeInfo
{
    public string DriveLetter { get; set; } = string.Empty;
    public string VolumeLabel { get; set; } = string.Empty;
    public string FileSystem { get; set; } = string.Empty;
    public ulong TotalSizeBytes { get; set; }
    public ulong FreeSizeBytes { get; set; }
    public ulong UsedSizeBytes => TotalSizeBytes > FreeSizeBytes ? TotalSizeBytes - FreeSizeBytes : 0;
    public double PercentUsed => TotalSizeBytes > 0 ? (double)(TotalSizeBytes - FreeSizeBytes) / TotalSizeBytes * 100.0 : 0;
    public string DriveType { get; set; } = string.Empty;
    public bool IsReady { get; set; }

    public string FormattedTotal => Helpers.UnitFormatter.FormatBytes(TotalSizeBytes);
    public string FormattedUsed => Helpers.UnitFormatter.FormatBytes(UsedSizeBytes);
    public string FormattedFree => Helpers.UnitFormatter.FormatBytes(FreeSizeBytes);
    public string FormattedPercent => $"{PercentUsed:F1}%";
}
