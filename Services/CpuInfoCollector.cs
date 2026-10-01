using System;
using System.Collections.Generic;
using Microsoft.Win32;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class CpuInfoCollector
{
    public static CpuInfo Collect()
    {
        var info = new CpuInfo();

        try
        {
            var procData = WmiService.QueryFirst("SELECT * FROM Win32_Processor");
            if (procData != null)
            {
                info.ProcessorName = procData.GetString("Name");
                info.Manufacturer = procData.GetString("Manufacturer");
                info.CoreCount = procData.GetInt("NumberOfCores", Environment.ProcessorCount);
                info.LogicalProcessorCount = procData.GetInt("NumberOfLogicalProcessors", Environment.ProcessorCount);
                info.MaxClockSpeedMHz = procData.GetUint("MaxClockSpeed");
                info.BaseClockSpeedMHz = procData.GetUint("CurrentClockSpeed", info.MaxClockSpeedMHz);
                info.SocketDesignation = procData.GetString("SocketDesignation");
                info.Stepping = procData.GetString("Stepping");
                info.Revision = procData.GetString("Revision");
                info.L2CacheKB = procData.GetUint("L2CacheSize");
                info.L3CacheKB = procData.GetUint("L3CacheSize");

                var volt = procData.GetUint("CurrentVoltage");
                if (volt > 0)
                {
                    info.CurrentVoltage = $"{volt / 10.0:F2} V";
                }

                ushort archCode = (ushort)procData.GetUint("Architecture");
                info.Architecture = archCode switch
                {
                    0 => "x86 (32-bit)",
                    1 => "MIPS",
                    2 => "Alpha",
                    3 => "PowerPC",
                    5 => "ARM",
                    6 => "Itanium",
                    9 => "x64 (AMD64 / Intel 64)",
                    12 => "ARM64",
                    _ => Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"
                };
            }
        }
        catch { }

        // Registry fallback if WMI is incomplete
        if (string.IsNullOrEmpty(info.ProcessorName))
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key != null)
                {
                    info.ProcessorName = key.GetValue("ProcessorNameString") as string ?? "Generic Processor";
                    info.Manufacturer = key.GetValue("VendorIdentifier") as string ?? "";
                    if (info.MaxClockSpeedMHz == 0 && key.GetValue("~MHz") is int mhz)
                    {
                        info.MaxClockSpeedMHz = (uint)mhz;
                        info.BaseClockSpeedMHz = (uint)mhz;
                    }
                }
            }
            catch { }
        }

        if (info.CoreCount == 0) info.CoreCount = Environment.ProcessorCount;
        if (info.LogicalProcessorCount == 0) info.LogicalProcessorCount = Environment.ProcessorCount;
        if (string.IsNullOrEmpty(info.Architecture))
            info.Architecture = Environment.Is64BitOperatingSystem ? "x64 (64-bit)" : "x86 (32-bit)";

        // Cache details from Win32_CacheMemory
        try
        {
            var caches = WmiService.Query("SELECT Level, InstalledSize, Purpose FROM Win32_CacheMemory");
            foreach (var c in caches)
            {
                uint level = c.GetUint("Level");
                uint size = c.GetUint("InstalledSize");
                if (level == 3) info.L1DataCacheKB = Math.Max(info.L1DataCacheKB, size);
                else if (level == 4) info.L2CacheKB = Math.Max(info.L2CacheKB, size);
                else if (level == 5) info.L3CacheKB = Math.Max(info.L3CacheKB, size);
            }
        }
        catch { }

        // Instruction Sets & Features
        CheckFeatures(info);

        // Build Key-Value Properties
        info.Properties.Add(new PropertyItem("General", "Processor Name", info.ProcessorName));
        info.Properties.Add(new PropertyItem("General", "Manufacturer", info.Manufacturer));
        info.Properties.Add(new PropertyItem("General", "Architecture", info.Architecture));
        info.Properties.Add(new PropertyItem("Topology", "Physical Cores", info.CoreCount.ToString()));
        info.Properties.Add(new PropertyItem("Topology", "Logical Processors (Threads)", info.LogicalProcessorCount.ToString()));
        info.Properties.Add(new PropertyItem("Clocks", "Base / Current Clock", UnitFormatter.FormatHertz(info.BaseClockSpeedMHz)));
        info.Properties.Add(new PropertyItem("Clocks", "Max Clock Frequency", UnitFormatter.FormatHertz(info.MaxClockSpeedMHz)));
        if (!string.IsNullOrEmpty(info.SocketDesignation))
            info.Properties.Add(new PropertyItem("Socket", "Socket Designation", info.SocketDesignation));
        if (!string.IsNullOrEmpty(info.Stepping))
            info.Properties.Add(new PropertyItem("Stepping", "Stepping", info.Stepping));
        if (!string.IsNullOrEmpty(info.CurrentVoltage))
            info.Properties.Add(new PropertyItem("Clocks", "Voltage", info.CurrentVoltage));

        if (info.L1DataCacheKB > 0)
            info.Properties.Add(new PropertyItem("Cache", "L1 Cache", $"{info.L1DataCacheKB} KB"));
        if (info.L2CacheKB > 0)
            info.Properties.Add(new PropertyItem("Cache", "L2 Cache", $"{info.L2CacheKB} KB ({info.L2CacheKB / 1024.0:F1} MB)"));
        if (info.L3CacheKB > 0)
            info.Properties.Add(new PropertyItem("Cache", "L3 Cache", $"{info.L3CacheKB} KB ({info.L3CacheKB / 1024.0:F1} MB)"));

        // Virtualization Status
        string virtStatus = info.VirtualizationFirmwareEnabled
            ? "Enabled in Firmware"
            : "Disabled / Not Supported";

        try
        {
            var cs = WmiService.QueryFirst("SELECT HypervisorPresent FROM Win32_ComputerSystem");
            if (cs != null && cs.GetBool("HypervisorPresent"))
            {
                info.VirtualizationFirmwareEnabled = true;
                virtStatus = "Enabled in Firmware (Hyper-V / WSL Active)";
            }
        }
        catch { }

        info.Properties.Add(new PropertyItem("Virtualization", "Hardware Virtualization (VT-x / AMD-V)", virtStatus));

        return info;
    }

    private static void CheckFeatures(CpuInfo info)
    {
        bool hypervisorActive = false;
        try
        {
            var cs = WmiService.QueryFirst("SELECT HypervisorPresent FROM Win32_ComputerSystem");
            if (cs != null && cs.GetBool("HypervisorPresent")) hypervisorActive = true;
        }
        catch { }

        // Virtualization enabled either directly or via active hypervisor
        bool virtDirect = NativeMethods.IsProcessorFeaturePresent(NativeMethods.PF_VIRT_FIRMWARE_ENABLED);
        bool slat = NativeMethods.IsProcessorFeaturePresent(NativeMethods.PF_SECOND_LEVEL_ADDRESS_TRANSLATION);
        info.VirtualizationFirmwareEnabled = virtDirect || hypervisorActive || slat;

        bool isArm = info.Architecture.Contains("ARM", StringComparison.OrdinalIgnoreCase) ||
                     System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ||
                     System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm;

        if (isArm)
        {
            // ARM / ARM64 Instruction Sets Only
            var armFeatures = new (string name, string desc, uint id)[]
            {
                ("ARM Neon", "ARM Advanced SIMD Extensions", NativeMethods.PF_ARM_NEON_INSTRUCTIONS_AVAILABLE),
                ("ARMv8 Crypto", "ARM Cryptography Extensions (AES/SHA)", NativeMethods.PF_ARM_V8_CRYPTO_INSTRUCTIONS_AVAILABLE),
                ("ARMv8 CRC32", "ARM CRC32 Instructions", NativeMethods.PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE),
                ("ARMv8.1 Atomics", "ARMv8.1 Atomic Instructions", NativeMethods.PF_ARM_V81_ATOMIC_INSTRUCTIONS_AVAILABLE),
                ("ARM VFP-32", "ARM Vector Floating-Point 32 Registers", NativeMethods.PF_ARM_VFP_32_REGISTERS_AVAILABLE),
                ("ARM Divide", "ARM Hardware Integer Division", NativeMethods.PF_ARM_DIVIDE_INSTRUCTION_AVAILABLE),
                ("ARM 64-bit Atomic", "ARM 64-bit Load/Store Atomic", NativeMethods.PF_ARM_64BIT_LOADSTORE_ATOMIC),
                ("ARM External Cache", "ARM External Cache Architecture", NativeMethods.PF_ARM_EXTERNAL_CACHE_AVAILABLE),
                ("ARM FMAC", "ARM Fused Multiply-Accumulate", NativeMethods.PF_ARM_FMAC_INSTRUCTIONS_AVAILABLE),
                ("ARM Virtualization", "ARM EL2 Hardware Virtualization", NativeMethods.PF_VIRT_FIRMWARE_ENABLED)
            };

            foreach (var (name, desc, id) in armFeatures)
            {
                try
                {
                    bool supported = NativeMethods.IsProcessorFeaturePresent(id);
                    if (name.Contains("Virtualization") && (hypervisorActive || slat)) supported = true;
                    info.SupportedFeatures.Add(new CpuFeatureItem(name, desc, supported));
                }
                catch { }
            }
        }
        else
        {
            // Intel / AMD x86 & x64 Instruction Sets Only
            var x86Features = new (string name, string desc, uint id, Func<bool>? extraCheck)[]
            {
                ("MMX", "MultiMedia Extensions", NativeMethods.PF_MMX_INSTRUCTIONS_AVAILABLE, null),
                ("SSE", "Streaming SIMD Extensions", NativeMethods.PF_XMMI_INSTRUCTIONS_AVAILABLE, () => System.Runtime.Intrinsics.X86.Sse.IsSupported),
                ("SSE2", "Streaming SIMD Extensions 2", NativeMethods.PF_XMMI64_INSTRUCTIONS_AVAILABLE, () => System.Runtime.Intrinsics.X86.Sse2.IsSupported),
                ("SSE3", "Streaming SIMD Extensions 3", NativeMethods.PF_SSE3_INSTRUCTIONS_AVAILABLE, () => System.Runtime.Intrinsics.X86.Sse3.IsSupported),
                ("SSSE3", "Supplemental Streaming SIMD Extensions 3", NativeMethods.PF_SSSE3_INSTRUCTIONS_AVAILABLE, () => System.Runtime.Intrinsics.X86.Ssse3.IsSupported),
                ("SSE4.1", "Streaming SIMD Extensions 4.1", NativeMethods.PF_SSE4_1_INSTRUCTIONS_AVAILABLE, () => System.Runtime.Intrinsics.X86.Sse41.IsSupported),
                ("SSE4.2", "Streaming SIMD Extensions 4.2", NativeMethods.PF_SSE4_2_INSTRUCTIONS_AVAILABLE, () => System.Runtime.Intrinsics.X86.Sse42.IsSupported),
                ("AVX", "Advanced Vector Extensions", NativeMethods.PF_AVX_INSTRUCTIONS_AVAILABLE, () => System.Runtime.Intrinsics.X86.Avx.IsSupported),
                ("AVX2", "Advanced Vector Extensions 2", NativeMethods.PF_AVX2_INSTRUCTIONS_AVAILABLE, () => System.Runtime.Intrinsics.X86.Avx2.IsSupported),
                ("AVX-512", "AVX-512 Foundation Instructions", NativeMethods.PF_AVX512F_INSTRUCTIONS_AVAILABLE, () => System.Runtime.Intrinsics.X86.Avx512F.IsSupported),
                ("FMA3", "Fused Multiply-Add Extensions", 0, () => System.Runtime.Intrinsics.X86.Fma.IsSupported),
                ("AES-NI", "Advanced Encryption Standard Instructions", 0, () => System.Runtime.Intrinsics.X86.Aes.IsSupported),
                ("VT-x / AMD-V", "Hardware Virtualization (VMX / SVM)", NativeMethods.PF_VIRT_FIRMWARE_ENABLED, () => hypervisorActive || slat),
                ("SLAT / EPT", "Second Level Address Translation", NativeMethods.PF_SECOND_LEVEL_ADDRESS_TRANSLATION, () => hypervisorActive),
                ("NX / XD", "No-Execute / Execute-Disable Bit", NativeMethods.PF_NX_ENABLED, null),
                ("RDTSC", "Read Time-Stamp Counter", NativeMethods.PF_RDTSC_INSTRUCTION_AVAILABLE, null),
                ("RDTSCP", "Serialized Read Time-Stamp Counter", NativeMethods.PF_RDTSCP_INSTRUCTION_AVAILABLE, null),
                ("RDRAND", "On-chip Hardware Random Number Generator", NativeMethods.PF_RDRAND_INSTRUCTION_AVAILABLE, null),
                ("XSAVE", "Processor Extended States Save/Restore", NativeMethods.PF_XSAVE_ENABLED, null)
            };

            foreach (var (name, desc, id, extraCheck) in x86Features)
            {
                try
                {
                    bool supported = false;
                    if (id != 0) supported = NativeMethods.IsProcessorFeaturePresent(id);
                    if (!supported && extraCheck != null)
                    {
                        supported = extraCheck();
                    }
                    if (name.Contains("VT-x") && (hypervisorActive || slat))
                    {
                        supported = true;
                    }

                    info.SupportedFeatures.Add(new CpuFeatureItem(name, desc, supported));
                }
                catch { }
            }
        }
    }
}
