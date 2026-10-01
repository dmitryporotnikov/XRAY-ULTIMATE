using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class MemoryPage : Page
{
    public MemoryPage()
    {
        this.InitializeComponent();
        this.Loaded += MemoryPage_Loaded;
    }

    private void MemoryPage_Loaded(object sender, RoutedEventArgs e)
    {
        var mem = SystemDiagnosticsService.Instance.CurrentReport.Memory;

        TxtRamSummary.Text = $"Total: {UnitFormatter.FormatBytes(mem.TotalPhysicalBytes)} | Available: {UnitFormatter.FormatBytes(mem.AvailablePhysicalBytes)} | Used: {UnitFormatter.FormatBytes(mem.UsedPhysicalBytes)}";
        TxtRamPercentLarge.Text = $"{mem.MemoryLoadPercent:F0}%";
        PrgRamLarge.Value = mem.MemoryLoadPercent;

        TxtPhysTotal.Text = $"Total Installed: {UnitFormatter.FormatBytes(mem.TotalPhysicalBytes)}";
        TxtPhysUsed.Text = $"Used RAM: {UnitFormatter.FormatBytes(mem.UsedPhysicalBytes)}";
        TxtPhysAvail.Text = $"Available: {UnitFormatter.FormatBytes(mem.AvailablePhysicalBytes)}";

        double pagePercent = mem.TotalPageFileBytes > 0 ? (double)mem.UsedPageFileBytes / mem.TotalPageFileBytes * 100.0 : 0;
        PrgPagefile.Value = pagePercent;
        TxtPagefileUsed.Text = $"Allocated: {UnitFormatter.FormatBytes(mem.UsedPageFileBytes)} ({pagePercent:F0}%)";
        TxtPagefileTotal.Text = $"Total File: {UnitFormatter.FormatBytes(mem.TotalPageFileBytes)}";

        TxtVirtTotal.Text = $"Total: {UnitFormatter.FormatBytes(mem.TotalVirtualBytes)}";
        TxtVirtAvail.Text = $"Free: {UnitFormatter.FormatBytes(mem.AvailableVirtualBytes)}";

        LstMemorySticks.ItemsSource = mem.MemorySticks;
        LstMemoryPropertyGroups.ItemsSource = PropertyItem.GroupByCategory(mem.Properties);
    }
}
