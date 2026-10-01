using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class ExportPage : Page
{
    private string _lastExportedFile = string.Empty;

    public ExportPage()
    {
        this.InitializeComponent();
    }

    private async void BtnExportHtml_Click(object sender, RoutedEventArgs e)
    {
        await DoExport("HTML Web Page", ".html", async path =>
        {
            var report = SystemDiagnosticsService.Instance.CurrentReport;
            await ReportExportService.ExportToHtmlAsync(report, path);
        });
    }

    private async void BtnExportJson_Click(object sender, RoutedEventArgs e)
    {
        await DoExport("JSON Document", ".json", async path =>
        {
            var report = SystemDiagnosticsService.Instance.CurrentReport;
            await ReportExportService.ExportToJsonAsync(report, path);
        });
    }

    private async void BtnExportTxt_Click(object sender, RoutedEventArgs e)
    {
        await DoExport("Text Document", ".txt", async path =>
        {
            var report = SystemDiagnosticsService.Instance.CurrentReport;
            await ReportExportService.ExportToTextAsync(report, path);
        });
    }

    private async System.Threading.Tasks.Task DoExport(string formatName, string extension, Func<string, System.Threading.Tasks.Task> exportAction)
    {
        try
        {
            string defaultName = $"XRAY_Report_{Environment.MachineName}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            string? filePath = NativeFileDialog.SaveFile(
                $"Export {formatName}",
                defaultName,
                extension,
                ($"{formatName} (*{extension})", $"*{extension}"),
                ("All Files (*.*)", "*.*")
            );

            if (!string.IsNullOrEmpty(filePath))
            {
                await exportAction(filePath);
                _lastExportedFile = filePath;
                IbExportStatus.Title = "Report Exported Successfully";
                IbExportStatus.Message = $"Saved to: {filePath}";
                IbExportStatus.Severity = InfoBarSeverity.Success;
                IbExportStatus.IsOpen = true;
            }
        }
        catch (Exception ex)
        {
            IbExportStatus.Title = "Export Failed";
            IbExportStatus.Message = ex.Message;
            IbExportStatus.Severity = InfoBarSeverity.Error;
            IbExportStatus.IsOpen = true;
        }
    }

    private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_lastExportedFile) && File.Exists(_lastExportedFile))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _lastExportedFile,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
