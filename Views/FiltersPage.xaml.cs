using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
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
                ? $"FilesystemFilters_fltmc_{Environment.MachineName}_{DateTime.Now:yyyyMMdd}"
                : $"NetworkFilterDrivers_{Environment.MachineName}_{DateTime.Now:yyyyMMdd}";

            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = defaultName
            };
            savePicker.FileTypeChoices.Add("CSV Comma-Separated Values", new List<string> { ".csv" });
            WindowHelper.InitializePicker(savePicker);

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                var sb = new StringBuilder();
                if (isFs)
                {
                    sb.AppendLine("FilterName,Altitude,Category,Instances,Frame,Description");
                    foreach (var fs in _allFsFilters)
                    {
                        sb.AppendLine($"\"{fs.FilterName}\",\"{fs.Altitude}\",\"{fs.Category}\",{fs.NumInstances},\"{fs.Frame}\",\"{fs.Description}\"");
                    }
                }
                else
                {
                    sb.AppendLine("DisplayName,ServiceName,FilterClass,FilterType,State");
                    foreach (var net in _allNetFilters)
                    {
                        sb.AppendLine($"\"{net.DisplayName}\",\"{net.ServiceName}\",\"{net.FilterClass}\",\"{net.FilterType}\",\"{net.State}\"");
                    }
                }

                await File.WriteAllTextAsync(file.Path, sb.ToString(), Encoding.UTF8);
            }
        }
        catch { }
    }
}
