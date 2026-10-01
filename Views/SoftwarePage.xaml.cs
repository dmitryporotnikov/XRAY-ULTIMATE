using System;
using System.Collections.Generic;
using System.Diagnostics;
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

public sealed partial class SoftwarePage : Page
{
    private List<InstalledApplication> _allApps = new();

    public SoftwarePage()
    {
        this.InitializeComponent();
        this.Loaded += SoftwarePage_Loaded;
    }

    private async void SoftwarePage_Loaded(object sender, RoutedEventArgs e)
    {
        if (BrdSoftwareLoading != null) BrdSoftwareLoading.Visibility = Visibility.Visible;
        if (PanelSoftwareContent != null) PanelSoftwareContent.Visibility = Visibility.Collapsed;

        await System.Threading.Tasks.Task.Yield();

        _allApps = SystemDiagnosticsService.Instance.CurrentReport.Software?.Applications ?? new();
        ApplyFilter();

        if (BrdSoftwareLoading != null) BrdSoftwareLoading.Visibility = Visibility.Collapsed;
        if (PanelSoftwareContent != null) PanelSoftwareContent.Visibility = Visibility.Visible;
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void CmbArchFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (LstSoftware == null || TxtAppCount == null || _allApps == null) return;

        string query = TxtSearch?.Text?.Trim() ?? "";
        string selectedArch = (CmbArchFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Architectures";

        var filtered = _allApps.AsEnumerable();

        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(a =>
                (!string.IsNullOrEmpty(a.DisplayName) && a.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(a.Publisher) && a.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        if (selectedArch.Contains("64-bit"))
        {
            filtered = filtered.Where(a => a.Architecture.Equals("64-bit", StringComparison.OrdinalIgnoreCase));
        }
        else if (selectedArch.Contains("32-bit"))
        {
            filtered = filtered.Where(a => a.Architecture.Equals("32-bit", StringComparison.OrdinalIgnoreCase));
        }
        else if (selectedArch.Contains("User"))
        {
            filtered = filtered.Where(a => a.Architecture.Equals("User", StringComparison.OrdinalIgnoreCase));
        }

        var list = filtered.ToList();
        TxtAppCount.Text = $"Installed Software ({list.Count})";
        LstSoftware.ItemsSource = list;
    }

    private async void BtnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"InstalledSoftware_{Environment.MachineName}_{DateTime.Now:yyyyMMdd}"
            };
            savePicker.FileTypeChoices.Add("CSV Comma-Separated Values", new List<string> { ".csv" });
            WindowHelper.InitializePicker(savePicker);

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("DisplayName,DisplayVersion,Publisher,InstallDate,Architecture,InstallLocation");
                foreach (var app in _allApps)
                {
                    string name = EscapeCsv(app.DisplayName);
                    string ver = EscapeCsv(app.DisplayVersion);
                    string pub = EscapeCsv(app.Publisher);
                    string date = EscapeCsv(app.InstallDate);
                    string arch = EscapeCsv(app.Architecture);
                    string loc = EscapeCsv(app.InstallLocation);
                    sb.AppendLine($"{name},{ver},{pub},{date},{arch},{loc}");
                }

                await File.WriteAllTextAsync(file.Path, sb.ToString(), Encoding.UTF8);
            }
        }
        catch { }
    }

    private async void BtnUninstall_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not InstalledApplication app) return;

        if (string.IsNullOrWhiteSpace(app.UninstallString))
        {
            IbUninstallStatus.Title = "Uninstall Unavailable";
            IbUninstallStatus.Message = $"No uninstall command was found in the registry for '{app.DisplayName}'.";
            IbUninstallStatus.Severity = InfoBarSeverity.Warning;
            IbUninstallStatus.IsOpen = true;
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Confirm Uninstallation",
            Content = $"Are you sure you want to run the uninstaller for '{app.DisplayName}'?\n\nCommand:\n{app.UninstallString}",
            PrimaryButtonText = "Launch Uninstaller",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                LaunchUninstaller(app.UninstallString);
                IbUninstallStatus.Title = "Uninstaller Launched";
                IbUninstallStatus.Message = $"Started uninstaller for '{app.DisplayName}'. Complete the wizard in the prompted window.";
                IbUninstallStatus.Severity = InfoBarSeverity.Success;
                IbUninstallStatus.IsOpen = true;
            }
            catch (Exception ex)
            {
                IbUninstallStatus.Title = "Launch Failed";
                IbUninstallStatus.Message = $"Could not launch uninstaller: {ex.Message}";
                IbUninstallStatus.Severity = InfoBarSeverity.Error;
                IbUninstallStatus.IsOpen = true;
            }
        }
    }

    private static void LaunchUninstaller(string rawCommand)
    {
        string cmd = rawCommand.Trim();
        string fileName = "";
        string arguments = "";

        if (cmd.StartsWith("\""))
        {
            int secondQuote = cmd.IndexOf('\"', 1);
            if (secondQuote > 0)
            {
                fileName = cmd.Substring(1, secondQuote - 1);
                arguments = cmd.Substring(secondQuote + 1).Trim();
            }
            else
            {
                fileName = cmd.Trim('\"');
            }
        }
        else if (cmd.StartsWith("msiexec", StringComparison.OrdinalIgnoreCase))
        {
            fileName = "msiexec.exe";
            int spaceIdx = cmd.IndexOf(' ');
            arguments = spaceIdx > 0 ? cmd.Substring(spaceIdx + 1).Trim() : "";
            if (arguments.StartsWith("/I", StringComparison.OrdinalIgnoreCase))
            {
                arguments = "/X" + arguments.Substring(2);
            }
        }
        else
        {
            int exeIdx = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIdx > 0)
            {
                fileName = cmd.Substring(0, exeIdx + 4).Trim();
                arguments = cmd.Substring(exeIdx + 4).Trim();
            }
            else
            {
                int spaceIdx = cmd.IndexOf(' ');
                if (spaceIdx > 0)
                {
                    fileName = cmd.Substring(0, spaceIdx).Trim();
                    arguments = cmd.Substring(spaceIdx + 1).Trim();
                }
                else
                {
                    fileName = cmd;
                }
            }
        }

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
    }

    private void BtnOpenWindowsInstalledApps_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:appsfeatures",
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "appwiz.cpl",
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }

    private static string EscapeCsv(string str)
    {
        if (string.IsNullOrEmpty(str)) return "\"\"";
        return $"\"{str.Replace("\"", "\"\"")}\"";
    }
}
