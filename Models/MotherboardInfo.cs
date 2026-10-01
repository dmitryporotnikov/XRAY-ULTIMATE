using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class MotherboardInfo
{
    public string Manufacturer { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;

    public string BiosVendor { get; set; } = string.Empty;
    public string BiosVersion { get; set; } = string.Empty;
    public string BiosReleaseDate { get; set; } = string.Empty;
    public string SmbiosVersion { get; set; } = string.Empty;

    public string SystemFamily { get; set; } = string.Empty;
    public string SystemSku { get; set; } = string.Empty;

    public List<PropertyItem> Properties { get; set; } = new();
}
