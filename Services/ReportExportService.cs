using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class ReportExportService
{
    public static async Task ExportToJsonAsync(SystemReport report, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        string json = JsonSerializer.Serialize(report, options);
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
    }

    public static async Task ExportToTextAsync(SystemReport report, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("                   XRAY ULTIMATE - SYSTEM DIAGNOSTICS REPORT                   ");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Computer Name: {report.Overview.ComputerName}");
        sb.AppendLine($"OS: {report.Overview.OsName} (Build {report.Overview.OsBuild})");
        sb.AppendLine($"CPU: {report.Cpu.ProcessorName}");
        sb.AppendLine($"Memory: {report.Memory.Properties.Find(p => p.Property == "Total Installed RAM")?.Value}");
        sb.AppendLine();

        void AppendSection(string title, System.Collections.Generic.List<PropertyItem> items)
        {
            sb.AppendLine($"--- [ {title.ToUpperInvariant()} ] ------------------------------------------------");
            string currentCategory = "";
            foreach (var item in items)
            {
                if (item.Category != currentCategory)
                {
                    currentCategory = item.Category;
                    sb.AppendLine($"\n  <{currentCategory}>");
                }
                sb.AppendLine($"    {item.Property,-32}: {item.DisplayValue}");
            }
            sb.AppendLine();
        }

        AppendSection("System Overview", report.Overview.Properties);
        AppendSection("Processor (CPU)", report.Cpu.Properties);
        AppendSection("Memory (RAM)", report.Memory.Properties);
        AppendSection("Motherboard & BIOS", report.Motherboard.Properties);
        AppendSection("Display & GPU", report.Gpu.Properties);
        AppendSection("Storage & Drives", report.Storage.Properties);
        AppendSection("Network", report.Network.Properties);
        AppendSection("Battery & Power", report.Battery.Properties);
        AppendSection("Security & TPM", report.Security.Properties);
        AppendSection("Entra ID / Cloud Join", report.Dsregcmd.ParsedProperties);
        AppendSection("Filter Drivers (fltmc)", report.FilterDrivers.Properties);
        AppendSection("ACPI & DSDT Tables", report.Acpi.Properties);
        AppendSection("Device Manager", report.DeviceManager.Properties);

        // Benchmark
        if (report.Benchmark != null && report.Benchmark.IsCompleted)
        {
            sb.AppendLine("--- [ HARDWARE BENCHMARK ] -----------------------------------------------");
            sb.AppendLine($"    Single-Thread Score             : {report.Benchmark.CpuSingleThreadScore:N0} pts");
            sb.AppendLine($"    Multi-Thread Score              : {report.Benchmark.CpuMultiThreadScore:N0} pts");
            sb.AppendLine($"    Multi-Thread Efficiency         : {report.Benchmark.MultiThreadEfficiencyRatio:F2}x");
            sb.AppendLine($"    Memory Read Bandwidth           : {report.Benchmark.MemoryReadBandwidthMBs:F1} MB/s");
            sb.AppendLine($"    Memory Write Bandwidth          : {report.Benchmark.MemoryWriteBandwidthMBs:F1} MB/s");
            sb.AppendLine($"    Memory Copy Bandwidth           : {report.Benchmark.MemoryCopyBandwidthMBs:F1} MB/s");
            sb.AppendLine($"    Memory Latency                  : {report.Benchmark.MemoryLatencyNs:F1} ns");
            sb.AppendLine();
        }

        // Software
        sb.AppendLine("--- [ INSTALLED SOFTWARE APPLICATIONS ] ---------------------------------");
        foreach (var app in report.Software.Applications)
        {
            sb.AppendLine($"    {app.DisplayName} | Version: {app.DisplayVersion} | Publisher: {app.Publisher} | Arch: {app.Architecture}");
        }
        sb.AppendLine();

        // Dsregcmd Raw Output
        sb.AppendLine("--- [ DSREGCMD RAW OUTPUT ] ---------------------------------------------");
        sb.AppendLine(report.Dsregcmd.RawOutput);
        sb.AppendLine("================================================================================");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    public static async Task ExportToHtmlAsync(SystemReport report, string filePath)
    {
        var html = GenerateHtmlReport(report);
        await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);
    }

    private static string GenerateHtmlReport(SystemReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\" />");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.AppendLine($"<title>XRAY ULTIMATE - System Report ({Escape(report.Overview.ComputerName)})</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(@"
:root {
    --bg-primary: #0f141c;
    --bg-card: #18202c;
    --bg-subtle: #212c3d;
    --text-primary: #f0f4f8;
    --text-muted: #94a3b8;
    --accent: #38bdf8;
    --accent-glow: rgba(56, 189, 248, 0.2);
    --border: #2d3b4e;
    --success: #34d399;
    --warning: #fbbf24;
}
* { box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }
body { background: var(--bg-primary); color: var(--text-primary); padding: 30px 20px; line-height: 1.5; }
.container { max-width: 1200px; margin: 0 auto; }
.header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border); padding-bottom: 20px; margin-bottom: 25px; flex-wrap: wrap; gap: 15px; }
.logo-title { display: flex; align-items: center; gap: 15px; }
.logo-badge { background: linear-gradient(135deg, #0284c7, #38bdf8); color: #fff; font-weight: 800; padding: 10px 18px; border-radius: 12px; font-size: 20px; letter-spacing: 1px; box-shadow: 0 4px 15px var(--accent-glow); }
.header-meta { font-size: 13px; color: var(--text-muted); text-align: right; }
.stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 15px; margin-bottom: 30px; }
.stat-card { background: var(--bg-card); border: 1px solid var(--border); border-radius: 12px; padding: 18px; display: flex; flex-direction: column; }
.stat-label { font-size: 12px; text-transform: uppercase; color: var(--text-muted); font-weight: 600; letter-spacing: 0.5px; }
.stat-value { font-size: 18px; font-weight: 700; color: #fff; margin-top: 6px; }
.stat-sub { font-size: 12px; color: var(--accent); margin-top: 4px; }
.section { background: var(--bg-card); border: 1px solid var(--border); border-radius: 12px; margin-bottom: 25px; overflow: hidden; }
.section-header { background: var(--bg-subtle); padding: 14px 20px; font-weight: 700; font-size: 16px; display: flex; align-items: center; gap: 10px; border-bottom: 1px solid var(--border); }
.prop-table { width: 100%; border-collapse: collapse; }
.prop-table tr { border-bottom: 1px solid rgba(255,255,255,0.05); }
.prop-table tr:hover { background: rgba(255,255,255,0.02); }
.prop-category { background: rgba(56, 189, 248, 0.05); font-weight: 600; font-size: 13px; color: var(--accent); padding: 8px 20px; text-transform: uppercase; letter-spacing: 0.5px; }
.prop-name { width: 35%; padding: 10px 20px; font-size: 14px; color: var(--text-muted); }
.prop-val { padding: 10px 20px; font-size: 14px; color: var(--text-primary); font-weight: 500; word-break: break-all; }
.badge { display: inline-block; padding: 3px 8px; border-radius: 6px; font-size: 12px; font-weight: 600; }
.badge-active { background: rgba(52, 211, 153, 0.15); color: var(--success); }
.badge-inactive { background: rgba(148, 163, 184, 0.15); color: var(--text-muted); }
pre.raw-box { background: #0b0e14; padding: 15px; border-radius: 8px; font-family: 'Consolas', 'Courier New', monospace; font-size: 12px; color: #cbd5e1; overflow-x: auto; white-space: pre-wrap; line-height: 1.4; border: 1px solid var(--border); margin: 15px; }
@media print {
    body { background: #fff; color: #000; padding: 0; }
    .section, .stat-card { border: 1px solid #ccc; background: #fff; color: #000; break-inside: avoid; }
    .section-header { background: #f0f0f0; color: #000; }
    .prop-name { color: #555; }
    .prop-val { color: #000; }
}
");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"container\">");

        // Header
        sb.AppendLine("<div class=\"header\">");
        sb.AppendLine("  <div class=\"logo-title\">");
        sb.AppendLine("    <div class=\"logo-badge\">XRAY ULTIMATE</div>");
        sb.AppendLine("    <div>");
        sb.AppendLine($"      <h2>{Escape(report.Overview.ComputerName)}</h2>");
        sb.AppendLine($"      <p style=\"color: var(--text-muted); font-size: 14px;\">{Escape(report.Overview.OsName)} • {Escape(report.Overview.OsVersion)}</p>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class=\"header-meta\">");
        sb.AppendLine($"    <div><strong>Report Generated:</strong> {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}</div>");
        sb.AppendLine($"    <div><strong>Engine:</strong> {Escape(report.GeneratorVersion)}</div>");
        sb.AppendLine($"    <div><strong>Uptime:</strong> {Escape(report.Overview.SystemUptime)}</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");

        // Stats Cards
        sb.AppendLine("<div class=\"stats-grid\">");
        sb.AppendLine($"  <div class=\"stat-card\"><span class=\"stat-label\">Processor</span><span class=\"stat-value\">{Escape(report.Cpu.ProcessorName)}</span><span class=\"stat-sub\">{report.Cpu.CoreCount} Cores / {report.Cpu.LogicalProcessorCount} Threads</span></div>");
        sb.AppendLine($"  <div class=\"stat-card\"><span class=\"stat-label\">Memory (RAM)</span><span class=\"stat-value\">{Escape(report.Memory.Properties.Find(p => p.Property == "Total Installed RAM")?.Value ?? "")}</span><span class=\"stat-sub\">{report.Memory.MemoryLoadPercent:F0}% Utilized</span></div>");
        sb.AppendLine($"  <div class=\"stat-card\"><span class=\"stat-label\">Graphics</span><span class=\"stat-value\">{(report.Gpu.Adapters.Count > 0 ? Escape(report.Gpu.Adapters[0].Name) : "Integrated")}</span><span class=\"stat-sub\">{(report.Gpu.Adapters.Count > 0 ? report.Gpu.Adapters[0].PropertiesOrMode() : "")}</span></div>");
        sb.AppendLine($"  <div class=\"stat-card\"><span class=\"stat-label\">Cloud Identity</span><span class=\"stat-value\">{(report.Dsregcmd.AzureAdJoined ? "Azure AD Joined" : "Domain/Workgroup")}</span><span class=\"stat-sub\">{Escape(report.Dsregcmd.OverallStatus)}</span></div>");
        sb.AppendLine("</div>");

        void RenderSection(string title, System.Collections.Generic.List<PropertyItem> items)
        {
            if (items.Count == 0) return;
            sb.AppendLine("<div class=\"section\">");
            sb.AppendLine($"  <div class=\"section-header\">{title}</div>");
            sb.AppendLine("  <table class=\"prop-table\">");
            string currentCategory = "";
            foreach (var item in items)
            {
                if (item.Category != currentCategory)
                {
                    currentCategory = item.Category;
                    sb.AppendLine($"    <tr><td colspan=\"2\" class=\"prop-category\">{Escape(currentCategory)}</td></tr>");
                }
                sb.AppendLine($"    <tr><td class=\"prop-name\">{Escape(item.Property)}</td><td class=\"prop-val\">{Escape(item.DisplayValue)}</td></tr>");
            }
            sb.AppendLine("  </table>");
            sb.AppendLine("</div>");
        }

        RenderSection("System Overview", report.Overview.Properties);
        RenderSection("Processor (CPU)", report.Cpu.Properties);
        RenderSection("Memory (RAM)", report.Memory.Properties);
        RenderSection("Motherboard & BIOS", report.Motherboard.Properties);
        RenderSection("Display & Graphics", report.Gpu.Properties);
        RenderSection("Storage & Disks", report.Storage.Properties);
        RenderSection("Network & Wi-Fi", report.Network.Properties);
        RenderSection("Battery & Power Subsystem", report.Battery.Properties);
        RenderSection("Security & TPM", report.Security.Properties);
        RenderSection("Entra ID / dsregcmd Diagnostics", report.Dsregcmd.ParsedProperties);
        RenderSection("Filter Drivers & Filesystem Minifilters (fltmc)", report.FilterDrivers.Properties);
        RenderSection("ACPI & DSDT Firmware Tables", report.Acpi.Properties);
        RenderSection("Device Manager Overview", report.DeviceManager.Properties);

        // Benchmark Section
        if (report.Benchmark != null && report.Benchmark.IsCompleted)
        {
            sb.AppendLine("<div class=\"section\">");
            sb.AppendLine("  <div class=\"section-header\">Hardware Benchmark Results</div>");
            sb.AppendLine("  <table class=\"prop-table\">");
            sb.AppendLine($"    <tr><td class=\"prop-name\">Single-Core Score</td><td class=\"prop-val\"><strong>{report.Benchmark.CpuSingleThreadScore:N0}</strong> pts</td></tr>");
            sb.AppendLine($"    <tr><td class=\"prop-name\">Multi-Core Score</td><td class=\"prop-val\"><strong>{report.Benchmark.CpuMultiThreadScore:N0}</strong> pts ({report.Benchmark.MultiThreadEfficiencyRatio:F2}x efficiency)</td></tr>");
            sb.AppendLine($"    <tr><td class=\"prop-name\">Memory Read Bandwidth</td><td class=\"prop-val\">{report.Benchmark.MemoryReadBandwidthMBs:F1} MB/s</td></tr>");
            sb.AppendLine($"    <tr><td class=\"prop-name\">Memory Write Bandwidth</td><td class=\"prop-val\">{report.Benchmark.MemoryWriteBandwidthMBs:F1} MB/s</td></tr>");
            sb.AppendLine($"    <tr><td class=\"prop-name\">Memory Copy Bandwidth</td><td class=\"prop-val\">{report.Benchmark.MemoryCopyBandwidthMBs:F1} MB/s</td></tr>");
            sb.AppendLine($"    <tr><td class=\"prop-name\">Memory Latency</td><td class=\"prop-val\">{report.Benchmark.MemoryLatencyNs:F1} ns</td></tr>");
            sb.AppendLine("  </table>");
            sb.AppendLine("</div>");
        }

        // Installed Software Section
        if (report.Software.Applications.Count > 0)
        {
            sb.AppendLine("<div class=\"section\">");
            sb.AppendLine($"  <div class=\"section-header\">Installed Applications ({report.Software.Applications.Count})</div>");
            sb.AppendLine("  <table class=\"prop-table\">");
            sb.AppendLine("    <tr><td class=\"prop-category\">Application Name</td><td class=\"prop-category\">Version / Publisher</td></tr>");
            foreach (var app in report.Software.Applications)
            {
                sb.AppendLine($"    <tr><td class=\"prop-name\">{Escape(app.DisplayName)}</td><td class=\"prop-val\">{Escape(app.DisplayVersion)} <span style=\"color:var(--text-muted); font-size:12px;\">({Escape(app.Publisher)})</span></td></tr>");
            }
            sb.AppendLine("  </table>");
            sb.AppendLine("</div>");
        }

        // dsregcmd Raw Section
        if (!string.IsNullOrEmpty(report.Dsregcmd.RawOutput))
        {
            sb.AppendLine("<div class=\"section\">");
            sb.AppendLine("  <div class=\"section-header\">Raw dsregcmd /status Diagnostic Output</div>");
            sb.AppendLine($"  <pre class=\"raw-box\">{Escape(report.Dsregcmd.RawOutput)}</pre>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>"); // container
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string Escape(string? input) => System.Net.WebUtility.HtmlEncode(input ?? "");

    private static string PropertiesOrMode(this GraphicsCardInfo gpu)
    {
        if (gpu.CurrentHorizontalResolution > 0)
            return $"{gpu.CurrentHorizontalResolution}x{gpu.CurrentVerticalResolution} @ {gpu.CurrentRefreshRate}Hz";
        return gpu.Status;
    }
}
