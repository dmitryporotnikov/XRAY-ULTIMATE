using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;

namespace XRAY_ULTIMATE.Services;

public static class WmiService
{
    public static List<Dictionary<string, object?>> Query(string query, string scope = @"root\cimv2", int maxResults = 0)
    {
        var results = new List<Dictionary<string, object?>>();
        try
        {
            string normalizedScope = scope.StartsWith(@"\\", StringComparison.Ordinal)
                ? scope
                : @"\\.\" + scope.TrimStart('\\');

            var mgmtScope = new ManagementScope(normalizedScope, new ConnectionOptions
            {
                Impersonation = ImpersonationLevel.Impersonate,
                Authentication = AuthenticationLevel.PacketPrivacy,
                EnablePrivileges = true,
                Timeout = TimeSpan.FromSeconds(15)
            });

            var enumOptions = new EnumerationOptions
            {
                ReturnImmediately = true,
                Rewindable = false,
                DirectRead = true,
                Timeout = TimeSpan.FromSeconds(15)
            };

            using var searcher = new ManagementObjectSearcher(mgmtScope, new ObjectQuery(query), enumOptions);
            using var collection = searcher.Get();

            foreach (ManagementBaseObject baseObj in collection)
            {
                using (baseObj)
                {
                    var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    foreach (PropertyData prop in baseObj.Properties)
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
                    if (maxResults > 0 && results.Count >= maxResults)
                    {
                        break;
                    }
                }
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
        var list = Query(query, scope, maxResults: 1);
        return list.Count > 0 ? list[0] : null;
    }

    public static string GetString(this Dictionary<string, object?> dict, string key, string fallback = "")
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            if (val is string s)
            {
                var trimmed = s.Trim();
                return string.IsNullOrEmpty(trimmed) ? fallback : trimmed;
            }
            var str = val.ToString()?.Trim();
            return string.IsNullOrEmpty(str) ? fallback : str;
        }
        return fallback;
    }

    public static ulong GetUlong(this Dictionary<string, object?> dict, string key, ulong fallback = 0)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            return val switch
            {
                ulong ul => ul,
                uint ui => ui,
                ushort us => us,
                byte b => b,
                long l when l >= 0 => (ulong)l,
                int i when i >= 0 => (ulong)i,
                _ => ulong.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback
            };
        }
        return fallback;
    }

    public static uint GetUint(this Dictionary<string, object?> dict, string key, uint fallback = 0)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            return val switch
            {
                uint ui => ui,
                ushort us => us,
                byte b => b,
                ulong ul when ul <= uint.MaxValue => (uint)ul,
                int i when i >= 0 => (uint)i,
                long l when l >= 0 && l <= uint.MaxValue => (uint)l,
                _ => uint.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback
            };
        }
        return fallback;
    }

    public static int GetInt(this Dictionary<string, object?> dict, string key, int fallback = 0)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            return val switch
            {
                int i => i,
                short s => s,
                ushort us => us,
                byte b => b,
                uint ui when ui <= int.MaxValue => (int)ui,
                long l when l >= int.MinValue && l <= int.MaxValue => (int)l,
                _ => int.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback
            };
        }
        return fallback;
    }

    public static bool GetBool(this Dictionary<string, object?> dict, string key, bool fallback = false)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            return val switch
            {
                bool b => b,
                int i => i != 0,
                uint ui => ui != 0,
                ushort us => us != 0,
                byte by => by != 0,
                _ => bool.TryParse(val.ToString(), out var parsed) ? parsed : fallback
            };
        }
        return fallback;
    }
}
