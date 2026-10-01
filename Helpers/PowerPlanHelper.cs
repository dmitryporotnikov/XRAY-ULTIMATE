using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace XRAY_ULTIMATE.Helpers;

public class PowerPlanItem
{
    public string Guid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public string DisplayText => IsActive ? $"{Name} (Active)" : Name;
}

public static class PowerPlanHelper
{
    public static readonly (string Name, string Guid) StandardHighPerformance = ("High Performance", "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    public static readonly (string Name, string Guid) StandardPowerSaver = ("Power Saver", "a1841308-3541-4fab-bc81-f71556f20b4a");
    public static readonly (string Name, string Guid) StandardBalanced = ("Balanced", "381b4222-f694-41f0-9685-ff5bb260df2e");
    public static readonly (string Name, string Guid) StandardUltimate = ("Ultimate Performance", "e9a42b02-d5df-448d-aa00-03f14749eb61");

    public static List<PowerPlanItem> GetPowerPlans()
    {
        var list = new List<PowerPlanItem>();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = "/list",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                var matches = Regex.Matches(output, @"GUID:\s*([0-9a-fA-F\-]+)\s*\(([^)]+)\)(\s*\*)?");
                foreach (Match m in matches)
                {
                    string guid = m.Groups[1].Value.Trim();
                    string name = m.Groups[2].Value.Trim();
                    bool active = m.Groups[3].Success && m.Groups[3].Value.Contains("*");
                    list.Add(new PowerPlanItem
                    {
                        Guid = guid,
                        Name = name,
                        IsActive = active
                    });
                }
            }
        }
        catch { }

        return list;
    }

    public static bool SetActivePlan(string guid)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = $"/setactive {guid}",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                p.WaitForExit();
                return p.ExitCode == 0;
            }
        }
        catch { }
        return false;
    }

    public static bool RestoreStandardPlan(string baseGuid, out string newGuid)
    {
        newGuid = "";
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = $"/duplicatescheme {baseGuid}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                var m = Regex.Match(output, @"GUID:\s*([0-9a-fA-F\-]+)");
                if (m.Success)
                {
                    newGuid = m.Groups[1].Value.Trim();
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    public static void OpenPowerSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "powercfg.cpl",
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:powersleep",
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
