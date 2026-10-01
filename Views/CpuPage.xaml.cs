using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class CpuPage : Page
{
    public CpuPage()
    {
        this.InitializeComponent();
        this.Loaded += CpuPage_Loaded;
    }

    private void CpuPage_Loaded(object sender, RoutedEventArgs e)
    {
        var cpu = SystemDiagnosticsService.Instance.CurrentReport.Cpu;
        TxtCpuTitle.Text = string.IsNullOrEmpty(cpu.ProcessorName) ? "Detecting CPU..." : cpu.ProcessorName;
        TxtCpuArch.Text = $"{cpu.Manufacturer} • Architecture: {cpu.Architecture}";
        TxtCoresThreads.Text = $"Physical Cores: {cpu.CoreCount}  |  Logical Threads: {cpu.LogicalProcessorCount}";
        TxtSocket.Text = string.IsNullOrEmpty(cpu.SocketDesignation) ? "" : $"Socket: {cpu.SocketDesignation}";
        TxtClocks.Text = $"Base: {UnitFormatter.FormatHertz(cpu.BaseClockSpeedMHz)} | Max: {UnitFormatter.FormatHertz(cpu.MaxClockSpeedMHz)}";

        TxtL1Cache.Text = cpu.L1DataCacheKB > 0 ? $"{cpu.L1DataCacheKB} KB" : "Available";
        TxtL2Cache.Text = cpu.L2CacheKB > 0 ? (cpu.L2CacheKB >= 1024 ? $"{cpu.L2CacheKB / 1024.0:F1} MB" : $"{cpu.L2CacheKB} KB") : "Available";
        TxtL3Cache.Text = cpu.L3CacheKB > 0 ? $"{cpu.L3CacheKB / 1024.0:F1} MB" : "Available";

        GvFeatures.ItemsSource = cpu.SupportedFeatures;
        LstCpuPropertyGroups.ItemsSource = PropertyItem.GroupByCategory(cpu.Properties);
    }
}
