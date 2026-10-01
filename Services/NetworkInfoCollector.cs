using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class NetworkInfoCollector
{
    public static NetworkInfo Collect()
    {
        var info = new NetworkInfo();

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in interfaces)
            {
                // Only include true physical and valid virtual network adapters (segregate filter drivers)
                if (!IsTrueNetworkAdapter(ni)) continue;

                var adapter = new NetworkAdapterInfo
                {
                    Id = ni.Id,
                    Name = ni.Name,
                    Description = ni.Description,
                    Status = ni.OperationalStatus.ToString(),
                    InterfaceType = ni.NetworkInterfaceType.ToString(),
                    SpeedBps = ni.Speed
                };

                // MAC Address
                var macBytes = ni.GetPhysicalAddress().GetAddressBytes();
                if (macBytes.Length > 0)
                {
                    adapter.MacAddress = string.Join(":", Array.ConvertAll(macBytes, b => b.ToString("X2")));
                }

                // IP Properties
                var ipProps = ni.GetIPProperties();
                if (ipProps != null)
                {
                    foreach (var u in ipProps.UnicastAddresses)
                    {
                        if (u.Address.AddressFamily == AddressFamily.InterNetwork)
                            adapter.Ipv4Addresses.Add(u.Address.ToString());
                        else if (u.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            adapter.Ipv6Addresses.Add(u.Address.ToString());
                    }

                    foreach (var g in ipProps.GatewayAddresses)
                    {
                        if (g.Address != null)
                            adapter.Gateways.Add(g.Address.ToString());
                    }

                    foreach (var d in ipProps.DnsAddresses)
                    {
                        if (d != null)
                            adapter.DnsServers.Add(d.ToString());
                    }

                    try
                    {
                        var ipv4Props = ipProps.GetIPv4Properties();
                        if (ipv4Props != null)
                        {
                            adapter.DhcpEnabled = ipv4Props.IsDhcpEnabled;
                        }
                    }
                    catch { }

                    foreach (var dhcp in ipProps.DhcpServerAddresses)
                    {
                        if (dhcp != null)
                            adapter.DhcpServers.Add(dhcp.ToString());
                    }
                }

                info.Adapters.Add(adapter);
            }
        }
        catch { }

        // Wi-Fi diagnostics via netsh
        try
        {
            var wifi = QueryWifiDetails();
            if (wifi != null)
            {
                info.WifiDetails = wifi;
            }
        }
        catch { }

        // Populate Properties
        foreach (var ad in info.Adapters)
        {
            string cat = $"Adapter: {ad.Name} ({ad.Status})";
            info.Properties.Add(new PropertyItem(cat, "Description", ad.Description));
            info.Properties.Add(new PropertyItem(cat, "Status", ad.Status));
            info.Properties.Add(new PropertyItem(cat, "Type", ad.InterfaceType));
            if (!string.IsNullOrEmpty(ad.MacAddress))
                info.Properties.Add(new PropertyItem(cat, "MAC Address", ad.MacAddress));
            if (ad.SpeedBps > 0)
                info.Properties.Add(new PropertyItem(cat, "Link Speed", UnitFormatter.FormatSpeedBps(ad.SpeedBps)));
            if (ad.Ipv4Addresses.Count > 0)
                info.Properties.Add(new PropertyItem(cat, "IPv4 Address", string.Join(", ", ad.Ipv4Addresses)));
            if (ad.Ipv6Addresses.Count > 0)
                info.Properties.Add(new PropertyItem(cat, "IPv6 Address", string.Join(", ", ad.Ipv6Addresses)));
            if (ad.Gateways.Count > 0)
                info.Properties.Add(new PropertyItem(cat, "Default Gateway", string.Join(", ", ad.Gateways)));
            if (ad.DnsServers.Count > 0)
                info.Properties.Add(new PropertyItem(cat, "DNS Servers", string.Join(", ", ad.DnsServers)));
            info.Properties.Add(new PropertyItem(cat, "DHCP Enabled", ad.DhcpEnabled ? "Yes" : "No"));
        }

        if (info.WifiDetails != null)
        {
            string cat = $"Wi-Fi Diagnostics: {info.WifiDetails.Ssid}";
            info.Properties.Add(new PropertyItem(cat, "SSID", info.WifiDetails.Ssid));
            info.Properties.Add(new PropertyItem(cat, "BSSID", info.WifiDetails.BSSID));
            info.Properties.Add(new PropertyItem(cat, "Signal Quality", $"{info.WifiDetails.SignalQuality}%"));
            info.Properties.Add(new PropertyItem(cat, "Radio Type", info.WifiDetails.RadioType));
            info.Properties.Add(new PropertyItem(cat, "Channel", info.WifiDetails.Channel));
            info.Properties.Add(new PropertyItem(cat, "Authentication", info.WifiDetails.Authentication));
            info.Properties.Add(new PropertyItem(cat, "Cipher", info.WifiDetails.Cipher));
            if (!string.IsNullOrEmpty(info.WifiDetails.ReceiveRateMbps))
                info.Properties.Add(new PropertyItem(cat, "Rx Rate", $"{info.WifiDetails.ReceiveRateMbps} Mbps"));
            if (!string.IsNullOrEmpty(info.WifiDetails.TransmitRateMbps))
                info.Properties.Add(new PropertyItem(cat, "Tx Rate", $"{info.WifiDetails.TransmitRateMbps} Mbps"));
        }

        return info;
    }

    private static WifiDetailsInfo? QueryWifiDetails()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh.exe",
                Arguments = "wlan show interfaces",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return null;

            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(3000);

            if (string.IsNullOrWhiteSpace(output) || !output.Contains("SSID")) return null;

            var wifi = new WifiDetailsInfo();
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                int colonIdx = line.IndexOf(':');
                if (colonIdx < 0) continue;

                string key = line.Substring(0, colonIdx).Trim().ToLowerInvariant();
                string val = line.Substring(colonIdx + 1).Trim();

                if (key.Equals("ssid")) wifi.Ssid = val;
                else if (key.Equals("bssid")) wifi.Bssid = val;
                else if (key.Equals("signal"))
                {
                    var match = Regex.Match(val, @"\d+");
                    if (match.Success && int.TryParse(match.Value, out int sig))
                        wifi.SignalQuality = sig;
                }
                else if (key.Equals("radio type")) wifi.RadioType = val;
                else if (key.Equals("channel")) wifi.Channel = val;
                else if (key.Equals("authentication")) wifi.Authentication = val;
                else if (key.Equals("cipher")) wifi.Cipher = val;
                else if (key.Equals("receive rate (mbps)")) wifi.ReceiveRateMbps = val;
                else if (key.Equals("transmit rate (mbps)")) wifi.TransmitRateMbps = val;
                else if (key.Equals("name")) wifi.InterfaceName = val;
            }

            return string.IsNullOrEmpty(wifi.Ssid) ? null : wifi;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsTrueNetworkAdapter(NetworkInterface ni)
    {
        if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
            ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
            return false;

        string desc = ni.Description.ToLowerInvariant();
        string name = ni.Name.ToLowerInvariant();

        // Segregate NDIS filter drivers, packet schedulers, and WAN miniports
        if (desc.Contains("filter") || name.Contains("filter")) return false;
        if (desc.Contains("wan miniport") || name.Contains("wan miniport")) return false;
        if (desc.Contains("packet scheduler") || name.Contains("packet scheduler")) return false;
        if (desc.Contains("lightweight filter") || desc.Contains("lwf")) return false;
        if (desc.Contains("kernel debug") || name.Contains("kernel debug")) return false;
        if (desc.Contains("teredo") || desc.Contains("pseudo-interface")) return false;
        if (desc.Contains("ip-https") || desc.Contains("6to4") || desc.Contains("isatap")) return false;

        return true;
    }
}
