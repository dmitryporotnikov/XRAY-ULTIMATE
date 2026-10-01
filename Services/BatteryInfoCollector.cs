using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class BatteryInfoCollector
{
    public static BatteryInfo Collect()
    {
        var info = new BatteryInfo();

        // Query Win32 GetSystemPowerStatus
        try
        {
            if (NativeMethods.GetSystemPowerStatus(out var status))
            {
                info.IsBatteryPresent = status.BatteryFlag != 128 && status.BatteryFlag != 255;
                info.BatteryLifePercent = status.BatteryLifePercent == 255 ? -1 : status.BatteryLifePercent;
                info.PowerLineStatus = status.ACLineStatus switch
                {
                    0 => "On Battery (Offline)",
                    1 => "AC Connected (Online)",
                    _ => "Unknown"
                };

                info.BatteryState = status.BatteryFlag switch
                {
                    1 => "High (>66%)",
                    2 => "Low (<33%)",
                    4 => "Critical (<5%)",
                    8 => "Charging",
                    128 => "No System Battery",
                    _ => (status.BatteryFlag & 8) != 0 ? "Charging" : (status.ACLineStatus == 1 ? "Fully Charged / Plugged In" : "Discharging")
                };

                info.EstimatedRunTimeSeconds = status.BatteryLifeTime;
            }
        }
        catch { }

        // Query WMI Win32_Battery
        try
        {
            var batteryData = WmiService.QueryFirst("SELECT * FROM Win32_Battery");
            if (batteryData != null)
            {
                info.IsBatteryPresent = true;
                info.DeviceName = batteryData.GetString("Name", batteryData.GetString("DeviceID"));
                info.DesignCapacityMWh = batteryData.GetUint("DesignCapacity");
                info.FullChargeCapacityMWh = batteryData.GetUint("FullChargeCapacity");
                info.Chemistry = batteryData.GetString("Chemistry");

                uint chemCode = batteryData.GetUint("Chemistry");
                if (chemCode > 0)
                {
                    info.Chemistry = chemCode switch
                    {
                        1 => "Other",
                        2 => "Unknown",
                        3 => "Lead Acid",
                        4 => "Nickel Cadmium",
                        5 => "Nickel Metal Hydride",
                        6 => "Lithium-ion",
                        7 => "Zinc air",
                        8 => "Lithium Polymer",
                        _ => "Lithium-ion"
                    };
                }

                if (info.DesignCapacityMWh > 0 && info.FullChargeCapacityMWh > 0)
                {
                    info.BatteryHealthPercent = Math.Min(100.0, (double)info.FullChargeCapacityMWh / info.DesignCapacityMWh * 100.0);
                }
            }
        }
        catch { }

        // Active Power Scheme
        info.ActivePowerScheme = QueryActivePowerScheme();

        // Populate Properties
        info.Properties.Add(new PropertyItem("Power State", "Battery Present", info.IsBatteryPresent ? "Yes" : "No (Desktop / AC Only)"));
        info.Properties.Add(new PropertyItem("Power State", "Power Source", info.PowerLineStatus));
        if (info.IsBatteryPresent)
        {
            info.Properties.Add(new PropertyItem("Battery Status", "Current Charge", info.BatteryLifePercent >= 0 ? $"{info.BatteryLifePercent}%" : "Unknown"));
            info.Properties.Add(new PropertyItem("Battery Status", "Battery State", info.BatteryState));
            if (info.EstimatedRunTimeSeconds > 0 && info.EstimatedRunTimeSeconds < 72000)
            {
                var span = TimeSpan.FromSeconds(info.EstimatedRunTimeSeconds);
                info.Properties.Add(new PropertyItem("Battery Status", "Estimated Runtime Remaining", $"{span.Hours}h {span.Minutes}m"));
            }
            if (info.DesignCapacityMWh > 0)
                info.Properties.Add(new PropertyItem("Capacity", "Design Capacity", $"{info.DesignCapacityMWh} mWh"));
            if (info.FullChargeCapacityMWh > 0)
                info.Properties.Add(new PropertyItem("Capacity", "Full Charge Capacity", $"{info.FullChargeCapacityMWh} mWh"));
            if (info.BatteryHealthPercent > 0)
                info.Properties.Add(new PropertyItem("Capacity", "Battery Health", $"{info.BatteryHealthPercent:F1}%"));
            if (!string.IsNullOrEmpty(info.Chemistry))
                info.Properties.Add(new PropertyItem("Hardware", "Chemistry / Technology", info.Chemistry));
            if (!string.IsNullOrEmpty(info.DeviceName))
                info.Properties.Add(new PropertyItem("Hardware", "Device Identifier", info.DeviceName));
        }

        info.Properties.Add(new PropertyItem("Power Management", "Active Power Scheme", info.ActivePowerScheme));

        return info;
    }

    private static string QueryActivePowerScheme()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = "/getactivescheme",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(2000);
                var match = Regex.Match(output, @"\(([^)]+)\)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        catch { }

        return "Balanced";
    }
}
