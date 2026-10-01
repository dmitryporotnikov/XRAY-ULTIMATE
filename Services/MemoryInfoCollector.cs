using System;
using System.Collections.Generic;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class MemoryInfoCollector
{
    public static MemoryInfo Collect()
    {
        var info = new MemoryInfo();

        try
        {
            var memStatus = new NativeMethods.MEMORYSTATUSEX();
            if (NativeMethods.GlobalMemoryStatusEx(memStatus))
            {
                info.MemoryLoadPercent = memStatus.dwMemoryLoad;
                info.TotalPhysicalBytes = memStatus.ullTotalPhys;
                info.AvailablePhysicalBytes = memStatus.ullAvailPhys;
                info.TotalPageFileBytes = memStatus.ullTotalPageFile;
                info.AvailablePageFileBytes = memStatus.ullAvailPageFile;
                info.TotalVirtualBytes = memStatus.ullTotalVirtual;
                info.AvailableVirtualBytes = memStatus.ullAvailVirtual;
            }
        }
        catch { }

        // Enumerate physical memory modules (DIMM / SODIMM)
        try
        {
            var modules = WmiService.Query("SELECT * FROM Win32_PhysicalMemory");
            foreach (var m in modules)
            {
                var stick = new PhysicalMemoryStick
                {
                    BankLabel = m.GetString("BankLabel"),
                    DeviceLocator = m.GetString("DeviceLocator"),
                    CapacityBytes = m.GetUlong("Capacity"),
                    SpeedMHz = m.GetUint("Speed"),
                    ConfiguredClockSpeedMHz = m.GetUint("ConfiguredClockSpeed", m.GetUint("Speed")),
                    Manufacturer = m.GetString("Manufacturer"),
                    PartNumber = m.GetString("PartNumber"),
                    SerialNumber = m.GetString("SerialNumber"),
                    DataWidth = (ushort)m.GetUint("DataWidth", 64)
                };

                uint smbiosMemoryType = m.GetUint("SMBIOSMemoryType");
                stick.MemoryType = TranslateSmbiosMemoryType(smbiosMemoryType);

                uint formFactor = m.GetUint("FormFactor");
                stick.FormFactor = formFactor switch
                {
                    8 => "DIMM",
                    12 => "SODIMM",
                    9 => "SIMM",
                    _ => "DIMM"
                };

                info.MemorySticks.Add(stick);
            }
        }
        catch { }

        // Add to Properties
        info.Properties.Add(new PropertyItem("Physical Memory", "Total Installed RAM", UnitFormatter.FormatBytes(info.TotalPhysicalBytes)));
        info.Properties.Add(new PropertyItem("Physical Memory", "Used RAM", UnitFormatter.FormatBytes(info.UsedPhysicalBytes)));
        info.Properties.Add(new PropertyItem("Physical Memory", "Available RAM", UnitFormatter.FormatBytes(info.AvailablePhysicalBytes)));
        info.Properties.Add(new PropertyItem("Physical Memory", "Memory Utilization", $"{info.MemoryLoadPercent:F0}%"));

        info.Properties.Add(new PropertyItem("Paging / Swap", "Total Pagefile Size", UnitFormatter.FormatBytes(info.TotalPageFileBytes)));
        info.Properties.Add(new PropertyItem("Paging / Swap", "Used Pagefile", UnitFormatter.FormatBytes(info.UsedPageFileBytes)));
        info.Properties.Add(new PropertyItem("Paging / Swap", "Free Pagefile", UnitFormatter.FormatBytes(info.AvailablePageFileBytes)));

        info.Properties.Add(new PropertyItem("Virtual Memory", "Total Virtual Memory", UnitFormatter.FormatBytes(info.TotalVirtualBytes)));
        info.Properties.Add(new PropertyItem("Virtual Memory", "Available Virtual Memory", UnitFormatter.FormatBytes(info.AvailableVirtualBytes)));

        int stickIndex = 1;
        foreach (var stick in info.MemorySticks)
        {
            string cat = $"Slot {stickIndex}: {stick.DeviceLocator}";
            info.Properties.Add(new PropertyItem(cat, "Capacity", UnitFormatter.FormatBytes(stick.CapacityBytes)));
            info.Properties.Add(new PropertyItem(cat, "Type", stick.MemoryType));
            info.Properties.Add(new PropertyItem(cat, "Speed", $"{stick.SpeedMHz} MHz (Configured: {stick.ConfiguredClockSpeedMHz} MHz)"));
            info.Properties.Add(new PropertyItem(cat, "Manufacturer", stick.Manufacturer));
            info.Properties.Add(new PropertyItem(cat, "Part Number", stick.PartNumber));
            info.Properties.Add(new PropertyItem(cat, "Serial Number", stick.SerialNumber));
            info.Properties.Add(new PropertyItem(cat, "Form Factor", stick.FormFactor));
            stickIndex++;
        }

        return info;
    }

    private static string TranslateSmbiosMemoryType(uint type) => type switch
    {
        20 => "DDR",
        21 => "DDR2",
        22 => "DDR2 FB-DIMM",
        24 => "DDR3",
        25 => "FBD2",
        26 => "DDR4",
        27 => "LPDDR",
        28 => "LPDDR2",
        29 => "LPDDR3",
        30 => "LPDDR4",
        31 => "Logical non-volatile device",
        32 => "HBM",
        33 => "HBM2",
        34 => "DDR5",
        35 => "LPDDR5",
        _ => "DDR4/DDR5 SDRAM"
    };
}
