using System;

namespace XRAY_ULTIMATE.Models;

public class SystemReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string GeneratorVersion { get; set; } = "1.0.0 (XRAY ULTIMATE System Diagnostics)";

    public SystemOverviewInfo Overview { get; set; } = new();
    public CpuInfo Cpu { get; set; } = new();
    public MemoryInfo Memory { get; set; } = new();
    public MotherboardInfo Motherboard { get; set; } = new();
    public GpuInfo Gpu { get; set; } = new();
    public StorageInfo Storage { get; set; } = new();
    public NetworkInfo Network { get; set; } = new();
    public BatteryInfo Battery { get; set; } = new();
    public OsSecurityInfo Security { get; set; } = new();
    public DsregcmdInfo Dsregcmd { get; set; } = new();
    public SoftwareInfo Software { get; set; } = new();
    public ProcessServiceInfo ProcessService { get; set; } = new();
    public FilterDriversReport FilterDrivers { get; set; } = new();
    public AcpiReport Acpi { get; set; } = new();
    public DeviceManagerReport DeviceManager { get; set; } = new();
    public BenchmarkResult? Benchmark { get; set; }
}
