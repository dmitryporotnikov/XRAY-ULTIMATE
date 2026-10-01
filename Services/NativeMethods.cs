using System;
using System.Runtime.InteropServices;

namespace XRAY_ULTIMATE.Services;

public static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    public enum FIRMWARE_TYPE
    {
        FirmwareTypeUnknown = 0,
        FirmwareTypeBios = 1,
        FirmwareTypeUefi = 2,
        FirmwareTypeMax = 3
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetFirmwareType(out FIRMWARE_TYPE FirmwareType);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetSystemFirmwareTable(uint FirmwareTableProviderSignature, uint FirmwareTableID, IntPtr pFirmwareTableBuffer, uint BufferSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint EnumSystemFirmwareTables(uint FirmwareTableProviderSignature, IntPtr pFirmwareTableEnumBuffer, uint BufferSize);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsProcessorFeaturePresent(uint ProcessorFeature);

    // Common Processor Feature Constants
    public const uint PF_FLOATING_POINT_PRECISION_ERRATA = 0;
    public const uint PF_FLOATING_POINT_EMULATED = 1;
    public const uint PF_COMPARE_EXCHANGE_DOUBLE = 2;
    public const uint PF_MMX_INSTRUCTIONS_AVAILABLE = 3;
    public const uint PF_PPC_MOVEMEM_64BIT_OK = 4;
    public const uint PF_ALPHA_BYTE_INSTRUCTIONS = 5;
    public const uint PF_XMMI_INSTRUCTIONS_AVAILABLE = 6;      // SSE
    public const uint PF_3DNOW_INSTRUCTIONS_AVAILABLE = 7;
    public const uint PF_RDTSC_INSTRUCTION_AVAILABLE = 8;
    public const uint PF_PAE_ENABLED = 9;
    public const uint PF_XMMI64_INSTRUCTIONS_AVAILABLE = 10;   // SSE2
    public const uint PF_SSE_DAZ_MODE_AVAILABLE = 11;
    public const uint PF_NX_ENABLED = 12;
    public const uint PF_SSE3_INSTRUCTIONS_AVAILABLE = 13;
    public const uint PF_COMPARE_EXCHANGE128 = 14;
    public const uint PF_COMPARE64_EXCHANGE128 = 15;
    public const uint PF_CHANNELS_ENABLED = 16;
    public const uint PF_XSAVE_ENABLED = 17;
    public const uint PF_ARM_VFP_32_REGISTERS_AVAILABLE = 18;
    public const uint PF_ARM_NEON_INSTRUCTIONS_AVAILABLE = 19;
    public const uint PF_SECOND_LEVEL_ADDRESS_TRANSLATION = 20;
    public const uint PF_VIRT_FIRMWARE_ENABLED = 21;
    public const uint PF_RDWRFSGSBASE_AVAILABLE = 22;
    public const uint PF_FASTFAIL_AVAILABLE = 23;
    public const uint PF_ARM_DIVIDE_INSTRUCTION_AVAILABLE = 24;
    public const uint PF_ARM_64BIT_LOADSTORE_ATOMIC = 25;
    public const uint PF_ARM_EXTERNAL_CACHE_AVAILABLE = 26;
    public const uint PF_ARM_FMAC_INSTRUCTIONS_AVAILABLE = 27;
    public const uint PF_RDRAND_INSTRUCTION_AVAILABLE = 28;
    public const uint PF_ARM_V8_INSTRUCTIONS_AVAILABLE = 29;
    public const uint PF_ARM_V8_CRYPTO_INSTRUCTIONS_AVAILABLE = 30;
    public const uint PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE = 31;
    public const uint PF_RDTSCP_INSTRUCTION_AVAILABLE = 32;
    public const uint PF_AVX_INSTRUCTIONS_AVAILABLE = 33;
    public const uint PF_AVX2_INSTRUCTIONS_AVAILABLE = 34;
    public const uint PF_AVX512F_INSTRUCTIONS_AVAILABLE = 35;
    public const uint PF_ARM_V81_ATOMIC_INSTRUCTIONS_AVAILABLE = 36;
    public const uint PF_SSSE3_INSTRUCTIONS_AVAILABLE = 37;
    public const uint PF_SSE4_1_INSTRUCTIONS_AVAILABLE = 38;
    public const uint PF_SSE4_2_INSTRUCTIONS_AVAILABLE = 39;
}
