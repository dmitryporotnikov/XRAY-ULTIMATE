using System;

namespace XRAY_ULTIMATE.Models;

public class SensorData
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double MemoryUsedGB { get; set; }
    public double MemoryTotalGB { get; set; }
    public double DiskReadMBs { get; set; }
    public double DiskWriteMBs { get; set; }
    public double NetworkRxKbps { get; set; }
    public double NetworkTxKbps { get; set; }
    public string SystemUptime { get; set; } = string.Empty;

    // Thermal & Temperature Sensors
    public double CpuTemperatureCelsius { get; set; }
    public double CpuTemperatureFahrenheit => (CpuTemperatureCelsius * 9.0 / 5.0) + 32.0;
    public double SystemTemperatureCelsius { get; set; }
    public double StorageTemperatureCelsius { get; set; }
    public double CriticalTripPointCelsius { get; set; } = 100.0;
    public string ThermalStatus { get; set; } = "Normal";

    // GPU Thermal & Utilization Sensors
    public double GpuTemperatureCelsius { get; set; }
    public double GpuTemperatureFahrenheit => (GpuTemperatureCelsius * 9.0 / 5.0) + 32.0;
    public string GpuName { get; set; } = string.Empty;
    public double GpuUsagePercent { get; set; }
    public string GpuThermalStatus { get; set; } = "Normal";

    public string CpuTemperatureDisplay => $"{CpuTemperatureCelsius:F1} °C ({CpuTemperatureFahrenheit:F1} °F)";
    public string GpuTemperatureDisplay => $"{GpuTemperatureCelsius:F1} °C ({GpuTemperatureFahrenheit:F1} °F)";
}
