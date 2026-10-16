using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class FiltersPage : Page
{
    private List<FilesystemFilter> _allFsFilters = new();
    private List<NetworkFilterDriver> _allNetFilters = new();

    public FiltersPage()
    {
        this.InitializeComponent();
        this.Loaded += FiltersPage_Loaded;
    }

    private void FiltersPage_Loaded(object sender, RoutedEventArgs e)
    {
        var report = SystemDiagnosticsService.Instance.CurrentReport.FilterDrivers;
        _allFsFilters = report?.FilesystemFilters ?? new();
        _allNetFilters = report?.NetworkFilters ?? new();
        ApplyFilter();
    }

    private void FilterMode_Changed(object sender, RoutedEventArgs e)
    {
        if (LstFsFilters == null || LstNetFilters == null) return;

        bool isFs = RbFsFilters?.IsChecked == true;
        LstFsFilters.Visibility = isFs ? Visibility.Visible : Visibility.Collapsed;
        LstNetFilters.Visibility = isFs ? Visibility.Collapsed : Visibility.Visible;
        ApplyFilter();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (LstFsFilters == null || LstNetFilters == null || TxtCountBadge == null) return;

        string query = TxtSearch?.Text?.Trim() ?? "";
        bool isFs = RbFsFilters?.IsChecked == true;

        if (isFs)
        {
            var filtered = _allFsFilters.AsEnumerable();
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(f =>
                    f.FilterName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    f.Altitude.Contains(query) ||
                    f.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    f.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
            }
            var list = filtered.ToList();
            TxtCountBadge.Text = $"Minifilters: {list.Count}";
            LstFsFilters.ItemsSource = list;
        }
        else
        {
            var filtered = _allNetFilters.AsEnumerable();
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(n =>
                    n.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    n.ServiceName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    n.FilterClass.Contains(query, StringComparison.OrdinalIgnoreCase));
            }
            var list = filtered.ToList();
            TxtCountBadge.Text = $"Network Filters: {list.Count}";
            LstNetFilters.ItemsSource = list;
        }
    }

    private async void BtnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool isFs = RbFsFilters?.IsChecked == true;
            string defaultName = isFs
                ? $"FilesystemFilters_fltmc_{Environment.MachineName}_{DateTime.Now:yyyyMMdd}.csv"
                : $"NetworkFilterDrivers_{Environment.MachineName}_{DateTime.Now:yyyyMMdd}.csv";

            string? filePath = NativeFileDialog.SaveFile(
                "Export Filter Drivers to CSV",
                defaultName,
                ".csv",
                ("CSV Comma-Separated Values (*.csv)", "*.csv"),
                ("All Files (*.*)", "*.*")
            );

            if (!string.IsNullOrEmpty(filePath))
            {
                var sb = new StringBuilder();
                if (isFs)
                {
                    sb.AppendLine("FilterName,Altitude,Category,Instances,Frame,Description");
                    foreach (var fs in _allFsFilters)
                    {
                        sb.AppendLine($"{EscapeCsv(fs.FilterName)},{EscapeCsv(fs.Altitude)},{EscapeCsv(fs.Category)},{fs.NumInstances},{EscapeCsv(fs.Frame)},{EscapeCsv(fs.Description)}");
                    }
                }
                else
                {
                    sb.AppendLine("DisplayName,ServiceName,FilterClass,FilterType,State");
                    foreach (var net in _allNetFilters)
                    {
                        sb.AppendLine($"{EscapeCsv(net.DisplayName)},{EscapeCsv(net.ServiceName)},{EscapeCsv(net.FilterClass)},{EscapeCsv(net.FilterType)},{EscapeCsv(net.State)}");
                    }
                }

                await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
            }
        }
        catch { }
    }

    private static string EscapeCsv(string str)
    {
        if (string.IsNullOrEmpty(str)) return "\"\"";
        return $"\"{str.Replace("\"", "\"\"")}\"";
    }
}
