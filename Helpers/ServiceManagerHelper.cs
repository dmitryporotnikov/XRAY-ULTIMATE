using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace XRAY_ULTIMATE.Helpers;

public static class ServiceManagerHelper
{
    private const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
    private const uint SERVICE_ALL_ACCESS = 0xF01FF;
    private const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;

    private const uint SERVICE_AUTO_START = 2;
    private const uint SERVICE_DEMAND_START = 3;
    private const uint SERVICE_DISABLED = 4;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenSCManager(string? machineName, string? databaseName, uint dwAccess);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ChangeServiceConfig(
        IntPtr hService,
        uint dwServiceType,
        uint dwStartType,
        uint dwErrorControl,
        string? lpBinaryPathName,
        string? lpLoadOrderGroup,
        IntPtr lpdwTagId,
        string? lpDependencies,
        string? lpServiceStartName,
        string? lpPassword,
        string? lpDisplayName);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseServiceHandle(IntPtr hSCObject);

    public static void OpenTaskManager()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "taskmgr.exe",
                UseShellExecute = true
            });
        }
        catch { }
    }

    public static void OpenServicesMsc()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "services.msc",
                UseShellExecute = true
            });
        }
        catch { }
    }

    public static async Task<(bool Success, string Message)> StartServiceAsync(string serviceName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status == ServiceControllerStatus.Running)
                    return (true, $"Service '{serviceName}' is already running.");

                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8));
                return (true, $"Service '{serviceName}' started successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to start '{serviceName}': {ex.Message}");
            }
        });
    }

    public static async Task<(bool Success, string Message)> StopServiceAsync(string serviceName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status == ServiceControllerStatus.Stopped)
                    return (true, $"Service '{serviceName}' is already stopped.");

                if (!sc.CanStop)
                    return (false, $"Service '{serviceName}' reports it cannot be stopped.");

                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8));
                return (true, $"Service '{serviceName}' stopped successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to stop '{serviceName}': {ex.Message}");
            }
        });
    }

    public static async Task<(bool Success, string Message)> RestartServiceAsync(string serviceName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    if (!sc.CanStop)
                        return (false, $"Service '{serviceName}' reports it cannot be stopped.");

                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8));
                }

                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8));
                return (true, $"Service '{serviceName}' restarted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to restart '{serviceName}': {ex.Message}");
            }
        });
    }

    public static async Task<(bool Success, string Message)> ChangeStartupTypeAsync(string serviceName, string newStartType)
    {
        return await Task.Run(() =>
        {
            uint startCode = newStartType.ToLowerInvariant() switch
            {
                "automatic" => SERVICE_AUTO_START,
                "manual" => SERVICE_DEMAND_START,
                "disabled" => SERVICE_DISABLED,
                _ => SERVICE_DEMAND_START
            };

            IntPtr scm = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            if (scm == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                return (false, $"Access Denied opening Service Manager (Error {err})");
            }

            try
            {
                IntPtr svc = OpenService(scm, serviceName, SERVICE_ALL_ACCESS);
                if (svc == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    return (false, $"Access Denied opening service '{serviceName}' (Error {err})");
                }

                try
                {
                    bool success = ChangeServiceConfig(
                        svc,
                        SERVICE_NO_CHANGE,
                        startCode,
                        SERVICE_NO_CHANGE,
                        null, null, IntPtr.Zero, null, null, null, null);

                    if (!success)
                    {
                        int err = Marshal.GetLastWin32Error();
                        return (false, $"Failed to configure service '{serviceName}' (Error {err})");
                    }

                    return (true, $"Startup type for '{serviceName}' changed to {newStartType}.");
                }
                finally
                {
                    CloseServiceHandle(svc);
                }
            }
            finally
            {
                CloseServiceHandle(scm);
            }
        });
    }
}
