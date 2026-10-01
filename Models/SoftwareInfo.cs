using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class InstalledApplication
{
    public string DisplayName { get; set; } = string.Empty;
    public string DisplayVersion { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string InstallDate { get; set; } = string.Empty;
    public string InstallLocation { get; set; } = string.Empty;
    public string UninstallString { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty; // 64-bit, 32-bit, Store App
    public long EstimatedSizeKB { get; set; }

    public string FormattedSize => EstimatedSizeKB > 0
        ? (EstimatedSizeKB > 1024 * 1024
            ? $"{EstimatedSizeKB / 1024.0 / 1024.0:F2} GB"
            : $"{EstimatedSizeKB / 1024.0:F1} MB")
        : string.Empty;
}

public class SoftwareInfo
{
    public int TotalCount => Applications.Count;
    public List<InstalledApplication> Applications { get; set; } = new();
}
