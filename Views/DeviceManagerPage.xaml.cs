using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Models;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class DeviceManagerPage : Page
{
    private List<DeviceClassGroup> _allGroups = new();
    private List<DeviceItem> _allDevices = new();

    public DeviceManagerPage()
    {
        this.InitializeComponent();
        this.Loaded += DeviceManagerPage_Loaded;
    }

    private async void DeviceManagerPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (BrdDmLoading != null) BrdDmLoading.Visibility = Visibility.Visible;
        if (PanelDmContent != null) PanelDmContent.Visibility = Visibility.Collapsed;

        // Yield to allow instant page display
        await System.Threading.Tasks.Task.Yield();

        var dm = SystemDiagnosticsService.Instance.CurrentReport.DeviceManager;
        _allGroups = dm?.Groups ?? new();
        _allDevices = dm?.AllDevices ?? new();

        int problems = dm?.ProblemDevicesCount ?? 0;
        if (problems > 0)
        {
            TxtIssuesText.Text = $"{problems} Device Problem(s)";
            BrdIssuesBadge.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68));
            TxtIssuesText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        }
        else
        {
            TxtIssuesText.Text = "All Devices Healthy";
        }

        ApplyFilter();

        if (BrdDmLoading != null) BrdDmLoading.Visibility = Visibility.Collapsed;
        if (PanelDmContent != null) PanelDmContent.Visibility = Visibility.Visible;
    }

    private void ViewMode_Changed(object sender, RoutedEventArgs e)
    {
        if (LstGroups == null || BrdFlatDevices == null) return;

        bool isGrouped = RbGrouped?.IsChecked == true;
        LstGroups.Visibility = isGrouped ? Visibility.Visible : Visibility.Collapsed;
        BrdFlatDevices.Visibility = isGrouped ? Visibility.Collapsed : Visibility.Visible;
        ApplyFilter();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (LstGroups == null || BrdFlatDevices == null || LstFlatDevices == null || TxtTotalCount == null) return;

        string query = TxtSearch?.Text?.Trim() ?? "";
        bool isGrouped = RbGrouped?.IsChecked == true;
        bool isIssuesOnly = RbIssues?.IsChecked == true;

        var baseDevices = _allDevices.AsEnumerable();

        if (isIssuesOnly)
        {
            baseDevices = baseDevices.Where(d => d.HasProblem);
        }

        if (!string.IsNullOrEmpty(query))
        {
            baseDevices = baseDevices.Where(d =>
                d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.Manufacturer.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.DeviceId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.FriendlyClassName.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        var filteredList = baseDevices.ToList();
        TxtTotalCount.Text = $"Devices ({filteredList.Count})";

        if (isGrouped)
        {
            var filteredGroups = filteredList
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

            LstGroups.ItemsSource = filteredGroups;
        }
        else
        {
            LstFlatDevices.ItemsSource = filteredList;
        }
    }

    private void BtnOpenDevMgmt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "devmgmt.msc",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
