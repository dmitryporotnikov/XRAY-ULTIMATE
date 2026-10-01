using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class SystemOverviewInfo
{
    public string ComputerName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DomainOrWorkgroup { get; set; } = string.Empty;
    public string OsName { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string OsBuild { get; set; } = string.Empty;
    public string OsArchitecture { get; set; } = string.Empty;
    public string InstallDate { get; set; } = string.Empty;
    public string SystemUptime { get; set; } = string.Empty;
    public string BootMode { get; set; } = string.Empty;
    public string SecureBootState { get; set; } = string.Empty;
    public string BitLockerStatus { get; set; } = string.Empty;
    public bool HypervisorPresent { get; set; }
    public string TimeZone { get; set; } = string.Empty;

    public List<PropertyItem> Properties { get; set; } = new();
}
