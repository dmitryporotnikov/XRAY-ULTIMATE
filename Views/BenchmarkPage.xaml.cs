using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Models;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class BenchmarkPage : Page
{
    public BenchmarkPage()
    {
        this.InitializeComponent();
        this.Loaded += BenchmarkPage_Loaded;
    }

    private void BenchmarkPage_Loaded(object sender, RoutedEventArgs e)
    {
        var cpu = SystemDiagnosticsService.Instance.CurrentReport.Cpu;
        TxtBenchmarkCpu.Text = $"Processor: {cpu.ProcessorName} ({cpu.LogicalProcessorCount} threads)";

        var bm = SystemDiagnosticsService.Instance.CurrentReport.Benchmark;
        if (bm != null && bm.IsCompleted)
        {
            DisplayResults(bm);
        }
    }

    private async void BtnRunBenchmark_Click(object sender, RoutedEventArgs e)
    {
        BtnRunBenchmark.IsEnabled = false;
        PrgRing.Visibility = Visibility.Visible;
        IconRun.Visibility = Visibility.Collapsed;
        TxtBtnText.Text = "Benchmarking...";

        var progress = new Progress<string>(msg =>
        {
            TxtStatus.Text = msg;
        });

        try
        {
            var result = await BenchmarkService.RunBenchmarkAsync(progress);
            SystemDiagnosticsService.Instance.CurrentReport.Benchmark = result;
            DisplayResults(result);
            TxtStatus.Text = $"Completed in {result.DurationSeconds:F2} seconds.";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Benchmark error: {ex.Message}";
        }
        finally
        {
            BtnRunBenchmark.IsEnabled = true;
            PrgRing.Visibility = Visibility.Collapsed;
            IconRun.Visibility = Visibility.Visible;
            TxtBtnText.Text = "Run Benchmark Again";
        }
    }

    private void DisplayResults(BenchmarkResult bm)
    {
        TxtSingleScore.Text = $"{bm.CpuSingleThreadScore:N0} pts";
        TxtMultiScore.Text = $"{bm.CpuMultiThreadScore:N0} pts";
        TxtScaling.Text = $"Parallel execution scaling: {bm.MultiThreadEfficiencyRatio:F2}x across {bm.CoresUsed} threads";

        TxtMemRead.Text = $"{bm.MemoryReadBandwidthMBs:F1} MB/s";
        TxtMemWrite.Text = $"{bm.MemoryWriteBandwidthMBs:F1} MB/s";
        TxtMemCopy.Text = $"{bm.MemoryCopyBandwidthMBs:F1} MB/s";
        TxtMemLatency.Text = $"{bm.MemoryLatencyNs:F1} ns";
    }
}
