using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class DsregcmdCollector
{
    public static async Task<DsregcmdInfo> CollectAsync()
    {
        return await Task.Run(() => Collect());
    }

    public static DsregcmdInfo Collect()
    {
        var info = new DsregcmdInfo();
        string output = string.Empty;

        try
        {
            var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var exePath = Path.Combine(systemPath, "dsregcmd.exe");

            var psi = new ProcessStartInfo
            {
                FileName = File.Exists(exePath) ? exePath : "dsregcmd.exe",
                Arguments = "/status",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc != null)
            {
                output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(4000);
            }
        }
        catch (Exception ex)
        {
            output = $"Error running dsregcmd.exe: {ex.Message}";
        }

        info.RawOutput = string.IsNullOrWhiteSpace(output) ? "No output returned by dsregcmd.exe /status." : output;

        // Parse lines
        string currentSection = "General";
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("+---") || line.StartsWith("---"))
                continue;

            if (line.StartsWith("|") && line.EndsWith("|"))
            {
                currentSection = line.Trim('|', ' ', '-');
                continue;
            }

            int colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                string key = line.Substring(0, colonIndex).Trim();
                string val = line.Substring(colonIndex + 1).Trim();

                if (string.IsNullOrEmpty(key)) continue;

                // Store in parsed properties
                info.ParsedProperties.Add(new PropertyItem(currentSection, key, val));

                // Specific property mapping
                if (key.Equals("AzureAdJoined", StringComparison.OrdinalIgnoreCase))
                    info.AzureAdJoined = val.Equals("YES", StringComparison.OrdinalIgnoreCase);
                else if (key.Equals("EnterpriseJoined", StringComparison.OrdinalIgnoreCase))
                    info.EnterpriseJoined = val.Equals("YES", StringComparison.OrdinalIgnoreCase);
                else if (key.Equals("DomainJoined", StringComparison.OrdinalIgnoreCase))
                    info.DomainJoined = val.Equals("YES", StringComparison.OrdinalIgnoreCase);
                else if (key.Equals("WorkplaceJoined", StringComparison.OrdinalIgnoreCase))
                    info.WorkplaceJoined = val.Equals("YES", StringComparison.OrdinalIgnoreCase);
                else if (key.Equals("DeviceId", StringComparison.OrdinalIgnoreCase))
                    info.DeviceId = val;
                else if (key.Equals("TenantId", StringComparison.OrdinalIgnoreCase))
                    info.TenantId = val;
                else if (key.Equals("TenantName", StringComparison.OrdinalIgnoreCase))
                    info.TenantName = val;
                else if (key.Equals("Idp", StringComparison.OrdinalIgnoreCase))
                    info.Idp = val;
                else if (key.Equals("KeyContainerId", StringComparison.OrdinalIgnoreCase))
                    info.KeyContainerId = val;
                else if (key.Equals("KeyProvider", StringComparison.OrdinalIgnoreCase))
                    info.KeyProvider = val;
                else if (key.Equals("TpmProtected", StringComparison.OrdinalIgnoreCase))
                    info.TpmProtected = val;
                else if (key.Equals("DeviceCertificateValidity", StringComparison.OrdinalIgnoreCase))
                    info.DeviceCertificateValidity = val;
                else if (key.Equals("AzureAdPrt", StringComparison.OrdinalIgnoreCase))
                    info.AzureAdPrt = val.Equals("YES", StringComparison.OrdinalIgnoreCase);
                else if (key.Equals("AzureAdPrtUpdateTime", StringComparison.OrdinalIgnoreCase))
                    info.AzureAdPrtUpdateTime = val;
                else if (key.Equals("AzureAdPrtAuthority", StringComparison.OrdinalIgnoreCase))
                    info.AzureAdPrtAuthority = val;
                else if (key.Equals("NgcSet", StringComparison.OrdinalIgnoreCase))
                    info.NgcSet = val;
                else if (key.Equals("WamDefaultSet", StringComparison.OrdinalIgnoreCase))
                    info.WamDefaultSet = val;
                else if (key.Equals("DomainName", StringComparison.OrdinalIgnoreCase))
                    info.DomainName = val;
            }
        }

        return info;
    }
}
