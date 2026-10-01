using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class ProcessesPage : Page
{
    private List<ProcessItem> _allProcesses = new();
    private List<WindowsServiceItem> _allServices = new();

    public ProcessesPage()
    {
        this.InitializeComponent();
        this.Loaded += ProcessesPage_Loaded;
    }

    private async void ProcessesPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (BrdProcessesLoading != null) BrdProcessesLoading.Visibility = Visibility.Visible;
        if (PanelProcessesContent != null) PanelProcessesContent.Visibility = Visibility.Collapsed;

        await Task.Yield();

        _allProcesses = SystemDiagnosticsService.Instance.CurrentReport.ProcessService.Processes;
        _allServices = SystemDiagnosticsService.Instance.CurrentReport.ProcessService.Services;
        ApplyFilter();

        if (BrdProcessesLoading != null) BrdProcessesLoading.Visibility = Visibility.Collapsed;
        if (PanelProcessesContent != null) PanelProcessesContent.Visibility = Visibility.Visible;
    }

    private void BtnOpenTaskMgr_Click(object sender, RoutedEventArgs e)
    {
        ServiceManagerHelper.OpenTaskManager();
    }

    private void BtnOpenServicesMsc_Click(object sender, RoutedEventArgs e)
    {
        ServiceManagerHelper.OpenServicesMsc();
    }

    private void FilterMode_Changed(object sender, RoutedEventArgs e)
    {
        if (LstProcesses == null || LstServices == null) return;

        bool isProcesses = RbProcesses?.IsChecked == true;
        LstProcesses.Visibility = isProcesses ? Visibility.Visible : Visibility.Collapsed;
        LstServices.Visibility = isProcesses ? Visibility.Collapsed : Visibility.Visible;
        if (CmbProcessSort != null)
            CmbProcessSort.Visibility = isProcesses ? Visibility.Visible : Visibility.Collapsed;
        if (CmbServiceSort != null)
            CmbServiceSort.Visibility = isProcesses ? Visibility.Collapsed : Visibility.Visible;
        ApplyFilter();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void CmbProcessSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void CmbServiceSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (LstProcesses == null || LstServices == null || _allProcesses == null || _allServices == null) return;

        string query = TxtSearch?.Text?.Trim() ?? "";
        bool isProcesses = RbProcesses?.IsChecked == true;

        if (isProcesses)
        {
            var filtered = _allProcesses.AsEnumerable();
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(p =>
                    p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    p.Id.ToString().Contains(query) ||
                    p.FilePath.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            int sortIdx = CmbProcessSort?.SelectedIndex ?? 0;
            filtered = sortIdx switch
            {
                0 => filtered.OrderByDescending(p => p.MemoryWorkingSetMB),
                1 => filtered.OrderBy(p => p.MemoryWorkingSetMB),
                2 => filtered.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase),
                3 => filtered.OrderByDescending(p => p.Name, StringComparer.OrdinalIgnoreCase),
                4 => filtered.OrderBy(p => p.Id),
                5 => filtered.OrderByDescending(p => p.Id),
                _ => filtered.OrderByDescending(p => p.MemoryWorkingSetMB)
            };

            var list = filtered.ToList();
            if (TxtItemsCount != null) TxtItemsCount.Text = $"Active Processes ({list.Count})";
            LstProcesses.ItemsSource = list;
        }
        else
        {
            var filtered = _allServices.AsEnumerable();
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(s =>
                    s.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.ServiceName.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            int sortIdx = CmbServiceSort?.SelectedIndex ?? 0;
            filtered = sortIdx switch
            {
                0 => filtered.OrderBy(s => s.IsRunning ? 0 : 1).ThenBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase),
                1 => filtered.OrderBy(s => s.IsRunning ? 1 : 0).ThenBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase),
                2 => filtered.OrderBy(s => s.StartType.Equals("Automatic", StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase),
                3 => filtered.OrderBy(s => s.StartType.Equals("Manual", StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase),
                4 => filtered.OrderBy(s => s.StartType.Equals("Disabled", StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase),
                5 => filtered.OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase),
                _ => filtered.OrderBy(s => s.IsRunning ? 0 : 1).ThenBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
            };

            var list = filtered.ToList();
            if (TxtItemsCount != null) TxtItemsCount.Text = $"Windows Services ({list.Count})";
            LstServices.ItemsSource = list;
        }
    }

    private async void BtnTerminateProcess_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not ProcessItem p) return;

        var dialog = new ContentDialog
        {
            Title = "Terminate Process",
            Content = $"Are you sure you want to terminate '{p.Name}' (PID: {p.Id})?\n\nTerminating an active process may cause unsaved data to be lost.",
            PrimaryButtonText = "Terminate",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                var proc = Process.GetProcessById(p.Id);
                proc.Kill(entireProcessTree: true);

                IbStatus.Title = "Process Terminated";
                IbStatus.Message = $"Process '{p.Name}' (PID: {p.Id}) was terminated successfully.";
                IbStatus.Severity = InfoBarSeverity.Success;
                IbStatus.IsOpen = true;

                _allProcesses.RemoveAll(x => x.Id == p.Id);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                IbStatus.Title = "Termination Failed";
                IbStatus.Message = $"Could not terminate '{p.Name}' (PID: {p.Id}): {ex.Message}";
                IbStatus.Severity = InfoBarSeverity.Error;
                IbStatus.IsOpen = true;
            }
        }
    }

    private async void BtnStartService_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not WindowsServiceItem svc) return;

        IbStatus.Title = "Starting Service...";
        IbStatus.Message = $"Requesting service start for '{svc.DisplayName}' ({svc.ServiceName})...";
        IbStatus.Severity = InfoBarSeverity.Informational;
        IbStatus.IsOpen = true;

        var (success, msg) = await ServiceManagerHelper.StartServiceAsync(svc.ServiceName);
        IbStatus.Title = success ? "Service Started" : "Start Failed";
        IbStatus.Message = msg;
        IbStatus.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;

        if (success)
        {
            svc.Status = "Running";
            ApplyFilter();
        }
    }

    private async void BtnStopService_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not WindowsServiceItem svc) return;

        IbStatus.Title = "Stopping Service...";
        IbStatus.Message = $"Requesting service stop for '{svc.DisplayName}' ({svc.ServiceName})...";
        IbStatus.Severity = InfoBarSeverity.Informational;
        IbStatus.IsOpen = true;

        var (success, msg) = await ServiceManagerHelper.StopServiceAsync(svc.ServiceName);
        IbStatus.Title = success ? "Service Stopped" : "Stop Failed";
        IbStatus.Message = msg;
        IbStatus.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;

        if (success)
        {
            svc.Status = "Stopped";
            ApplyFilter();
        }
    }

    private async void BtnRestartService_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not WindowsServiceItem svc) return;

        IbStatus.Title = "Restarting Service...";
        IbStatus.Message = $"Restarting '{svc.DisplayName}' ({svc.ServiceName})...";
        IbStatus.Severity = InfoBarSeverity.Informational;
        IbStatus.IsOpen = true;

        var (success, msg) = await ServiceManagerHelper.RestartServiceAsync(svc.ServiceName);
        IbStatus.Title = success ? "Service Restarted" : "Restart Failed";
        IbStatus.Message = msg;
        IbStatus.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;

        if (success)
        {
            svc.Status = "Running";
            ApplyFilter();
        }
    }

    private async void MenuStartupType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item || item.Tag is not WindowsServiceItem svc) return;
        string newType = item.Text;

        IbStatus.Title = "Configuring Service...";
        IbStatus.Message = $"Setting startup type of '{svc.DisplayName}' to {newType}...";
        IbStatus.Severity = InfoBarSeverity.Informational;
        IbStatus.IsOpen = true;

        var (success, msg) = await ServiceManagerHelper.ChangeStartupTypeAsync(svc.ServiceName, newType);
        IbStatus.Title = success ? "Configuration Updated" : "Configuration Failed";
        IbStatus.Message = msg;
        IbStatus.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;

        if (success)
        {
            svc.StartType = newType;
            ApplyFilter();
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        BtnRefresh.IsEnabled = false;
        try
        {
            var res = await Task.Run(() => ProcessServiceCollector.Collect());
            SystemDiagnosticsService.Instance.CurrentReport.ProcessService = res;
            _allProcesses = res.Processes;
            _allServices = res.Services;
            ApplyFilter();
        }
        finally
        {
            BtnRefresh.IsEnabled = true;
        }
    }
}
