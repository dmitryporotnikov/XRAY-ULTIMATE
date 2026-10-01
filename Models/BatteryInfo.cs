using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class BatteryInfo
{
    public bool IsBatteryPresent { get; set; }
    public string PowerLineStatus { get; set; } = "Unknown"; // AC Connected / On Battery
    public int BatteryLifePercent { get; set; }
    public string BatteryState { get; set; } = "Unknown"; // Charging, Discharging, Full
    public int EstimatedRunTimeSeconds { get; set; }
    public uint DesignCapacityMWh { get; set; }
    public uint FullChargeCapacityMWh { get; set; }
    public double BatteryHealthPercent { get; set; }
    public string Chemistry { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string ActivePowerScheme { get; set; } = string.Empty;

    public List<PropertyItem> Properties { get; set; } = new();
}
