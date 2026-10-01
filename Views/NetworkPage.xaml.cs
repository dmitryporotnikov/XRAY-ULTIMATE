using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class NetworkPage : Page
{
    public NetworkPage()
    {
        this.InitializeComponent();
        this.Loaded += NetworkPage_Loaded;
    }

    private void NetworkPage_Loaded(object sender, RoutedEventArgs e)
    {
        var net = SystemDiagnosticsService.Instance.CurrentReport.Network;

        if (net.WifiDetails != null)
        {
            BrdWifi.Visibility = Visibility.Visible;
            TxtWifiSsid.Text = net.WifiDetails.Ssid;
            TxtWifiRadio.Text = net.WifiDetails.RadioType;
            TxtWifiChannel.Text = $"Channel: {net.WifiDetails.Channel}";
            TxtWifiSecurity.Text = $"{net.WifiDetails.Authentication} / {net.WifiDetails.Cipher} (BSSID: {net.WifiDetails.BSSID})";
            TxtWifiRates.Text = $"Rx: {net.WifiDetails.ReceiveRateMbps} Mbps | Tx: {net.WifiDetails.TransmitRateMbps} Mbps";
            TxtWifiSignal.Text = $"{net.WifiDetails.SignalQuality}%";
            PrgWifiSignal.Value = net.WifiDetails.SignalQuality;
        }
        else
        {
            BrdWifi.Visibility = Visibility.Collapsed;
        }

        LstAdapters.ItemsSource = net.Adapters;
    }

    private void BtnOpenNcpa_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ncpa.cpl",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
