using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public class SystemDiagnosticsService
{
    private static SystemDiagnosticsService? _instance;
    public static SystemDiagnosticsService Instance => _instance ??= new SystemDiagnosticsService();

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public SystemReport CurrentReport { get; private set; } = new();

    public async Task<SystemReport> RefreshAllAsync(IProgress<string>? progress = null)
    {
        await _refreshLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var previousBenchmark = CurrentReport?.Benchmark;
            var report = new SystemReport
            {
                Benchmark = previousBenchmark
            };

            T RunStep<T>(string statusMessage, Func<T> collector)
            {
                progress?.Report(statusMessage);
                return collector();
            }

            // Wave 1: Core hardware & firmware collectors in parallel
            progress?.Report("Scanning CPU, Memory, Motherboard, GPU & Storage...");
            var cpuTask = Task.Run(() => RunStep("Scanning Processor (CPU)...", CpuInfoCollector.Collect));
            var memTask = Task.Run(() => RunStep("Scanning Physical & Virtual Memory (RAM)...", MemoryInfoCollector.Collect));
            var mbTask = Task.Run(() => RunStep("Scanning Motherboard & BIOS/UEFI Firmware...", MotherboardInfoCollector.Collect));
            var gpuTask = Task.Run(() => RunStep("Scanning GPU & Connected Displays...", GpuInfoCollector.Collect));
            var storageTask = Task.Run(() => RunStep("Scanning Physical Disks & Volumes...", StorageInfoCollector.Collect));
            var batteryTask = Task.Run(() => RunStep("Scanning Battery & Power Subsystem...", BatteryInfoCollector.Collect));

            await Task.WhenAll(cpuTask, memTask, mbTask, gpuTask, storageTask, batteryTask).ConfigureAwait(false);

            report.Cpu = cpuTask.Result;
            report.Memory = memTask.Result;
            report.Motherboard = mbTask.Result;
            report.Gpu = gpuTask.Result;
            report.Storage = storageTask.Result;
            report.Battery = batteryTask.Result;

            // Wave 2: OS, Security, Network, Cloud Identity, Software, Processes, Filters, ACPI & Device Manager
            progress?.Report("Scanning Network, Security, ACPI Tables & System Services...");
            var netTask = Task.Run(() => RunStep("Scanning Network Interfaces & Wi-Fi...", NetworkInfoCollector.Collect));
            var secTask = Task.Run(() => RunStep("Scanning Windows OS, Security, TPM & Updates...", OsSecurityInfoCollector.Collect));
            var dsregTask = Task.Run(() => RunStep("Querying Entra ID / dsregcmd Diagnostics...", DsregcmdCollector.Collect));
            var swTask = Task.Run(() => RunStep("Enumerating Installed Software Applications...", SoftwareCollector.Collect));
            var procTask = Task.Run(() => RunStep("Querying Active Processes & Windows Services...", ProcessServiceCollector.Collect));
            var fltTask = Task.Run(() => RunStep("Querying Network & Filesystem Filter Drivers (fltmc)...", FilterDriverCollector.Collect));
            var acpiTask = Task.Run(() => RunStep("Reading ACPI Firmware Tables & Parsing DSDT...", AcpiService.Collect));
            var dmTask = Task.Run(() => RunStep("Enumerating Hardware Devices (Device Manager)...", DeviceManagerCollector.Collect));

            await Task.WhenAll(netTask, secTask, dsregTask, swTask, procTask, fltTask, acpiTask, dmTask).ConfigureAwait(false);

            report.Network = netTask.Result;
            report.Security = secTask.Result;
            report.Dsregcmd = dsregTask.Result;
            report.Software = swTask.Result;
            report.ProcessService = procTask.Result;
            report.FilterDrivers = fltTask.Result;
            report.Acpi = acpiTask.Result;
            report.DeviceManager = dmTask.Result;

            progress?.Report("Building System Summary Overview...");
            report.Overview = BuildOverview(report);

            report.GeneratedAt = DateTime.Now;
            CurrentReport = report;

            progress?.Report("Ready.");
            return report;
        }
        finally
        {
            _refreshLock.Release();
        }
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
            HypervisorPresent = report.Cpu.VirtualizationFirmwareEnabled,
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
