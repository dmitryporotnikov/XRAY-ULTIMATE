using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class SecurityPage : Page
{
    public SecurityPage()
    {
        this.InitializeComponent();
        this.Loaded += SecurityPage_Loaded;
    }

    private void SecurityPage_Loaded(object sender, RoutedEventArgs e)
    {
        var sec = SystemDiagnosticsService.Instance.CurrentReport.Security;

        TxtTpmSpec.Text = sec.Tpm.IsPresent ? $"TPM {sec.Tpm.SpecVersion}" : "Not Detected";
        TxtTpmStatus.Text = sec.Tpm.IsPresent ? (sec.Tpm.IsEnabled ? "Enabled & Active" : "Present") : "TPM 2.0 Missing or Disabled in BIOS";
        TxtTpmMfg.Text = $"Manufacturer: {(string.IsNullOrEmpty(sec.Tpm.ManufacturerName) ? "Hardware Security" : sec.Tpm.ManufacturerName)}";
        if (TxtHeaderTpmBadge != null)
            TxtHeaderTpmBadge.Text = sec.Tpm.IsPresent ? $"TPM {sec.Tpm.SpecVersion} Active" : "No TPM";

        TxtSecureBoot.Text = $"Secure Boot: {(sec.SecureBootEnabled ? "Enabled" : "Disabled (UEFI Supported)")}";
        TxtUac.Text = $"UAC: {sec.UacStatus}";
        if (TxtHeaderSbBadge != null)
            TxtHeaderSbBadge.Text = sec.SecureBootEnabled ? "Secure Boot: Active" : "Secure Boot: Disabled";

        if (sec.AntivirusProducts.Count > 0)
        {
            var av = sec.AntivirusProducts[0];
            TxtAvName.Text = av.DisplayName;
            TxtAvStatus.Text = av.StateStatus;
            if (TxtHeaderAvBadge != null) TxtHeaderAvBadge.Text = av.DisplayName;
        }
        else
        {
            TxtAvName.Text = "Windows Defender Antivirus";
            TxtAvStatus.Text = "Active & Enforced";
            if (TxtHeaderAvBadge != null) TxtHeaderAvBadge.Text = "Windows Defender";
        }

        LstBitLocker.ItemsSource = sec.BitLockerDrives;
        LstHotfixes.ItemsSource = sec.InstalledHotfixes;
        LstSecurityPropertyGroups.ItemsSource = Models.PropertyItem.GroupByCategory(sec.Properties);
    }

    private void BtnOpenWindowsSecurity_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "windowsdefender:",
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "wscui.cpl",
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
