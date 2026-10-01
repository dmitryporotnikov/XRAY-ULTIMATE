using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using XRAY_ULTIMATE.Models;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class DsregcmdPage : Page
{
    public DsregcmdPage()
    {
        this.InitializeComponent();
        this.Loaded += DsregcmdPage_Loaded;
    }

    private void DsregcmdPage_Loaded(object sender, RoutedEventArgs e)
    {
        PopulateUI(SystemDiagnosticsService.Instance.CurrentReport.Dsregcmd);
    }

    private void PopulateUI(DsregcmdInfo ds)
    {
        TxtOverallStatus.Text = ds.OverallStatus;
        TxtAadJoined.Text = ds.AzureAdJoined ? "YES" : "NO";
        TxtEnterpriseJoined.Text = ds.EnterpriseJoined ? "YES" : "NO";
        TxtDomainJoined.Text = ds.DomainJoined ? "YES" : "NO";
        TxtPrtStatus.Text = ds.AzureAdPrt ? "YES" : "NO";

        TxtTenantName.Text = string.IsNullOrEmpty(ds.TenantName) ? "Tenant Name: None" : $"Tenant Name: {ds.TenantName}";
        TxtTenantId.Text = string.IsNullOrEmpty(ds.TenantId) ? "Tenant ID: None" : $"Tenant ID: {ds.TenantId}";
        TxtIdp.Text = string.IsNullOrEmpty(ds.Idp) ? "IDP: None" : $"IDP: {ds.Idp}";

        TxtDeviceId.Text = string.IsNullOrEmpty(ds.DeviceId) ? "Device ID: None" : $"Device ID: {ds.DeviceId}";
        TxtTpmProtected.Text = string.IsNullOrEmpty(ds.TpmProtected) ? "TPM Protected: Unknown" : $"TPM Protected: {ds.TpmProtected}";
        TxtKeyProvider.Text = string.IsNullOrEmpty(ds.KeyProvider) ? "Key Provider: N/A" : $"Key Provider: {ds.KeyProvider}";

        LstParsedFields.ItemsSource = ds.ParsedProperties;
        TxtRawOutput.Text = ds.RawOutput;
    }

    private async void BtnRerun_Click(object sender, RoutedEventArgs e)
    {
        BtnRerun.IsEnabled = false;
        try
        {
            var ds = await DsregcmdCollector.CollectAsync();
            SystemDiagnosticsService.Instance.CurrentReport.Dsregcmd = ds;
            PopulateUI(ds);
        }
        finally
        {
            BtnRerun.IsEnabled = true;
        }
    }

    private void BtnCopyRaw_Click(object sender, RoutedEventArgs e)
    {
        var dp = new DataPackage();
        dp.SetText(TxtRawOutput.Text);
        Clipboard.SetContent(dp);
    }

    private void BtnOpenSystemPropertiesDomain_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "SystemPropertiesComputerName.exe",
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "control.exe",
                    Arguments = "sysdm.cpl,,1",
                    UseShellExecute = true
                });
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "rundll32.exe",
                        Arguments = "shell32.dll,Control_RunDLL sysdm.cpl,,1",
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }
    }
}
