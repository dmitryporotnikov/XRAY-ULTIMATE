using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class FilterDriverCollector
{
    public static FilterDriversReport Collect()
    {
        var report = new FilterDriversReport();

        // 1. Collect Network Filter Drivers
        CollectNetworkFilters(report);

        // 2. Collect Filesystem Minifilters
        CollectFilesystemFilters(report);

        // Populate Properties
        report.Properties.Add(new PropertyItem("Summary", "Total Network Filter Drivers", report.NetworkFilters.Count.ToString()));
        report.Properties.Add(new PropertyItem("Summary", "Total Filesystem Minifilters", report.FilesystemFilters.Count.ToString()));

        foreach (var nf in report.NetworkFilters)
        {
            report.Properties.Add(new PropertyItem("Network Filters", nf.DisplayName, $"{nf.ServiceName} ({nf.FilterClass})"));
        }

        foreach (var fs in report.FilesystemFilters)
        {
            report.Properties.Add(new PropertyItem("Filesystem Minifilters", fs.FilterName, $"Altitude: {fs.Altitude} ({fs.Category})"));
        }

        return report;
    }

    private static void CollectNetworkFilters(FilterDriversReport report)
    {
        var seenServices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Parse netcfg.exe -s n output
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netcfg.exe",
                Arguments = "-s n",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc != null)
            {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(3000);

                string currentSection = "";
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("---")) continue;

                    if (line.EndsWith("Services", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = "Services";
                        continue;
                    }
                    if (line.EndsWith("Protocols", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = "Protocols";
                        continue;
                    }
                    if (line.EndsWith("Adapters", StringComparison.OrdinalIgnoreCase) || line.EndsWith("Clients", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = "Other";
                        continue;
                    }

                    if (currentSection == "Services")
                    {
                        // line is like: "ms_pacer                   QoS Packet Scheduler"
                        var parts = Regex.Split(line, @"\s{2,}");
                        if (parts.Length >= 2)
                        {
                            string compId = parts[0].Trim();
                            string desc = parts[1].Trim();

                            if (desc.StartsWith("@%windir%"))
                            {
                                if (compId.Contains("vwifi")) desc = "Virtual WiFi Filter Driver";
                            }

                            if (seenServices.Add(compId))
                            {
                                report.NetworkFilters.Add(new NetworkFilterDriver
                                {
                                    ServiceName = compId,
                                    Name = compId,
                                    DisplayName = desc,
                                    Description = desc,
                                    FilterClass = InferNetworkFilterClass(compId, desc),
                                    FilterType = "Lightweight Filter (LWF)",
                                    State = "Enabled"
                                });
                            }
                        }
                    }
                }
            }
        }
        catch { }

        // Augment from Registry NDIS filter drivers
        try
        {
            using var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (servicesKey != null)
            {
                foreach (var subName in servicesKey.GetSubKeyNames())
                {
                    try
                    {
                        using var svc = servicesKey.OpenSubKey(subName);
                        if (svc == null) continue;

                        string group = svc.GetValue("Group") as string ?? "";
                        if (group.Equals("NDIS", StringComparison.OrdinalIgnoreCase) ||
                            group.Equals("NDIS Wrapper", StringComparison.OrdinalIgnoreCase) ||
                            group.Equals("NDISUIO", StringComparison.OrdinalIgnoreCase))
                        {
                            if (seenServices.Add(subName))
                            {
                                string disp = svc.GetValue("DisplayName") as string ?? subName;
                                string desc = svc.GetValue("Description") as string ?? disp;

                                report.NetworkFilters.Add(new NetworkFilterDriver
                                {
                                    ServiceName = subName,
                                    Name = subName,
                                    DisplayName = CleanDriverString(disp),
                                    Description = CleanDriverString(desc),
                                    FilterClass = InferNetworkFilterClass(subName, disp),
                                    FilterType = "NDIS Filter Driver",
                                    State = "Active"
                                });
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private static void CollectFilesystemFilters(FilterDriversReport report)
    {
        var seenFilters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Attempt fltmc.exe filters first (works when elevated)
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "fltmc.exe",
                Arguments = "filters",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc != null)
            {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(3000);

                if (!string.IsNullOrWhiteSpace(output) && !output.Contains("failed with error", StringComparison.OrdinalIgnoreCase))
                {
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    bool inTable = false;

                    foreach (var rawLine in lines)
                    {
                        string line = rawLine.Trim();
                        if (line.StartsWith("----"))
                        {
                            inTable = true;
                            continue;
                        }
                        if (!inTable) continue;

                        // Columns: Filter Name, Num Instances, Altitude, Frame
                        var tokens = Regex.Split(line, @"\s+");
                        if (tokens.Length >= 4)
                        {
                            string name = tokens[0].Trim();
                            int.TryParse(tokens[1].Trim(), out int instances);
                            string altitude = tokens[2].Trim();
                            string frame = tokens[3].Trim();

                            if (seenFilters.Add(name))
                            {
                                report.FilesystemFilters.Add(new FilesystemFilter
                                {
                                    FilterName = name,
                                    NumInstances = instances,
                                    Altitude = altitude,
                                    Frame = frame,
                                    Category = ClassifyAltitude(altitude),
                                    Description = GetFilterFriendlyName(name)
                                });
                            }
                        }
                    }
                }
            }
        }
        catch { }

        // Always query Registry Services for all registered minifilters
        try
        {
            using var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (servicesKey != null)
            {
                foreach (var subName in servicesKey.GetSubKeyNames())
                {
                    try
                    {
                        using var instKey = servicesKey.OpenSubKey($@"{subName}\Instances");
                        if (instKey == null) continue;

                        string defaultInst = instKey.GetValue("DefaultInstance") as string ?? "";
                        string altitude = "";

                        if (!string.IsNullOrEmpty(defaultInst))
                        {
                            using var defSub = instKey.OpenSubKey(defaultInst);
                            altitude = defSub?.GetValue("Altitude") as string ?? "";
                        }

                        if (string.IsNullOrEmpty(altitude))
                        {
                            // Try first subkey
                            var subKeys = instKey.GetSubKeyNames();
                            if (subKeys.Length > 0)
                            {
                                using var firstSub = instKey.OpenSubKey(subKeys[0]);
                                altitude = firstSub?.GetValue("Altitude") as string ?? "";
                            }
                        }

                        if (!string.IsNullOrEmpty(altitude) && seenFilters.Add(subName))
                        {
                            using var driverKey = servicesKey.OpenSubKey(subName);
                            string disp = driverKey?.GetValue("DisplayName") as string ?? subName;

                            report.FilesystemFilters.Add(new FilesystemFilter
                            {
                                FilterName = subName,
                                NumInstances = 1,
                                Altitude = altitude,
                                Frame = "0",
                                Category = ClassifyAltitude(altitude),
                                Description = CleanDriverString(disp)
                            });
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        // Sort by altitude descending (standard fltmc order)
        report.FilesystemFilters = report.FilesystemFilters
            .OrderByDescending(f => double.TryParse(f.Altitude, out double a) ? a : 0)
            .ToList();
    }

    private static string ClassifyAltitude(string altitude)
    {
        if (double.TryParse(altitude, out double alt))
        {
            if (alt >= 400000) return "Filter Manager";
            if (alt >= 380000) return "FSFilter Top";
            if (alt >= 360000) return "Activity Monitor";
            if (alt >= 340000) return "Undelete";
            if (alt >= 320000) return "Anti-Virus / EDR";
            if (alt >= 300000) return "Replication";
            if (alt >= 280000) return "Continuous Backup";
            if (alt >= 260000) return "Content Screener";
            if (alt >= 240000) return "Quota Management";
            if (alt >= 220000) return "System Recovery";
            if (alt >= 200000) return "Cluster File System";
            if (alt >= 180000) return "HSM";
            if (alt >= 160000) return "Compression";
            if (alt >= 140000) return "Encryption";
            if (alt >= 120000) return "Physical Quota";
            if (alt >= 100000) return "Open File";
            if (alt >= 80000) return "Security Enhancer";
            if (alt >= 60000) return "Copy Protection";
            if (alt >= 40000) return "Bottom / Virtualization";
            return "System";
        }
        return "Minifilter";
    }

    private static string GetFilterFriendlyName(string filterName) => filterName.ToLowerInvariant() switch
    {
        "wdfilter" => "Windows Defender Real-Time Protection Minifilter",
        "bindflt" => "Windows Container & Bind Filter",
        "fileinfo" => "File Information Minifilter",
        "filecrypt" => "Windows File Encryption Filter",
        "cldflt" => "Windows Cloud Files Mini-filter (OneDrive)",
        "storqosflt" => "Storage Quality of Service Filter",
        "applockerfltr" => "Application Locker Execution Filter",
        "bfs" => "Boot File System Filter",
        "filetrace" => "Windows File Tracing Filter",
        "npsvctmp" => "Named Pipe Service Virtualization Filter",
        _ => filterName
    };

    private static string InferNetworkFilterClass(string compId, string desc)
    {
        string text = $"{compId} {desc}".ToLowerInvariant();
        if (text.Contains("scheduler") || text.Contains("pacer") || text.Contains("qos")) return "Scheduler / QoS";
        if (text.Contains("wfp") || text.Contains("filtering") || text.Contains("firewall")) return "Security / WFP";
        if (text.Contains("capture") || text.Contains("pcap") || text.Contains("wireshark")) return "Packet Capture";
        if (text.Contains("switch") || text.Contains("vswitch") || text.Contains("hyper-v")) return "Virtual Switch";
        if (text.Contains("wifi") || text.Contains("wireless")) return "Wireless Filter";
        if (text.Contains("bridge")) return "Bridge Driver";
        if (text.Contains("vpn") || text.Contains("tunnel")) return "VPN / Tunnel";
        return "Custom NDIS LWF";
    }

    private static string CleanDriverString(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        if (str.StartsWith("@%windir%", StringComparison.OrdinalIgnoreCase) || str.StartsWith("@system32", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(str, @"([a-zA-Z0-9_\-]+)\.sys", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value;
        }
        return str;
    }
}
