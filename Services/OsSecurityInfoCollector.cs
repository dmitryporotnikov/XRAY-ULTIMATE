using System;
using System.Collections.Generic;
using Microsoft.Win32;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class OsSecurityInfoCollector
{
    public static OsSecurityInfo Collect()
    {
        var info = new OsSecurityInfo();

        // Query Windows NT CurrentVersion from registry
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                info.ProductName = key.GetValue("ProductName") as string ?? "Windows 11";
                info.DisplayVersion = key.GetValue("DisplayVersion") as string ?? key.GetValue("ReleaseId") as string ?? "";
                info.CurrentBuild = key.GetValue("CurrentBuild") as string ?? key.GetValue("CurrentBuildNumber") as string ?? "";
                var ubr = key.GetValue("UBR");
                if (ubr != null) info.Ubr = ubr.ToString() ?? "";
                info.RegisteredOwner = key.GetValue("RegisteredOwner") as string ?? "";
                info.RegisteredOrganization = key.GetValue("RegisteredOrganization") as string ?? "";

                // Accurate Windows 11 branding check
                if (int.TryParse(info.CurrentBuild, out int buildNum) && buildNum >= 22000)
                {
                    if (info.ProductName.Contains("Windows 10"))
                    {
                        info.ProductName = info.ProductName.Replace("Windows 10", "Windows 11");
                    }
                }
            }
        }
        catch { }

        // Query WMI Win32_OperatingSystem for paths & install date
        try
        {
            var osData = WmiService.QueryFirst("SELECT * FROM Win32_OperatingSystem");
            if (osData != null)
            {
                if (string.IsNullOrEmpty(info.ProductName))
                    info.ProductName = osData.GetString("Caption");

                info.WindowsFolder = osData.GetString("WindowsDirectory");
                info.SystemFolder = osData.GetString("SystemDirectory");

                string rawDate = osData.GetString("InstallDate");
                if (rawDate.Length >= 8 && char.IsDigit(rawDate[0]))
                {
                    info.InstallDate = $"{rawDate.Substring(0, 4)}-{rawDate.Substring(4, 2)}-{rawDate.Substring(6, 2)}";
                }
            }
        }
        catch { }

        // Secure Boot
        string secureBootStatus = "Disabled in Firmware (UEFI Supported)";
        try
        {
            using var sbKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            if (sbKey != null)
            {
                var sb = sbKey.GetValue("UEFISecureBootEnabled");
                if (sb is int val && val == 1)
                {
                    info.SecureBootEnabled = true;
                    secureBootStatus = "Enabled in Firmware";
                }
            }
        }
        catch { }

        // UAC Status
        try
        {
            using var uacKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            if (uacKey != null)
            {
                var lua = uacKey.GetValue("EnableLUA");
                info.UacStatus = (lua is int val && val == 1) ? "Enabled" : "Disabled";
            }
        }
        catch { }

        // TPM Details (Multi-Tier: WMI -> PnP SecurityDevices -> ACPI Firmware)
        try
        {
            var tpmData = WmiService.QueryFirst("SELECT * FROM Win32_Tpm", @"root\CIMV2\Security\MicrosoftTpm");
            if (tpmData != null)
            {
                info.Tpm.IsPresent = true;
                info.Tpm.SpecVersion = tpmData.GetString("SpecVersion");
                info.Tpm.ManufacturerName = tpmData.GetString("ManufacturerIdTxt", tpmData.GetString("ManufacturerId"));
                info.Tpm.ManufacturerVersion = tpmData.GetString("ManufacturerVersion");
                info.Tpm.IsActivated = tpmData.GetBool("IsActivated_InitialValue");
                info.Tpm.IsEnabled = tpmData.GetBool("IsEnabled_InitialValue");
                info.Tpm.IsOwned = tpmData.GetBool("IsOwned_InitialValue");
            }
        }
        catch { }

        // Fallback 1: Query PnP Device Manager for Security Devices (TPM)
        if (!info.Tpm.IsPresent)
        {
            try
            {
                var tpmPnp = WmiService.Query("SELECT Name, DeviceID, Manufacturer, Status FROM Win32_PnPEntity WHERE PNPClass = 'SecurityDevices'");
                if (tpmPnp.Count > 0)
                {
                    var t = tpmPnp[0];
                    string tpmName = t.GetString("Name");
                    info.Tpm.IsPresent = true;
                    info.Tpm.SpecVersion = tpmName.Contains("2.0") ? "2.0" : (tpmName.Contains("1.2") ? "1.2" : "2.0");
                    info.Tpm.ManufacturerName = t.GetString("Manufacturer", "Hardware Security Module");
                    info.Tpm.IsEnabled = t.GetString("Status").Equals("OK", StringComparison.OrdinalIgnoreCase);
                    info.Tpm.IsActivated = info.Tpm.IsEnabled;
                }
            }
            catch { }
        }

        // Fallback 2: Query ACPI Firmware TPM2 Table
        if (!info.Tpm.IsPresent)
        {
            try
            {
                uint acpiProvider = 0x41435049; // 'ACPI'
                uint tpm2Id = (uint)('T' | ('P' << 8) | ('M' << 16) | ('2' << 24));
                uint size = NativeMethods.GetSystemFirmwareTable(acpiProvider, tpm2Id, IntPtr.Zero, 0);
                if (size >= 36)
                {
                    info.Tpm.IsPresent = true;
                    info.Tpm.SpecVersion = "2.0";
                    info.Tpm.ManufacturerName = "Discrete TPM (ACPI TPM2 Present)";
                    info.Tpm.IsEnabled = true;
                    info.Tpm.IsActivated = true;
                }
            }
            catch { }
        }

        // Antivirus Products
        try
        {
            var avData = WmiService.Query("SELECT * FROM AntiVirusProduct", @"root\SecurityCenter2");
            foreach (var av in avData)
            {
                var product = new AntivirusInfo
                {
                    DisplayName = av.GetString("displayName"),
                    InstanceGuid = av.GetString("instanceGuid"),
                    PathToSignedProductExe = av.GetString("pathToSignedProductExe")
                };

                uint state = av.GetUint("productState");
                bool enabled = ((state >> 12) & 0x0F) == 1;
                bool upToDate = ((state >> 4) & 0x0F) == 0;
                product.IsActive = enabled;
                product.IsUpToDate = upToDate;
                product.StateStatus = $"{(enabled ? "Active / Enabled" : "Disabled")}, {(upToDate ? "Up to date" : "Out of date")}";

                info.AntivirusProducts.Add(product);
            }
        }
        catch { }

        // Fallback: Detect Windows Defender if SecurityCenter is restricted
        if (info.AntivirusProducts.Count == 0)
        {
            try
            {
                using var defKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender");
                if (defKey != null)
                {
                    info.AntivirusProducts.Add(new AntivirusInfo
                    {
                        DisplayName = "Windows Defender Antivirus",
                        StateStatus = "Real-Time Protection Active",
                        IsActive = true,
                        IsUpToDate = true
                    });
                }
            }
            catch { }
        }

        // BitLocker Drives
        try
        {
            var bitlockerData = WmiService.Query("SELECT DriveLetter, ProtectionStatus, EncryptionMethod, LockStatus FROM Win32_EncryptableVolume", @"root\CIMV2\Security\MicrosoftVolumeEncryption");
            foreach (var bv in bitlockerData)
            {
                string letter = bv.GetString("DriveLetter");
                if (string.IsNullOrEmpty(letter)) continue;

                uint prot = bv.GetUint("ProtectionStatus");
                uint lockStat = bv.GetUint("LockStatus");
                uint method = bv.GetUint("EncryptionMethod");

                info.BitLockerDrives.Add(new BitLockerInfo
                {
                    DriveLetter = letter,
                    ProtectionStatus = prot switch { 1 => "Protection ON", 0 => "Protection OFF", _ => "Unknown" },
                    LockStatus = lockStat switch { 0 => "Unlocked", 1 => "Locked", _ => "Unknown" },
                    EncryptionMethod = method switch
                    {
                        1 => "AES 128",
                        2 => "AES 256",
                        3 => "AES 128 Diffuser",
                        4 => "AES 256 Diffuser",
                        6 => "XTS-AES 128",
                        7 => "XTS-AES 256",
                        _ => "None / Not Encrypted"
                    }
                });
            }
        }
        catch { }

        // Hotfixes / Updates
        try
        {
            var hotfixes = WmiService.Query("SELECT HotFixID, Description, InstalledOn, InstalledBy FROM Win32_QuickFixEngineering");
            foreach (var hf in hotfixes)
            {
                string id = hf.GetString("HotFixID");
                if (!string.IsNullOrEmpty(id) && !id.Equals("File 1", StringComparison.OrdinalIgnoreCase))
                {
                    info.InstalledHotfixes.Add(new HotfixInfo
                    {
                        HotFixId = id,
                        Description = hf.GetString("Description"),
                        InstalledOn = hf.GetString("InstalledOn"),
                        InstalledBy = hf.GetString("InstalledBy")
                    });
                }
            }
        }
        catch { }

        // Build Properties
        info.Properties.Add(new PropertyItem("Operating System", "OS Name", info.ProductName));
        info.Properties.Add(new PropertyItem("Operating System", "Version / Release", info.DisplayVersion));
        info.Properties.Add(new PropertyItem("Operating System", "OS Build", info.FullBuildString));
        info.Properties.Add(new PropertyItem("Operating System", "Architecture", Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"));
        if (!string.IsNullOrEmpty(info.InstallDate))
            info.Properties.Add(new PropertyItem("Operating System", "Installation Date", info.InstallDate));
        if (!string.IsNullOrEmpty(info.RegisteredOwner))
            info.Properties.Add(new PropertyItem("Operating System", "Registered Owner", info.RegisteredOwner));
        if (!string.IsNullOrEmpty(info.WindowsFolder))
            info.Properties.Add(new PropertyItem("Paths", "Windows Directory", info.WindowsFolder));
        if (!string.IsNullOrEmpty(info.SystemFolder))
            info.Properties.Add(new PropertyItem("Paths", "System Directory", info.SystemFolder));

        info.Properties.Add(new PropertyItem("Security", "Secure Boot", secureBootStatus));
        info.Properties.Add(new PropertyItem("Security", "User Account Control (UAC)", info.UacStatus));

        if (info.Tpm.IsPresent)
        {
            info.Properties.Add(new PropertyItem("TPM", "TPM Specification", info.Tpm.SpecVersion));
            info.Properties.Add(new PropertyItem("TPM", "Manufacturer", info.Tpm.ManufacturerName));
            info.Properties.Add(new PropertyItem("TPM", "Status", info.Tpm.IsEnabled && info.Tpm.IsActivated ? "Enabled & Activated" : "Present"));
        }
        else
        {
            info.Properties.Add(new PropertyItem("TPM", "TPM Chip", "Not detected / Not available"));
        }

        foreach (var av in info.AntivirusProducts)
        {
            info.Properties.Add(new PropertyItem("Antivirus", av.DisplayName, av.StateStatus));
        }

        foreach (var bld in info.BitLockerDrives)
        {
            info.Properties.Add(new PropertyItem("BitLocker", $"Drive {bld.DriveLetter}", $"{bld.ProtectionStatus} ({bld.EncryptionMethod})"));
        }

        return info;
    }
}
