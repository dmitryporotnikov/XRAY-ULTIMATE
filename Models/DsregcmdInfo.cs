using System.Collections.Generic;

namespace XRAY_ULTIMATE.Models;

public class DsregcmdInfo
{
    public bool AzureAdJoined { get; set; }
    public bool EnterpriseJoined { get; set; }
    public bool DomainJoined { get; set; }
    public bool WorkplaceJoined { get; set; }

    public string DeviceId { get; set; } = string.Empty;
    public string DeviceCertificateValidity { get; set; } = string.Empty;
    public string KeyContainerId { get; set; } = string.Empty;
    public string KeyProvider { get; set; } = string.Empty;
    public string TpmProtected { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string Idp { get; set; } = string.Empty;

    public bool AzureAdPrt { get; set; }
    public string AzureAdPrtUpdateTime { get; set; } = string.Empty;
    public string AzureAdPrtAuthority { get; set; } = string.Empty;

    public string NgcSet { get; set; } = string.Empty; // Windows Hello
    public string WamDefaultSet { get; set; } = string.Empty;
    public string DomainName { get; set; } = string.Empty;

    public string OverallStatus
    {
        get
        {
            if (AzureAdJoined && DomainJoined) return "Hybrid Azure AD / Entra ID Joined";
            if (AzureAdJoined) return "Azure AD / Entra ID Joined";
            if (DomainJoined) return "On-Premises Active Directory Domain Joined";
            if (WorkplaceJoined) return "Workplace / Entra Registered";
            return "Workgroup / Standalone (Not Cloud Joined)";
        }
    }

    public List<PropertyItem> ParsedProperties { get; set; } = new();
    public string RawOutput { get; set; } = string.Empty;
}
