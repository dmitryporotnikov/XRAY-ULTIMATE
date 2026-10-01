using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class ProcessServiceCollector
{
    public static ProcessServiceInfo Collect()
    {
        var info = new ProcessServiceInfo();

        // Processes
        try
        {
            var processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                try
                {
                    var item = new ProcessItem
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        MemoryWorkingSetMB = p.WorkingSet64 / (1024.0 * 1024.0),
                        ThreadCount = p.Threads.Count
                    };

                    try
                    {
                        item.StartTime = p.StartTime;
                    }
                    catch { }

                    try
                    {
                        item.FilePath = p.MainModule?.FileName ?? "";
                    }
                    catch { }

                    info.Processes.Add(item);
                }
                catch { }
                finally
                {
                    p.Dispose();
                }
            }

            info.Processes = info.Processes
                .OrderByDescending(x => x.MemoryWorkingSetMB)
                .ToList();
        }
        catch { }

        // Services
        try
        {
            var services = ServiceController.GetServices();
            foreach (var s in services)
            {
                try
                {
                    info.Services.Add(new WindowsServiceItem
                    {
                        ServiceName = s.ServiceName,
                        DisplayName = string.IsNullOrEmpty(s.DisplayName) ? s.ServiceName : s.DisplayName,
                        Status = s.Status.ToString(),
                        StartType = s.StartType.ToString()
                    });
                }
                catch { }
                finally
                {
                    s.Dispose();
                }
            }

            info.Services = info.Services
                .OrderBy(s => s.Status == "Running" ? 0 : 1)
                .ThenBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch { }

        return info;
    }
}
