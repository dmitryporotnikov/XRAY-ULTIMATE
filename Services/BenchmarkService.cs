using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class BenchmarkService
{
    public static async Task<BenchmarkResult> RunBenchmarkAsync(IProgress<string>? progress = null)
    {
        return await Task.Run(() =>
        {
            var result = new BenchmarkResult
            {
                RunTime = DateTime.Now,
                CoresUsed = Environment.ProcessorCount
            };

            var swTotal = Stopwatch.StartNew();

            // 1. Single-Thread CPU Benchmark
            progress?.Report("Running Single-Core CPU Benchmark (Integer & Crypto)...");
            long singleThreadScore = RunSingleThreadTest();
            result.CpuSingleThreadScore = singleThreadScore;

            // 2. Multi-Thread CPU Benchmark
            progress?.Report($"Running Multi-Core CPU Benchmark across {Environment.ProcessorCount} threads...");
            long multiThreadScore = RunMultiThreadTest();
            result.CpuMultiThreadScore = multiThreadScore;

            // 3. Memory Write Benchmark
            progress?.Report("Testing Memory Write Bandwidth...");
            double writeMBs = RunMemoryWriteTest();
            result.MemoryWriteBandwidthMBs = writeMBs;

            // 4. Memory Read Benchmark
            progress?.Report("Testing Memory Read Bandwidth...");
            double readMBs = RunMemoryReadTest();
            result.MemoryReadBandwidthMBs = readMBs;

            // 5. Memory Copy Benchmark
            progress?.Report("Testing Memory Copy Bandwidth...");
            double copyMBs = RunMemoryCopyTest();
            result.MemoryCopyBandwidthMBs = copyMBs;

            // 6. Memory Latency Benchmark
            progress?.Report("Testing Memory Latency...");
            double latencyNs = RunMemoryLatencyTest();
            result.MemoryLatencyNs = latencyNs;

            swTotal.Stop();
            result.DurationSeconds = swTotal.Elapsed.TotalSeconds;
            result.IsCompleted = true;

            progress?.Report("Benchmark completed successfully.");
            return result;
        });
    }

    private static long RunSingleThreadTest()
    {
        var sw = Stopwatch.StartNew();
        long ops = 0;
        using var sha = SHA256.Create();
        byte[] buffer = new byte[1024];
        new Random(42).NextBytes(buffer);

        while (sw.ElapsedMilliseconds < 1200)
        {
            for (int i = 0; i < 500; i++)
            {
                buffer[0] = (byte)(i & 0xFF);
                var hash = sha.ComputeHash(buffer);
                ops += hash[0];
            }
        }
        sw.Stop();
        return (long)(ops / (sw.Elapsed.TotalSeconds * 100));
    }

    private static long RunMultiThreadTest()
    {
        int threadCount = Math.Max(1, Environment.ProcessorCount);
        long totalOps = 0;
        var sw = Stopwatch.StartNew();

        Parallel.For(0, threadCount, _ =>
        {
            long localOps = 0;
            using var sha = SHA256.Create();
            byte[] buffer = new byte[1024];
            new Random(Thread.CurrentThread.ManagedThreadId).NextBytes(buffer);

            var localSw = Stopwatch.StartNew();
            while (localSw.ElapsedMilliseconds < 1200)
            {
                for (int i = 0; i < 500; i++)
                {
                    buffer[0] = (byte)(i & 0xFF);
                    var hash = sha.ComputeHash(buffer);
                    localOps += hash[0];
                }
            }
            Interlocked.Add(ref totalOps, localOps);
        });

        sw.Stop();
        return (long)(totalOps / (sw.Elapsed.TotalSeconds * 100));
    }

    private static double RunMemoryWriteTest()
    {
        const int size = 64 * 1024 * 1024; // 64 MB
        byte[] buffer = new byte[size];
        int iterations = 10;

        var sw = Stopwatch.StartNew();
        for (int it = 0; it < iterations; it++)
        {
            byte val = (byte)it;
            Array.Fill(buffer, val);
        }
        sw.Stop();

        double totalBytes = (double)size * iterations;
        double seconds = sw.Elapsed.TotalSeconds;
        return (totalBytes / (1024.0 * 1024.0)) / Math.Max(seconds, 0.001);
    }

    private static double RunMemoryReadTest()
    {
        const int size = 64 * 1024 * 1024; // 64 MB
        byte[] buffer = new byte[size];
        new Random(123).NextBytes(buffer);
        int iterations = 10;
        long sum = 0;

        var sw = Stopwatch.StartNew();
        for (int it = 0; it < iterations; it++)
        {
            for (int i = 0; i < size; i += 64)
            {
                sum += buffer[i];
            }
        }
        sw.Stop();

        double totalBytes = (double)size * iterations;
        double seconds = sw.Elapsed.TotalSeconds;
        // prevent optimizer from dead-code eliminating
        if (sum == 42) GC.KeepAlive(buffer);

        return (totalBytes / (1024.0 * 1024.0)) / Math.Max(seconds, 0.001);
    }

    private static double RunMemoryCopyTest()
    {
        const int size = 64 * 1024 * 1024; // 64 MB
        byte[] src = new byte[size];
        byte[] dst = new byte[size];
        new Random(456).NextBytes(src);
        int iterations = 8;

        var sw = Stopwatch.StartNew();
        for (int it = 0; it < iterations; it++)
        {
            Buffer.BlockCopy(src, 0, dst, 0, size);
        }
        sw.Stop();

        double totalBytes = (double)size * iterations;
        double seconds = sw.Elapsed.TotalSeconds;
        return (totalBytes / (1024.0 * 1024.0)) / Math.Max(seconds, 0.001);
    }

    private static double RunMemoryLatencyTest()
    {
        // Pointer chasing array test
        const int count = 2 * 1024 * 1024;
        int[] indices = new int[count];
        for (int i = 0; i < count; i++) indices[i] = i;

        // Shuffle
        var rand = new Random(789);
        for (int i = count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            int tmp = indices[i];
            indices[i] = indices[j];
            indices[j] = tmp;
        }

        int curr = 0;
        const int steps = 5_000_000;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < steps; i++)
        {
            curr = indices[curr];
        }
        sw.Stop();

        GC.KeepAlive(curr);
        double totalNs = sw.Elapsed.TotalMilliseconds * 1_000_000.0;
        return Math.Round(totalNs / steps, 1);
    }
}
