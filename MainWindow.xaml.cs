using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Services;
using XRAY_ULTIMATE.Views;

namespace XRAY_ULTIMATE;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        // Initialize Window Handle for pickers
        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WindowHelper.MainWindowHandle = hWnd;

        // Custom TitleBar
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);

        // AppWindow sizing
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        if (appWindow != null)
        {
            // Set Application Window & Taskbar Icon
            string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "appicon.ico");
            if (System.IO.File.Exists(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }

            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            int width = 1440;
            int height = 900;
            if (displayArea != null)
            {
                // Target 75% of screen width and 80% of screen height
                width = Math.Max(1280, (int)(displayArea.WorkArea.Width * 0.75));
                height = Math.Max(820, (int)(displayArea.WorkArea.Height * 0.80));

                appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

                var centeredPosition = appWindow.Position;
                centeredPosition.X = Math.Max(0, (displayArea.WorkArea.Width - width) / 2);
                centeredPosition.Y = Math.Max(0, (displayArea.WorkArea.Height - height) / 2);
                appWindow.Move(centeredPosition);
            }
            else
            {
                appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
            }
        }

        // Start Initial Hardware Discovery
        _ = InitializeSystemAsync();
    }

    private async Task InitializeSystemAsync()
    {
        BrdInitialLoading.Visibility = Visibility.Visible;

        var progress = new Progress<string>(msg =>
        {
            TxtInitProgress.Text = msg;
        });

        await SystemDiagnosticsService.Instance.RefreshAllAsync(progress);

        BrdInitialLoading.Visibility = Visibility.Collapsed;
        NavView.SelectedItem = NavDashboard;
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            Type? pageType = tag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Cpu" => typeof(CpuPage),
                "Memory" => typeof(MemoryPage),
                "Motherboard" => typeof(MotherboardPage),
                "Acpi" => typeof(AcpiPage),
                "DeviceManager" => typeof(DeviceManagerPage),
                "Gpu" => typeof(GpuPage),
                "Storage" => typeof(StoragePage),
                "Network" => typeof(NetworkPage),
                "Battery" => typeof(BatteryPage),
                "Security" => typeof(SecurityPage),
                "Dsregcmd" => typeof(DsregcmdPage),
                "Filters" => typeof(FiltersPage),
                "Software" => typeof(SoftwarePage),
                "Processes" => typeof(ProcessesPage),
                "Sensors" => typeof(SensorsPage),
                "Benchmark" => typeof(BenchmarkPage),
                "Export" => typeof(ExportPage),
                _ => typeof(DashboardPage)
            };

            if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
            {
                if (PrgNavLoading != null) PrgNavLoading.Visibility = Visibility.Visible;
                ContentFrame.Navigate(pageType);
            }
        }
    }

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        if (PrgNavLoading != null) PrgNavLoading.Visibility = Visibility.Collapsed;
    }

    private async void BtnQuickRefresh_Click(object sender, RoutedEventArgs e)
    {
        BtnQuickRefresh.IsEnabled = false;
        try
        {
            await SystemDiagnosticsService.Instance.RefreshAllAsync();
            // Re-navigate current page to trigger refresh
            var currentType = ContentFrame.CurrentSourcePageType ?? typeof(DashboardPage);
            ContentFrame.Navigate(currentType);
        }
        finally
        {
            BtnQuickRefresh.IsEnabled = true;
        }
    }

    private void BtnQuickExport_Click(object sender, RoutedEventArgs e)
    {
        NavView.SelectedItem = NavExport;
        ContentFrame.Navigate(typeof(ExportPage));
    }
}
