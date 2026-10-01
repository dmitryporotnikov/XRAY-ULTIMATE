using System;
using System.Collections.Generic;
using System.Linq;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class DeviceManagerCollector
{
    public static DeviceManagerReport Collect()
    {
        var report = new DeviceManagerReport();
        var allDevices = new List<DeviceItem>();

        try
        {
            var pnpList = WmiService.Query("SELECT Name, Caption, Description, DeviceID, PNPClass, Manufacturer, Status, ConfigManagerErrorCode, Service FROM Win32_PnPEntity");
            foreach (var p in pnpList)
            {
                string name = p.GetString("Name", p.GetString("Caption"));
                if (string.IsNullOrWhiteSpace(name)) continue;

                string pnpClass = p.GetString("PNPClass", "Other");
                if (string.IsNullOrWhiteSpace(pnpClass)) pnpClass = "Other";

                uint errorCode = p.GetUint("ConfigManagerErrorCode");
                string status = p.GetString("Status", "OK");

                var item = new DeviceItem
                {
                    Name = name,
                    DeviceClass = pnpClass,
                    FriendlyClassName = GetFriendlyClassName(pnpClass),
                    Manufacturer = p.GetString("Manufacturer", "Standard System Device"),
                    DeviceId = p.GetString("DeviceID"),
                    Status = status,
                    ErrorCode = errorCode,
                    StatusMessage = GetStatusMessage(errorCode, status),
                    Service = p.GetString("Service"),
                    ClassIconGlyph = GetClassGlyph(pnpClass),
                    HasProblem = errorCode != 0 || !status.Equals("OK", StringComparison.OrdinalIgnoreCase)
                };

                allDevices.Add(item);
            }
        }
        catch { }

        // Sort all devices
        allDevices = allDevices.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToList();
        report.AllDevices = allDevices;
        report.TotalDevicesCount = allDevices.Count;
        report.ProblemDevicesCount = allDevices.Count(d => d.HasProblem);

        // Group by Device Class
        var groups = allDevices
            .GroupBy(d => d.FriendlyClassName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new DeviceClassGroup
            {
                ClassName = g.First().DeviceClass,
                FriendlyName = g.Key,
                IconGlyph = g.First().ClassIconGlyph,
                Devices = g.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToList()
            })
            .OrderBy(g => g.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        report.Groups = groups;

        // Populate Properties
        report.Properties.Add(new PropertyItem("Summary", "Total Hardware Devices", report.TotalDevicesCount.ToString()));
        report.Properties.Add(new PropertyItem("Summary", "Device Classes", report.Groups.Count.ToString()));
        report.Properties.Add(new PropertyItem("Summary", "Devices with Issues", report.ProblemDevicesCount.ToString()));

        foreach (var g in report.Groups)
        {
            report.Properties.Add(new PropertyItem("Device Classes", g.FriendlyName, $"{g.DeviceCount} device(s)"));
        }

        return report;
    }

    private static string GetFriendlyClassName(string pnpClass) => pnpClass.ToLowerInvariant() switch
    {
        "audioendpoint" => "Audio Inputs & Outputs",
        "battery" => "Batteries",
        "biometric" => "Biometric Devices",
        "bluetooth" => "Bluetooth Radios",
        "camera" or "image" => "Cameras & Imaging",
        "computerdriver" or "computer" => "Computer",
        "diskdrive" => "Disk Drives",
        "display" => "Display Adapters",
        "firmware" => "Firmware & UEFI",
        "hidclass" => "Human Interface Devices (HID)",
        "keyboard" => "Keyboards",
        "mouse" => "Mice and Other Pointing Devices",
        "monitor" => "Monitors",
        "net" => "Network Adapters",
        "printqueue" or "printer" => "Print Queues & Printers",
        "processor" => "Processors",
        "securitydevices" => "Security Devices (TPM)",
        "softwarecomponent" => "Software Components",
        "softwaredevice" => "Software Devices",
        "media" => "Sound, Video and Game Controllers",
        "scsiadapter" or "hdc" => "Storage Controllers",
        "system" => "System Devices & Chipset",
        "usb" => "Universal Serial Bus (USB) Controllers",
        "ports" => "Ports (COM & LPT)",
        "sensors" => "Sensors",
        _ => pnpClass
    };

    private static string GetClassGlyph(string pnpClass) => pnpClass.ToLowerInvariant() switch
    {
        "audioendpoint" => "\uE767",     // Volume
        "battery" => "\uEA93",           // Battery
        "bluetooth" => "\uE702",         // Bluetooth
        "camera" or "image" => "\uE722", // Camera
        "computer" => "\uE7F8",          // PC
        "diskdrive" => "\uEDA2",         // Drive
        "display" => "\uE790",           // Video
        "firmware" => "\uEB51",          // Chip/Firmware
        "hidclass" => "\uE961",          // Controller
        "keyboard" => "\uE765",          // Keyboard
        "mouse" => "\uE962",             // Mouse
        "monitor" => "\uE7F4",           // Display
        "net" => "\uE704",               // Network
        "printqueue" or "printer" => "\uE749", // Printer
        "processor" => "\uE950",         // CPU
        "securitydevices" => "\uEA18",   // Shield
        "softwarecomponent" => "\uE71D", // Component
        "media" => "\uE8D6",             // Media
        "scsiadapter" or "hdc" => "\uE958", // Storage
        "system" => "\uE770",            // Chipset
        "usb" => "\uE88E",               // USB
        "ports" => "\uEA86",             // Plug
        _ => "\uE9A9"                    // Device
    };

    private static string GetStatusMessage(uint errorCode, string status) => errorCode switch
    {
        0 => "This device is working properly.",
        1 => "This device is not configured correctly (Code 1).",
        10 => "This device cannot start (Code 10).",
        14 => "This device cannot work properly until you restart your computer (Code 14).",
        22 => "This device is disabled (Code 22).",
        28 => "The drivers for this device are not installed (Code 28).",
        43 => "Windows stopped this device because it reported problems (Code 43).",
        _ => status.Equals("OK", StringComparison.OrdinalIgnoreCase) ? "This device is working properly." : $"Device Status: {status} (Code {errorCode})"
    };
}
