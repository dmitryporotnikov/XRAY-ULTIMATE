using System;
using System.Diagnostics;
using System.Threading.Tasks;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public class SystemDiagnosticsService
{
    private static SystemDiagnosticsService? _instance;
    public static SystemDiagnosticsService Instance => _instance ??= new SystemDiagnosticsService();

    public SystemReport CurrentReport { get; private set; } = new();

    public async Task<SystemReport> RefreshAllAsync(IProgress<string>? progress = null)
    {
        var report = new SystemReport();

        progress?.Report("Scanning Processor (CPU)...");
        report.Cpu = await Task.Run(() => CpuInfoCollector.Collect());

        progress?.Report("Scanning Physical & Virtual Memory (RAM)...");
        report.Memory = await Task.Run(() => MemoryInfoCollector.Collect());

        progress?.Report("Scanning Motherboard & BIOS/UEFI Firmware...");
        report.Motherboard = await Task.Run(() => MotherboardInfoCollector.Collect());

        progress?.Report("Scanning GPU & Connected Displays...");
        report.Gpu = await Task.Run(() => GpuInfoCollector.Collect());

        progress?.Report("Scanning Physical Disks & Volumes...");
        report.Storage = await Task.Run(() => StorageInfoCollector.Collect());

        progress?.Report("Scanning Network Interfaces & Wi-Fi...");
        report.Network = await Task.Run(() => NetworkInfoCollector.Collect());

        progress?.Report("Scanning Battery & Power Subsystem...");
        report.Battery = await Task.Run(() => BatteryInfoCollector.Collect());

        progress?.Report("Scanning Windows OS, Security, TPM & Updates...");
        report.Security = await Task.Run(() => OsSecurityInfoCollector.Collect());

        progress?.Report("Querying Entra ID / dsregcmd Diagnostics...");
        report.Dsregcmd = await DsregcmdCollector.CollectAsync();

        progress?.Report("Enumerating Installed Software Applications...");
        report.Software = await Task.Run(() => SoftwareCollector.Collect());

        progress?.Report("Querying Active Processes & Windows Services...");
        report.ProcessService = await Task.Run(() => ProcessServiceCollector.Collect());

        progress?.Report("Querying Network & Filesystem Filter Drivers (fltmc)...");
        report.FilterDrivers = await Task.Run(() => FilterDriverCollector.Collect());

        progress?.Report("Reading ACPI Firmware Tables & Parsing DSDT...");
        report.Acpi = await Task.Run(() => AcpiService.Collect());

        progress?.Report("Enumerating Hardware Devices (Device Manager)...");
        report.DeviceManager = await Task.Run(() => DeviceManagerCollector.Collect());

        progress?.Report("Building System Summary Overview...");
        report.Overview = BuildOverview(report);

        report.GeneratedAt = DateTime.Now;
        CurrentReport = report;

        progress?.Report("Ready.");
        return report;
    }

    private SystemOverviewInfo BuildOverview(SystemReport report)
    {
        var ov = new SystemOverviewInfo
        {
            ComputerName = Environment.MachineName,
            UserName = Environment.UserName,
            DomainOrWorkgroup = Environment.UserDomainName,
            OsName = report.Security.ProductName,
            OsVersion = report.Security.DisplayVersion,
            OsBuild = report.Security.FullBuildString,
            OsArchitecture = Environment.Is64BitOperatingSystem ? "64-bit Operating System, x64-based processor" : "32-bit",
            InstallDate = report.Security.InstallDate,
            BootMode = report.Motherboard.Properties.Find(p => p.Property.Contains("Boot Mode"))?.Value ?? "UEFI",
            SecureBootState = report.Security.SecureBootEnabled ? "Enabled" : "Disabled",
            TimeZone = TimeZoneInfo.Local.DisplayName
        };

        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        ov.SystemUptime = UnitFormatter.FormatUptime(uptime);

        if (report.Security.BitLockerDrives.Count > 0)
        {
            ov.BitLockerStatus = report.Security.BitLockerDrives[0].ProtectionStatus;
        }
        else
        {
            ov.BitLockerStatus = "Not Active";
        }

        ov.Properties.Add(new PropertyItem("Computer", "Computer Name", ov.ComputerName));
        ov.Properties.Add(new PropertyItem("Computer", "Current User", ov.UserName));
        ov.Properties.Add(new PropertyItem("Computer", "Domain / Workgroup", ov.DomainOrWorkgroup));
        ov.Properties.Add(new PropertyItem("Operating System", "OS Name", ov.OsName));
        ov.Properties.Add(new PropertyItem("Operating System", "OS Version", ov.OsVersion));
        ov.Properties.Add(new PropertyItem("Operating System", "Build Number", ov.OsBuild));
        ov.Properties.Add(new PropertyItem("Operating System", "System Uptime", ov.SystemUptime));
        ov.Properties.Add(new PropertyItem("Hardware Summary", "Processor", report.Cpu.ProcessorName));
        ov.Properties.Add(new PropertyItem("Hardware Summary", "Cores / Threads", $"{report.Cpu.CoreCount} Cores / {report.Cpu.LogicalProcessorCount} Threads"));
        ov.Properties.Add(new PropertyItem("Hardware Summary", "Installed Memory", UnitFormatter.FormatBytes(report.Memory.TotalPhysicalBytes)));
        if (report.Gpu.Adapters.Count > 0)
            ov.Properties.Add(new PropertyItem("Hardware Summary", "Primary Graphics", report.Gpu.Adapters[0].Name));
        ov.Properties.Add(new PropertyItem("Firmware & Security", "Motherboard", $"{report.Motherboard.Manufacturer} {report.Motherboard.Product}"));
        ov.Properties.Add(new PropertyItem("Firmware & Security", "BIOS Version", report.Motherboard.BiosVersion));
        ov.Properties.Add(new PropertyItem("Firmware & Security", "Boot Mode", ov.BootMode));
        ov.Properties.Add(new PropertyItem("Firmware & Security", "Secure Boot", ov.SecureBootState));
        ov.Properties.Add(new PropertyItem("Firmware & Security", "TPM Status", report.Security.Tpm.IsPresent ? $"TPM {report.Security.Tpm.SpecVersion} Present" : "Not Present"));
        ov.Properties.Add(new PropertyItem("Cloud / Identity", "Entra ID / Azure AD", report.Dsregcmd.OverallStatus));

        return ov;
    }
}
