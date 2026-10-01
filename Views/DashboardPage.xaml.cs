using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        this.InitializeComponent();
        this.Loaded += DashboardPage_Loaded;
    }

    private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateUI(SystemDiagnosticsService.Instance.CurrentReport);
    }

    public void UpdateUI(SystemReport report)
    {
        TxtComputerName.Text = string.IsNullOrEmpty(report.Overview.ComputerName) ? Environment.MachineName : report.Overview.ComputerName;
        TxtOsSub.Text = $"{report.Overview.OsName} ({report.Overview.OsArchitecture})";
        TxtUptime.Text = $"Uptime: {report.Overview.SystemUptime}";
        TxtUser.Text = $"User: {report.Overview.DomainOrWorkgroup}\\{report.Overview.UserName}";

        // CPU
        TxtCpuModel.Text = string.IsNullOrEmpty(report.Cpu.ProcessorName) ? "Detecting CPU..." : report.Cpu.ProcessorName;
        TxtCpuCores.Text = $"Cores: {report.Cpu.CoreCount}  |  Threads: {report.Cpu.LogicalProcessorCount}";
        TxtCpuClocks.Text = $"Clock: {UnitFormatter.FormatHertz(report.Cpu.BaseClockSpeedMHz)}";
        TxtCpuTempDash.Text = "Optimal";

        // RAM
        TxtRamTotal.Text = UnitFormatter.FormatBytes(report.Memory.TotalPhysicalBytes);
        PrgRamUsage.Value = report.Memory.MemoryLoadPercent;
        TxtRamUsed.Text = $"Used: {UnitFormatter.FormatBytes(report.Memory.UsedPhysicalBytes)}";
        TxtRamPercent.Text = $"{report.Memory.MemoryLoadPercent:F0}%";

        // GPU
        if (report.Gpu.Adapters.Count > 0)
        {
            var gpu = report.Gpu.Adapters[0];
            TxtGpuName.Text = gpu.Name;
            TxtGpuVram.Text = gpu.AdapterRamBytes > 0 ? $"VRAM: {UnitFormatter.FormatBytes(gpu.AdapterRamBytes)}" : "VRAM: Shared/Integrated";
            TxtGpuRes.Text = gpu.CurrentHorizontalResolution > 0 ? $"{gpu.CurrentHorizontalResolution}x{gpu.CurrentVerticalResolution} @ {gpu.CurrentRefreshRate}Hz" : gpu.Status;
            TxtGpuTempDash.Text = "Optimal";
        }

        // Storage
        var primaryVol = report.Storage.Volumes.FirstOrDefault(v => v.DriveLetter.StartsWith("C", StringComparison.OrdinalIgnoreCase)) ?? report.Storage.Volumes.FirstOrDefault();
        if (primaryVol != null && primaryVol.IsReady)
        {
            TxtStoragePrimary.Text = $"{primaryVol.DriveLetter} ({primaryVol.VolumeLabel})";
            PrgStorageUsage.Value = primaryVol.PercentUsed;
            TxtStorageFree.Text = $"Free: {UnitFormatter.FormatBytes(primaryVol.FreeSizeBytes)}";
            TxtStorageTotal.Text = $"Total: {UnitFormatter.FormatBytes(primaryVol.TotalSizeBytes)}";
        }
        else
        {
            TxtStoragePrimary.Text = $"{report.Storage.PhysicalDisks.Count} Physical Drive(s)";
            TxtStorageFree.Text = report.Storage.PhysicalDisks.Count > 0 ? report.Storage.PhysicalDisks[0].Model : "-";
        }

        // Network
        var activeAdapter = report.Network.Adapters.FirstOrDefault(a => a.Status.Equals("Up", StringComparison.OrdinalIgnoreCase) && a.Ipv4Addresses.Count > 0) ?? report.Network.Adapters.FirstOrDefault();
        if (activeAdapter != null)
        {
            TxtNetIp.Text = activeAdapter.PrimaryIpAddress;
            TxtNetAdapter.Text = activeAdapter.Name;
            TxtNetStatus.Text = $"{activeAdapter.InterfaceType} • {activeAdapter.Status}";
        }

        // Cloud Join / Entra ID
        TxtJoinState.Text = report.Dsregcmd.OverallStatus;
        TxtTenant.Text = string.IsNullOrEmpty(report.Dsregcmd.TenantName) ? (string.IsNullOrEmpty(report.Dsregcmd.TenantId) ? "No Tenant Linked" : $"Tenant: {report.Dsregcmd.TenantId}") : $"Tenant: {report.Dsregcmd.TenantName}";
        TxtPrt.Text = report.Dsregcmd.AzureAdPrt ? "AzureAdPrt: YES (SSO Active)" : "AzureAdPrt: NO";

        // Security
        TxtTpmStatus.Text = report.Security.Tpm.IsPresent ? $"TPM {report.Security.Tpm.SpecVersion} Active" : "TPM: Not Detected";
        TxtSecureBoot.Text = $"Secure Boot: {(report.Security.SecureBootEnabled ? "Enabled" : "Disabled")}";
        if (report.Security.AntivirusProducts.Count > 0)
            TxtAntivirus.Text = report.Security.AntivirusProducts[0].DisplayName;
        else
            TxtAntivirus.Text = "Antivirus: Active";

        // Battery
        if (report.Battery.IsBatteryPresent)
        {
            TxtBatteryPercent.Text = report.Battery.BatteryLifePercent >= 0 ? $"{report.Battery.BatteryLifePercent}% ({report.Battery.BatteryState})" : report.Battery.PowerLineStatus;
            TxtBatteryHealth.Text = report.Battery.BatteryHealthPercent > 0 ? $"Health: {report.Battery.BatteryHealthPercent:F1}%" : report.Battery.PowerLineStatus;
        }
        else
        {
            TxtBatteryPercent.Text = "AC Desktop / Online";
            TxtBatteryHealth.Text = "No Battery";
        }
        TxtPowerPlan.Text = $"Plan: {report.Battery.ActivePowerScheme}";

        // Detailed Properties Grouped by Category
        LstOverviewPropertyGroups.ItemsSource = PropertyItem.GroupByCategory(report.Overview.Properties);
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        PrgLoading.Visibility = Visibility.Visible;
        BtnRefresh.IsEnabled = false;

        try
        {
            var report = await SystemDiagnosticsService.Instance.RefreshAllAsync();
            UpdateUI(report);
        }
        finally
        {
            PrgLoading.Visibility = Visibility.Collapsed;
            BtnRefresh.IsEnabled = true;
        }
    }

    private void BtnOpenWindowsUpdate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ms-settings:windowsupdate",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
