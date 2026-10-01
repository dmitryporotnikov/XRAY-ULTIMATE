using System;
using System.Collections.Generic;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class GpuInfoCollector
{
    public static GpuInfo Collect()
    {
        var info = new GpuInfo();

        try
        {
            var gpus = WmiService.Query("SELECT * FROM Win32_VideoController");
            foreach (var g in gpus)
            {
                var card = new GraphicsCardInfo
                {
                    Name = g.GetString("Name"),
                    AdapterRamBytes = g.GetUlong("AdapterRAM"),
                    DriverVersion = g.GetString("DriverVersion"),
                    DriverDate = g.GetString("DriverDate"),
                    VideoProcessor = g.GetString("VideoProcessor"),
                    CurrentHorizontalResolution = g.GetUint("CurrentHorizontalResolution"),
                    CurrentVerticalResolution = g.GetUint("CurrentVerticalResolution"),
                    CurrentRefreshRate = g.GetUint("CurrentRefreshRate"),
                    VideoModeDescription = g.GetString("VideoModeDescription"),
                    Status = g.GetString("Status", "OK"),
                    DeviceId = g.GetString("PNPDeviceID")
                };

                if (card.DriverDate.Length >= 8)
                {
                    string d = card.DriverDate;
                    if (d.Length >= 8 && char.IsDigit(d[0]) && char.IsDigit(d[4]))
                        card.DriverDate = $"{d.Substring(0, 4)}-{d.Substring(4, 2)}-{d.Substring(6, 2)}";
                }

                info.Adapters.Add(card);
            }
        }
        catch { }

        try
        {
            var monitors = WmiService.Query("SELECT * FROM Win32_DesktopMonitor");
            foreach (var m in monitors)
            {
                var mon = new MonitorInfo
                {
                    Name = m.GetString("Name", m.GetString("Caption")),
                    Manufacturer = m.GetString("MonitorManufacturer"),
                    ScreenWidth = m.GetUint("ScreenWidth"),
                    ScreenHeight = m.GetUint("ScreenHeight"),
                    MonitorType = m.GetString("MonitorType")
                };
                if (!string.IsNullOrEmpty(mon.Name))
                {
                    info.Monitors.Add(mon);
                }
            }
        }
        catch { }

        int gpuIdx = 1;
        foreach (var gpu in info.Adapters)
        {
            string cat = $"GPU {gpuIdx}: {gpu.Name}";
            info.Properties.Add(new PropertyItem(cat, "Adapter Name", gpu.Name));
            if (gpu.AdapterRamBytes > 0)
                info.Properties.Add(new PropertyItem(cat, "Installed VRAM", UnitFormatter.FormatBytes(gpu.AdapterRamBytes)));
            if (!string.IsNullOrEmpty(gpu.VideoProcessor))
                info.Properties.Add(new PropertyItem(cat, "Video Processor", gpu.VideoProcessor));
            if (!string.IsNullOrEmpty(gpu.DriverVersion))
                info.Properties.Add(new PropertyItem(cat, "Driver Version", gpu.DriverVersion));
            if (!string.IsNullOrEmpty(gpu.DriverDate))
                info.Properties.Add(new PropertyItem(cat, "Driver Date", gpu.DriverDate));
            if (gpu.CurrentHorizontalResolution > 0 && gpu.CurrentVerticalResolution > 0)
                info.Properties.Add(new PropertyItem(cat, "Current Resolution", $"{gpu.CurrentHorizontalResolution} x {gpu.CurrentVerticalResolution} @ {gpu.CurrentRefreshRate} Hz"));
            if (!string.IsNullOrEmpty(gpu.VideoModeDescription))
                info.Properties.Add(new PropertyItem(cat, "Mode", gpu.VideoModeDescription));
            info.Properties.Add(new PropertyItem(cat, "Status", gpu.Status));

            gpuIdx++;
        }

        int monIdx = 1;
        foreach (var mon in info.Monitors)
        {
            string cat = $"Display {monIdx}: {mon.Name}";
            info.Properties.Add(new PropertyItem(cat, "Monitor", mon.Name));
            if (!string.IsNullOrEmpty(mon.Manufacturer))
                info.Properties.Add(new PropertyItem(cat, "Manufacturer", mon.Manufacturer));
            if (mon.ScreenWidth > 0 && mon.ScreenHeight > 0)
                info.Properties.Add(new PropertyItem(cat, "Native Resolution", $"{mon.ScreenWidth} x {mon.ScreenHeight}"));
            monIdx++;
        }

        return info;
    }
}
