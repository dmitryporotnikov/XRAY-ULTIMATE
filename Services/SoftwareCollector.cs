using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class SoftwareCollector
{
    public static SoftwareInfo Collect()
    {
        var info = new SoftwareInfo();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 64-bit Registry
        ScanRegistryKey(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "64-bit", info.Applications, set);

        // 32-bit Registry (WOW64)
        if (Environment.Is64BitOperatingSystem)
        {
            ScanRegistryKey(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "32-bit", info.Applications, set);
        }

        // Current User
        ScanRegistryKey(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "User", info.Applications, set);

        info.Applications = info.Applications
            .OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return info;
    }

    private static void ScanRegistryKey(RegistryKey root, string subKeyPath, string arch, List<InstalledApplication> list, HashSet<string> seen)
    {
        try
        {
            using var baseKey = root.OpenSubKey(subKeyPath);
            if (baseKey == null) return;

            var subKeyNames = baseKey.GetSubKeyNames();
            foreach (var subName in subKeyNames)
            {
                try
                {
                    using var appKey = baseKey.OpenSubKey(subName);
                    if (appKey == null) continue;

                    // Skip system components
                    var sysComp = appKey.GetValue("SystemComponent");
                    if (sysComp is int sc && sc == 1) continue;

                    var parentKey = appKey.GetValue("ParentKeyName");
                    if (parentKey != null && !string.IsNullOrEmpty(parentKey.ToString())) continue;

                    string displayName = appKey.GetValue("DisplayName") as string ?? "";
                    if (string.IsNullOrWhiteSpace(displayName)) continue;

                    string displayVersion = appKey.GetValue("DisplayVersion") as string ?? "";
                    string publisher = appKey.GetValue("Publisher") as string ?? "";
                    string installDate = appKey.GetValue("InstallDate") as string ?? "";
                    string installLocation = appKey.GetValue("InstallLocation") as string ?? "";
                    string uninstallString = appKey.GetValue("UninstallString") as string ?? "";

                    long estimatedSize = 0;
                    var estVal = appKey.GetValue("EstimatedSize");
                    if (estVal is int sizeInt) estimatedSize = sizeInt;
                    else if (estVal is long sizeLong) estimatedSize = sizeLong;

                    string keySignature = $"{displayName}::{displayVersion}::{publisher}";
                    if (!seen.Add(keySignature)) continue;

                    // Format date yyyyMMdd -> yyyy-MM-dd
                    if (installDate.Length == 8 && char.IsDigit(installDate[0]))
                    {
                        installDate = $"{installDate.Substring(0, 4)}-{installDate.Substring(4, 2)}-{installDate.Substring(6, 2)}";
                    }

                    list.Add(new InstalledApplication
                    {
                        DisplayName = displayName.Trim(),
                        DisplayVersion = displayVersion.Trim(),
                        Publisher = publisher.Trim(),
                        InstallDate = installDate.Trim(),
                        InstallLocation = installLocation.Trim(),
                        UninstallString = uninstallString.Trim(),
                        Architecture = arch,
                        EstimatedSizeKB = estimatedSize
                    });
                }
                catch { }
            }
        }
        catch { }
    }
}
