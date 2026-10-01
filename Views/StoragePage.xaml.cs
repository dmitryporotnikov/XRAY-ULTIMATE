using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class StoragePage : Page
{
    public StoragePage()
    {
        this.InitializeComponent();
        this.Loaded += StoragePage_Loaded;
    }

    private void StoragePage_Loaded(object sender, RoutedEventArgs e)
    {
        var storage = SystemDiagnosticsService.Instance.CurrentReport.Storage;
        if (TxtDrivesCount != null) TxtDrivesCount.Text = $"Drives: {storage.PhysicalDisks.Count}";
        if (TxtVolumesCount != null) TxtVolumesCount.Text = $"Volumes: {storage.Volumes.Count}";
        LstVolumes.ItemsSource = storage.Volumes;
        LstDisks.ItemsSource = storage.PhysicalDisks;
        LstStoragePropertyGroups.ItemsSource = Models.PropertyItem.GroupByCategory(storage.Properties);
    }

    private void BtnOpenDiskManagement_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "diskmgmt.msc",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
