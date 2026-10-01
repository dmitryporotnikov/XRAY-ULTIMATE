using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class MotherboardPage : Page
{
    public MotherboardPage()
    {
        this.InitializeComponent();
        this.Loaded += MotherboardPage_Loaded;
    }

    private void MotherboardPage_Loaded(object sender, RoutedEventArgs e)
    {
        var mb = SystemDiagnosticsService.Instance.CurrentReport.Motherboard;
        var ov = SystemDiagnosticsService.Instance.CurrentReport.Overview;

        TxtBoardTitle.Text = string.IsNullOrEmpty(mb.Product) ? "Generic Motherboard" : mb.Product;
        TxtBoardVendor.Text = string.IsNullOrEmpty(mb.Manufacturer) ? "System Manufacturer" : mb.Manufacturer;
        TxtBoardVersion.Text = $"Version: {(string.IsNullOrEmpty(mb.Version) ? "N/A" : mb.Version)}";
        TxtBoardSerial.Text = $"Serial: {(string.IsNullOrEmpty(mb.SerialNumber) ? "N/A" : mb.SerialNumber)}";

        TxtBiosVer.Text = string.IsNullOrEmpty(mb.BiosVersion) ? "Unknown" : mb.BiosVersion;
        TxtBiosVendor.Text = mb.BiosVendor;
        TxtBiosDate.Text = string.IsNullOrEmpty(mb.BiosReleaseDate) ? "Unknown" : mb.BiosReleaseDate;
        TxtSmbios.Text = $"SMBIOS {mb.SmbiosVersion}";
        TxtBootMode.Text = $"Boot Mode: {ov.BootMode}";

        LstMotherboardProperties.ItemsSource = mb.Properties;
    }
}
