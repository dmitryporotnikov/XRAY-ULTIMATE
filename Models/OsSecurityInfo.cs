using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class OsSecurityInfo
{
    public string ProductName { get; set; } = string.Empty;
    public string DisplayVersion { get; set; } = string.Empty;
    public string CurrentBuild { get; set; } = string.Empty;
    public string Ubr { get; set; } = string.Empty;
    public string FullBuildString => string.IsNullOrEmpty(Ubr) ? CurrentBuild : $"{CurrentBuild}.{Ubr}";
    public string InstallDate { get; set; } = string.Empty;
    public string RegisteredOwner { get; set; } = string.Empty;
    public string RegisteredOrganization { get; set; } = string.Empty;
    public string WindowsFolder { get; set; } = string.Empty;
    public string SystemFolder { get; set; } = string.Empty;

    public bool SecureBootEnabled { get; set; }
    public string UacStatus { get; set; } = string.Empty;
    public string FirewallStatus { get; set; } = string.Empty;

    public TpmInfo Tpm { get; set; } = new();
    public List<AntivirusInfo> AntivirusProducts { get; set; } = new();
    public List<BitLockerInfo> BitLockerDrives { get; set; } = new();
    public List<HotfixInfo> InstalledHotfixes { get; set; } = new();

    public List<PropertyItem> Properties { get; set; } = new();
}

public class TpmInfo
{
    public bool IsPresent { get; set; }
    public string SpecVersion { get; set; } = string.Empty;
    public string ManufacturerName { get; set; } = string.Empty;
    public string ManufacturerVersion { get; set; } = string.Empty;
    public bool IsActivated { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsOwned { get; set; }
}

public class AntivirusInfo
{
    public string DisplayName { get; set; } = string.Empty;
    public string InstanceGuid { get; set; } = string.Empty;
    public string PathToSignedProductExe { get; set; } = string.Empty;
    public string StateStatus { get; set; } = string.Empty; // Enabled / Disabled, Up to date / Out of date
    public bool IsActive { get; set; }
    public bool IsUpToDate { get; set; }
}

public class BitLockerInfo
{
    public string DriveLetter { get; set; } = string.Empty;
    public string ProtectionStatus { get; set; } = string.Empty;
    public string EncryptionMethod { get; set; } = string.Empty;
    public string LockStatus { get; set; } = string.Empty;
}

public class HotfixInfo
{
    public string HotFixId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InstalledOn { get; set; } = string.Empty;
    public string InstalledBy { get; set; } = string.Empty;
}
