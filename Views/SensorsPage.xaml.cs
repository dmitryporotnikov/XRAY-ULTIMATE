using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Models;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class SensorsPage : Page
{
    private LiveSensorService? _sensorService;

    public SensorsPage()
    {
        this.InitializeComponent();
        this.Loaded += SensorsPage_Loaded;
        this.Unloaded += SensorsPage_Unloaded;
    }

    private async void SensorsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (BrdSensorsLoading != null) BrdSensorsLoading.Visibility = Visibility.Visible;
        if (PanelSensorsContent != null) PanelSensorsContent.Visibility = Visibility.Collapsed;

        await System.Threading.Tasks.Task.Yield();

        _sensorService = new LiveSensorService();
        _sensorService.SensorUpdated += SensorService_SensorUpdated;
        int interval = GetSelectedInterval();
        _sensorService.Start(interval);

        if (BrdSensorsLoading != null) BrdSensorsLoading.Visibility = Visibility.Collapsed;
        if (PanelSensorsContent != null) PanelSensorsContent.Visibility = Visibility.Visible;
    }

    private void SensorsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_sensorService != null)
        {
            _sensorService.SensorUpdated -= SensorService_SensorUpdated;
            _sensorService.Dispose();
            _sensorService = null;
        }
    }

    private void SensorService_SensorUpdated(object? sender, SensorData data)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            TxtCpuLive.Text = $"{data.CpuUsagePercent:F1}%";
            PrgCpuLive.Value = data.CpuUsagePercent;
            TxtCpuLiveSub.Text = $"{Environment.ProcessorCount} Logical Processor Threads Active";

            TxtRamLive.Text = $"{data.MemoryUsagePercent:F0}%";
            PrgRamLive.Value = data.MemoryUsagePercent;
            TxtRamLiveSub.Text = $"Used: {data.MemoryUsedGB:F2} GB / Total: {data.MemoryTotalGB:F2} GB";

            TxtNetRx.Text = data.NetworkRxKbps >= 1000 ? $"{data.NetworkRxKbps / 1000.0:F2} Mbps" : $"{data.NetworkRxKbps:F1} Kbps";
            TxtNetTx.Text = data.NetworkTxKbps >= 1000 ? $"{data.NetworkTxKbps / 1000.0:F2} Mbps" : $"{data.NetworkTxKbps:F1} Kbps";

            // Temperature Sensors
            TxtCpuTemp.Text = $"{data.CpuTemperatureCelsius:F1} °C";
            TxtCpuTempF.Text = $"{data.CpuTemperatureFahrenheit:F1} °F";
            PrgCpuTemp.Value = data.CpuTemperatureCelsius;
            TxtThermalStatus.Text = data.ThermalStatus;
            TxtCritPoint.Text = $"Throttle: {data.CriticalTripPointCelsius:F0} °C";

            // GPU Temperature
            TxtGpuTemp.Text = $"{data.GpuTemperatureCelsius:F1} °C";
            TxtGpuTempF.Text = $"{data.GpuTemperatureFahrenheit:F1} °F";
            PrgGpuTemp.Value = data.GpuTemperatureCelsius;
            TxtGpuNameSensor.Text = string.IsNullOrEmpty(data.GpuName) ? "Graphics Processor" : data.GpuName;
            TxtGpuThermalStatus.Text = data.GpuThermalStatus;

            TxtSysTemp.Text = $"{data.SystemTemperatureCelsius:F1} °C";
            PrgSysTemp.Value = data.SystemTemperatureCelsius;

            TxtDriveTemp.Text = $"{data.StorageTemperatureCelsius:F1} °C";
            PrgDriveTemp.Value = data.StorageTemperatureCelsius;

            TxtLiveUptime.Text = data.SystemUptime;
        });
    }

    private void CmbInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_sensorService != null)
        {
            _sensorService.Start(GetSelectedInterval());
        }
    }

    private int GetSelectedInterval()
    {
        int idx = CmbInterval?.SelectedIndex ?? 1;
        return idx switch
        {
            0 => 500,
            1 => 1000,
            2 => 2000,
            _ => 1000
        };
    }
}
