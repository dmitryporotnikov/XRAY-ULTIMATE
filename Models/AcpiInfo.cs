using System;
using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class AcpiTableItem
{
    public string TableSignature { get; set; } = string.Empty;
    public uint TableLength { get; set; }
    public byte Revision { get; set; }
    public byte Checksum { get; set; }
    public string OemId { get; set; } = string.Empty;
    public string OemTableId { get; set; } = string.Empty;
    public uint OemRevision { get; set; }
    public string CreatorId { get; set; } = string.Empty;
    public uint CreatorRevision { get; set; }
    public string TableDescription { get; set; } = string.Empty;

    public string FormattedLength => $"{TableLength:N0} bytes ({TableLength / 1024.0:F1} KB)";
    public string FormattedOem => $"OEM: {OemId}";

    [System.Text.Json.Serialization.JsonIgnore]
    public byte[]? RawBytes { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string AslDisassembly { get; set; } = string.Empty;
}

public class AcpiDeviceObject
{
    public string Path { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public AcpiDeviceObject() { }

    public AcpiDeviceObject(string path, string objectType, string description)
    {
        Path = path;
        ObjectType = objectType;
        Description = description;
    }
}

public class DsdtReport
{
    public AcpiTableItem? Header { get; set; }
    public List<AcpiDeviceObject> DiscoveredObjects { get; set; } = new();
    public List<string> DiscoveredIdentifiers { get; set; } = new();
    public string HexDumpPreview { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    public string AslSourceCode { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    public byte[]? RawAmlData { get; set; }
}

public class AcpiReport
{
    public List<AcpiTableItem> Tables { get; set; } = new();
    public DsdtReport? Dsdt { get; set; }
    public List<PropertyItem> Properties { get; set; } = new();
}
