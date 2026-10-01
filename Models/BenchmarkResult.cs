using System;

namespace XRAY_ULTIMATE.Models;

public class BenchmarkResult
{
    public DateTime RunTime { get; set; } = DateTime.Now;
    public string CpuModel { get; set; } = string.Empty;
    public int CoresUsed { get; set; }

    public long CpuSingleThreadScore { get; set; }
    public long CpuMultiThreadScore { get; set; }
    public double MultiThreadEfficiencyRatio => CpuSingleThreadScore > 0 ? (double)CpuMultiThreadScore / CpuSingleThreadScore : 1.0;

    public double MemoryReadBandwidthMBs { get; set; }
    public double MemoryWriteBandwidthMBs { get; set; }
    public double MemoryCopyBandwidthMBs { get; set; }
    public double MemoryLatencyNs { get; set; }

    public double DurationSeconds { get; set; }
    public bool IsCompleted { get; set; }
}
