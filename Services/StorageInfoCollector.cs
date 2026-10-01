using System;
using System.Collections.Generic;
using System.IO;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class StorageInfoCollector
{
    public static StorageInfo Collect()
    {
        var info = new StorageInfo();

        // Query physical disk drives
        try
        {
            var disks = WmiService.Query("SELECT * FROM Win32_DiskDrive");
            foreach (var d in disks)
            {
                var disk = new PhysicalDiskInfo
                {
                    Model = d.GetString("Model"),
                    InterfaceType = d.GetString("InterfaceType"),
                    SizeBytes = d.GetUlong("Size"),
                    SerialNumber = d.GetString("SerialNumber"),
                    DeviceId = d.GetString("DeviceID"),
                    Partitions = d.GetUint("Partitions"),
                    Status = d.GetString("Status", "OK"),
                    FirmwareRevision = d.GetString("FirmwareRevision")
                };

                // Infer media type
                string modelLower = disk.Model.ToLowerInvariant();
                if (modelLower.Contains("nvme") || modelLower.Contains("ssd") || modelLower.Contains("optane"))
                {
                    disk.MediaType = "Solid State Drive (SSD / NVMe)";
                }
                else
                {
                    disk.MediaType = "Hard Disk Drive (HDD)";
                }

                info.PhysicalDisks.Add(disk);
            }
        }
        catch { }

        // Attempt refined media type from Storage WMI namespace
        try
        {
            var msftDisks = WmiService.Query("SELECT FriendlyName, MediaType, BusType FROM MSFT_PhysicalDisk", @"root\Microsoft\Windows\Storage");
            foreach (var md in msftDisks)
            {
                string fn = md.GetString("FriendlyName");
                uint mt = md.GetUint("MediaType"); // 3=HDD, 4=SSD, 5=SCM
                uint bt = md.GetUint("BusType"); // 17=NVMe, 11=SATA, 8=SCSI, 7=USB

                foreach (var physicalDisk in info.PhysicalDisks)
                {
                    if (physicalDisk.Model.Contains(fn, StringComparison.OrdinalIgnoreCase) ||
                        fn.Contains(physicalDisk.Model, StringComparison.OrdinalIgnoreCase))
                    {
                        string bus = bt switch
                        {
                            17 => "NVMe",
                            11 => "SATA",
                            7 => "USB",
                            8 => "SCSI",
                            _ => physicalDisk.InterfaceType
                        };
                        string type = mt switch
                        {
                            4 => "SSD",
                            3 => "HDD",
                            5 => "SCM",
                            _ => physicalDisk.MediaType
                        };
                        physicalDisk.MediaType = $"{type} ({bus})";
                        physicalDisk.InterfaceType = bus;
                    }
                }
            }
        }
        catch { }

        // Enumerate Logical Drives
        try
        {
            var drives = DriveInfo.GetDrives();
            foreach (var d in drives)
            {
                if (!d.IsReady)
                {
                    info.Volumes.Add(new VolumeInfo
                    {
                        DriveLetter = d.Name,
                        DriveType = d.DriveType.ToString(),
                        IsReady = false
                    });
                    continue;
                }

                info.Volumes.Add(new VolumeInfo
                {
                    DriveLetter = d.Name,
                    VolumeLabel = string.IsNullOrEmpty(d.VolumeLabel) ? "Local Disk" : d.VolumeLabel,
                    FileSystem = d.DriveFormat,
                    TotalSizeBytes = (ulong)d.TotalSize,
                    FreeSizeBytes = (ulong)d.AvailableFreeSpace,
                    DriveType = d.DriveType.ToString(),
                    IsReady = true
                });
            }
        }
        catch { }

        // Populate Properties
        int diskIndex = 0;
        foreach (var disk in info.PhysicalDisks)
        {
            string cat = $"Disk {diskIndex}: {disk.Model}";
            info.Properties.Add(new PropertyItem(cat, "Model", disk.Model));
            info.Properties.Add(new PropertyItem(cat, "Capacity", UnitFormatter.FormatBytes(disk.SizeBytes)));
            info.Properties.Add(new PropertyItem(cat, "Media Type", disk.MediaType));
            info.Properties.Add(new PropertyItem(cat, "Interface", disk.InterfaceType));
            if (!string.IsNullOrEmpty(disk.SerialNumber))
                info.Properties.Add(new PropertyItem(cat, "Serial Number", disk.SerialNumber.Trim()));
            if (!string.IsNullOrEmpty(disk.FirmwareRevision))
                info.Properties.Add(new PropertyItem(cat, "Firmware Revision", disk.FirmwareRevision.Trim()));
            info.Properties.Add(new PropertyItem(cat, "Partitions", disk.Partitions.ToString()));
            info.Properties.Add(new PropertyItem(cat, "Health / Status", disk.Status));
            diskIndex++;
        }

        foreach (var vol in info.Volumes)
        {
            if (!vol.IsReady) continue;
            string cat = $"Volume {vol.DriveLetter} ({vol.VolumeLabel})";
            info.Properties.Add(new PropertyItem(cat, "Drive", vol.DriveLetter));
            info.Properties.Add(new PropertyItem(cat, "Volume Label", vol.VolumeLabel));
            info.Properties.Add(new PropertyItem(cat, "File System", vol.FileSystem));
            info.Properties.Add(new PropertyItem(cat, "Total Size", UnitFormatter.FormatBytes(vol.TotalSizeBytes)));
            info.Properties.Add(new PropertyItem(cat, "Used Space", $"{UnitFormatter.FormatBytes(vol.UsedSizeBytes)} ({vol.PercentUsed:F1}%)"));
            info.Properties.Add(new PropertyItem(cat, "Free Space", UnitFormatter.FormatBytes(vol.FreeSizeBytes)));
            info.Properties.Add(new PropertyItem(cat, "Drive Type", vol.DriveType));
        }

        return info;
    }
}
