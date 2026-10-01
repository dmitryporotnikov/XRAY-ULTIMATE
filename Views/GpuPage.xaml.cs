using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Models;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class GpuPage : Page
{
    public GpuPage()
    {
        this.InitializeComponent();
        this.Loaded += GpuPage_Loaded;
    }

    private void GpuPage_Loaded(object sender, RoutedEventArgs e)
    {
        var gpu = SystemDiagnosticsService.Instance.CurrentReport.Gpu;

        TxtGpuCount.Text = $"{gpu.Adapters.Count} Video Adapter(s)";
        TxtMonCount.Text = $"{gpu.Monitors.Count} Connected Display(s)";

        if (gpu.Adapters.Count > 0)
        {
            var primary = gpu.Adapters[0];
            TxtPrimaryGpuName.Text = primary.Name;
            TxtPrimaryDriver.Text = primary.FormattedDriver;
            TxtPrimaryVram.Text = primary.FormattedVram;
            TxtPrimaryMode.Text = primary.FormattedResolution;
        }

        LstGpus.ItemsSource = gpu.Adapters;
        LstMonitors.ItemsSource = gpu.Monitors;
        LstGpuPropertyGroups.ItemsSource = PropertyItem.GroupByCategory(gpu.Properties);
    }
}
