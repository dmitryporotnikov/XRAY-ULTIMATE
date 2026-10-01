using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class NetworkFilterDriver
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FilterClass { get; set; } = string.Empty; // scheduler, custom, encryption, compression, etc.
    public string FilterType { get; set; } = string.Empty; // Modifying, Monitoring
    public string FilterRunOrder { get; set; } = string.Empty;
    public string State { get; set; } = "Enabled";
}

public class FilesystemFilter
{
    public string FilterName { get; set; } = string.Empty;
    public int NumInstances { get; set; }
    public string Altitude { get; set; } = string.Empty;
    public string Frame { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class FilterDriversReport
{
    public List<NetworkFilterDriver> NetworkFilters { get; set; } = new();
    public List<FilesystemFilter> FilesystemFilters { get; set; } = new();
    public List<PropertyItem> Properties { get; set; } = new();
}
