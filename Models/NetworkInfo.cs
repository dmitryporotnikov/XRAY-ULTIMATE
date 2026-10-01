using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class NetworkInfo
{
    public List<NetworkAdapterInfo> Adapters { get; set; } = new();
    public WifiDetailsInfo? WifiDetails { get; set; }
    public List<PropertyItem> Properties { get; set; } = new();
}

public class NetworkAdapterInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InterfaceType { get; set; } = string.Empty;
    public long SpeedBps { get; set; }
    public List<string> Ipv4Addresses { get; set; } = new();
    public List<string> Ipv6Addresses { get; set; } = new();
    public List<string> Gateways { get; set; } = new();
    public List<string> DnsServers { get; set; } = new();
    public bool DhcpEnabled { get; set; }
    public List<string> DhcpServers { get; set; } = new();

    public string PrimaryIpAddress => Ipv4Addresses.Count > 0 ? Ipv4Addresses[0] : (Ipv6Addresses.Count > 0 ? Ipv6Addresses[0] : "-");
    public string FormattedSpeed => Helpers.UnitFormatter.FormatSpeedBps(SpeedBps);
    public string FormattedIpv4 => Ipv4Addresses.Count > 0 ? string.Join(", ", Ipv4Addresses) : "None";
    public string FormattedIpv6 => Ipv6Addresses.Count > 0 ? string.Join(", ", Ipv6Addresses) : "None";
    public string FormattedGateways => Gateways.Count > 0 ? string.Join(", ", Gateways) : "None";
    public string FormattedDns => DnsServers.Count > 0 ? string.Join(", ", DnsServers) : "None";
    public string FormattedDhcp => DhcpEnabled ? (DhcpServers.Count > 0 ? $"Enabled ({string.Join(", ", DhcpServers)})" : "Enabled") : "Disabled (Static IP)";
    public bool IsUp => Status.Equals("Up", System.StringComparison.OrdinalIgnoreCase);
    public string Glyph => (InterfaceType.Contains("Wireless") || InterfaceType.Contains("Wi-Fi") || Name.Contains("Wi-Fi")) ? "\uE701" : "\uE839";
}

public class WifiDetailsInfo
{
    public string InterfaceName { get; set; } = string.Empty;
    public string Ssid { get; set; } = string.Empty;
    public string Bssid { get; set; } = string.Empty;
    public string BSSID { get => Bssid; set => Bssid = value; }
    public int SignalQuality { get; set; }
    public string RadioType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Authentication { get; set; } = string.Empty;
    public string Cipher { get; set; } = string.Empty;
    public string ReceiveRateMbps { get; set; } = string.Empty;
    public string TransmitRateMbps { get; set; } = string.Empty;
}
