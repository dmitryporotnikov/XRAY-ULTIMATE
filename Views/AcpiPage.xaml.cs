using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class AcpiPage : Page
{
    private AcpiReport? _acpi;
    private bool _aslLoaded = false;

    public AcpiPage()
    {
        this.InitializeComponent();
        this.Loaded += AcpiPage_Loaded;
    }

    private async void AcpiPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (BrdAcpiLoading != null) BrdAcpiLoading.Visibility = Visibility.Visible;
        if (PanelAcpiContent != null) PanelAcpiContent.Visibility = Visibility.Collapsed;

        // Yield to allow instant page display without any navigation lag
        await System.Threading.Tasks.Task.Yield();

        _acpi = SystemDiagnosticsService.Instance.CurrentReport.Acpi;
        if (_acpi == null)
        {
            if (BrdAcpiLoading != null) BrdAcpiLoading.Visibility = Visibility.Collapsed;
            return;
        }

        // Header Meta
        string dsdtSize = _acpi.Dsdt?.Header != null ? _acpi.Dsdt.Header.FormattedLength : "Not Found";
        string oem = _acpi.Dsdt?.Header != null ? _acpi.Dsdt.Header.OemId : "-";
        TxtAcpiMeta.Text = $"DSDT Size: {dsdtSize}  |  OEM ID: {oem}  |  Tables: {_acpi.Tables.Count}";

        // DSDT Header Card
        if (_acpi.Dsdt?.Header != null)
        {
            var h = _acpi.Dsdt.Header;
            TxtDsdtSize.Text = h.FormattedLength;
            TxtDsdtOem.Text = h.OemId;
            TxtDsdtOemTable.Text = h.OemTableId;
            TxtDsdtCreator.Text = h.CreatorId;
            TxtDsdtRev.Text = $"ACPI Rev: {h.Revision} (OemRev: 0x{h.OemRevision:X})";
        }

        // DSDT Discovered Objects & Hex Dump Preview
        if (_acpi.Dsdt != null)
        {
            LstAcpiObjects.ItemsSource = _acpi.Dsdt.DiscoveredObjects;
            TxtDsdtHex.Text = _acpi.Dsdt.HexDumpPreview;
        }

        // All Tables
        LstAllAcpiTables.ItemsSource = _acpi.Tables;

        // Transition from loading to content
        if (BrdAcpiLoading != null) BrdAcpiLoading.Visibility = Visibility.Collapsed;
        if (PanelAcpiContent != null) PanelAcpiContent.Visibility = Visibility.Visible;

        // Background ASL population
        _ = LoadAslTextAsync();
    }

    private async System.Threading.Tasks.Task LoadAslTextAsync()
    {
        if (_aslLoaded || _acpi?.Dsdt == null) return;

        string asl = _acpi.Dsdt.AslSourceCode;
        if (string.IsNullOrEmpty(asl)) return;

        int lines = await System.Threading.Tasks.Task.Run(() => asl.Split('\n').Length);
        if (TxtAslStats != null)
            TxtAslStats.Text = $"Lines: {lines:N0} ({asl.Length / 1024.0:F1} KB)";

        TxtAslSource.Text = asl;
        _aslLoaded = true;
    }

    private async void ViewMode_Changed(object sender, RoutedEventArgs e)
    {
        if (PanelDsdt == null || PanelAsl == null || PanelAllTables == null) return;

        bool isDsdt = RbDsdt?.IsChecked == true;
        bool isAsl = RbAsl?.IsChecked == true;

        PanelDsdt.Visibility = isDsdt ? Visibility.Visible : Visibility.Collapsed;
        PanelAsl.Visibility = isAsl ? Visibility.Visible : Visibility.Collapsed;
        PanelAllTables.Visibility = (!isDsdt && !isAsl) ? Visibility.Visible : Visibility.Collapsed;

        if (isAsl && !_aslLoaded)
        {
            await LoadAslTextAsync();
        }
    }

    private async void BtnExportDsl_Click(object sender, RoutedEventArgs e)
    {
        string asl = _acpi?.Dsdt?.AslSourceCode ?? TxtAslSource.Text;
        if (string.IsNullOrWhiteSpace(asl)) return;

        try
        {
            string defaultName = $"DSDT_{Environment.MachineName}_{DateTime.Now:yyyyMMdd}.dsl";
            string? filePath = NativeFileDialog.SaveFile(
                "Export Disassembled ACPI Source",
                defaultName,
                ".dsl",
                ("ACPI Disassembled Source (*.dsl)", "*.dsl"),
                ("ACPI Source Language (*.asl)", "*.asl"),
                ("All Files (*.*)", "*.*")
            );

            if (!string.IsNullOrEmpty(filePath))
            {
                await File.WriteAllTextAsync(filePath, asl, System.Text.Encoding.UTF8);
            }
        }
        catch { }
    }

    private void BtnCopyAsl_Click(object sender, RoutedEventArgs e)
    {
        var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dp.SetText(TxtAslSource.Text);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
    }

    private async void BtnExportDsdt_Click(object sender, RoutedEventArgs e)
    {
        if (_acpi?.Dsdt?.RawAmlData == null || _acpi.Dsdt.RawAmlData.Length == 0) return;

        try
        {
            string defaultName = $"DSDT_{Environment.MachineName}_{DateTime.Now:yyyyMMdd}.aml";
            string? filePath = NativeFileDialog.SaveFile(
                "Export Raw ACPI Machine Table",
                defaultName,
                ".aml",
                ("ACPI Machine Language (*.aml)", "*.aml"),
                ("Raw ACPI Binary (*.dat)", "*.dat"),
                ("All Files (*.*)", "*.*")
            );

            if (!string.IsNullOrEmpty(filePath))
            {
                await File.WriteAllBytesAsync(filePath, _acpi.Dsdt.RawAmlData);
            }
        }
        catch { }
    }
}
