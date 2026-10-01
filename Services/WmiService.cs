using System;
using System.Collections.Generic;
using System.Management;

namespace XRAY_ULTIMATE.Services;

public static class WmiService
{
    public static List<Dictionary<string, object?>> Query(string query, string scope = @"root\cimv2")
    {
        var results = new List<Dictionary<string, object?>>();
        try
        {
            var mgmtScope = new ManagementScope(scope, new ConnectionOptions
            {
                Impersonation = ImpersonationLevel.Impersonate,
                Authentication = AuthenticationLevel.PacketPrivacy,
                Timeout = TimeSpan.FromSeconds(20)
            });

            using var searcher = new ManagementObjectSearcher(mgmtScope, new ObjectQuery(query));
            searcher.Options.Timeout = TimeSpan.FromSeconds(20);
            using var collection = searcher.Get();

            foreach (ManagementObject mo in collection)
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (PropertyData prop in mo.Properties)
                {
                    try
                    {
                        dict[prop.Name] = prop.Value;
                    }
                    catch
                    {
                        dict[prop.Name] = null;
                    }
                }
                results.Add(dict);
                mo.Dispose();
            }
        }
        catch (Exception)
        {
            // Ignore WMI errors gracefully (e.g. absent namespace, access denied)
        }
        return results;
    }

    public static Dictionary<string, object?>? QueryFirst(string query, string scope = @"root\cimv2")
    {
        var list = Query(query, scope);
        return list.Count > 0 ? list[0] : null;
    }

    public static string GetString(this Dictionary<string, object?> dict, string key, string fallback = "")
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            var str = val.ToString()?.Trim();
            return string.IsNullOrEmpty(str) ? fallback : str;
        }
        return fallback;
    }

    public static ulong GetUlong(this Dictionary<string, object?> dict, string key, ulong fallback = 0)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            if (ulong.TryParse(val.ToString(), out var parsed)) return parsed;
        }
        return fallback;
    }

    public static uint GetUint(this Dictionary<string, object?> dict, string key, uint fallback = 0)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            if (uint.TryParse(val.ToString(), out var parsed)) return parsed;
        }
        return fallback;
    }

    public static int GetInt(this Dictionary<string, object?> dict, string key, int fallback = 0)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            if (int.TryParse(val.ToString(), out var parsed)) return parsed;
        }
        return fallback;
    }

    public static bool GetBool(this Dictionary<string, object?> dict, string key, bool fallback = false)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            if (bool.TryParse(val.ToString(), out var parsed)) return parsed;
        }
        return fallback;
    }
}
