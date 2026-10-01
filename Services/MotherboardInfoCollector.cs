using System;
using System.Collections.Generic;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class MotherboardInfoCollector
{
    public static MotherboardInfo Collect()
    {
        var info = new MotherboardInfo();

        try
        {
            var board = WmiService.QueryFirst("SELECT * FROM Win32_BaseBoard");
            if (board != null)
            {
                info.Manufacturer = board.GetString("Manufacturer");
                info.Product = board.GetString("Product");
                info.Version = board.GetString("Version");
                info.SerialNumber = board.GetString("SerialNumber");
            }
        }
        catch { }

        try
        {
            var bios = WmiService.QueryFirst("SELECT * FROM Win32_BIOS");
            if (bios != null)
            {
                info.BiosVendor = bios.GetString("Manufacturer");
                info.BiosVersion = bios.GetString("SMBIOSBIOSVersion", bios.GetString("Version"));
                info.BiosReleaseDate = bios.GetString("ReleaseDate");
                info.SmbiosVersion = $"{bios.GetUint("SMBIOSMajorVersion")}.{bios.GetUint("SMBIOSMinorVersion")}";

                if (info.BiosReleaseDate.Length >= 8)
                {
                    // WMI dates are formatted as yyyymmdd...
                    string raw = info.BiosReleaseDate;
                    if (raw.Length >= 8 && char.IsDigit(raw[0]) && char.IsDigit(raw[4]))
                    {
                        info.BiosReleaseDate = $"{raw.Substring(0, 4)}-{raw.Substring(4, 2)}-{raw.Substring(6, 2)}";
                    }
                }
            }
        }
        catch { }

        try
        {
            var systemProduct = WmiService.QueryFirst("SELECT * FROM Win32_ComputerSystemProduct");
            if (systemProduct != null)
            {
                info.SystemFamily = systemProduct.GetString("Family");
                info.SystemSku = systemProduct.GetString("SKUNumber");
            }
        }
        catch { }

        string firmwareType = "Unknown";
        if (NativeMethods.GetFirmwareType(out var ft))
        {
            firmwareType = ft switch
            {
                NativeMethods.FIRMWARE_TYPE.FirmwareTypeBios => "Legacy BIOS",
                NativeMethods.FIRMWARE_TYPE.FirmwareTypeUefi => "UEFI",
                _ => "Unknown"
            };
        }

        info.Properties.Add(new PropertyItem("Motherboard", "Manufacturer", info.Manufacturer));
        info.Properties.Add(new PropertyItem("Motherboard", "Model / Product", info.Product));
        if (!string.IsNullOrEmpty(info.Version))
            info.Properties.Add(new PropertyItem("Motherboard", "Version", info.Version));
        if (!string.IsNullOrEmpty(info.SerialNumber))
            info.Properties.Add(new PropertyItem("Motherboard", "Serial Number", info.SerialNumber));

        info.Properties.Add(new PropertyItem("BIOS / Firmware", "BIOS Vendor", info.BiosVendor));
        info.Properties.Add(new PropertyItem("BIOS / Firmware", "BIOS Version", info.BiosVersion));
        info.Properties.Add(new PropertyItem("BIOS / Firmware", "Release Date", info.BiosReleaseDate));
        info.Properties.Add(new PropertyItem("BIOS / Firmware", "SMBIOS Version", info.SmbiosVersion));
        info.Properties.Add(new PropertyItem("BIOS / Firmware", "Boot Mode / Firmware Type", firmwareType));

        if (!string.IsNullOrEmpty(info.SystemFamily))
            info.Properties.Add(new PropertyItem("System", "System Family", info.SystemFamily));
        if (!string.IsNullOrEmpty(info.SystemSku))
            info.Properties.Add(new PropertyItem("System", "System SKU", info.SystemSku));

        return info;
    }
}
