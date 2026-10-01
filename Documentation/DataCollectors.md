# Hardware & System Data Collectors Specification

This document details the telemetry extraction mechanisms, APIs, WMI namespaces, and fallback architectures implemented in XRAY ULTIMATE.

---

## 1. Processor (CPU) Collector (`CpuInfoCollector.cs`)

### Microarchitecture & Instruction Set Extension Probes
- **Hardware Topology**: Queries `Win32_Processor` for Socket, Core Count, Logical Processor Count, Base Clock, Max Clock, and Voltage.
- **Instruction Set Filtering**: Microarchitectural features are split strictly by platform architecture to prevent reporting incompatible features:
  - **x86/x64 Systems**: AVX-512, AVX2, AVX, SSE4.2, SSE4.1, SSSE3, SSE3, SSE2, SSE, MMX, AES-NI, FMA3, SHA-NI, VMX/VT-x, SVM/AMD-V.
  - **ARM64 Systems**: NEON, FP16, AES, PMULL, SHA1, SHA256, CRC32, Atomics (LSE), Virtualization.
- **Hyper-V & Hypervisor Aware Virtualization Detection**:
  - Under active hypervisors (WSL2, Hyper-V, Windows Sandbox), `IsProcessorFeaturePresent(PF_VIRT_FIRMWARE_ENABLED)` returns `false` because the hypervisor intercepts CPUID level 1 virtualization flags.
  - The collector checks `Win32_ComputerSystem.HypervisorPresent` and `PF_SECOND_LEVEL_ADDRESS_TRANSLATION` (SLAT) to accurately report:
    `"Enabled (Active under Hyper-V / Virtual Machine Platform)"`.

---

## 2. Memory (RAM) Collector (`MemoryInfoCollector.cs`)

### SMBIOS Physical DIMM Slot Extraction
- **DIMM Physical Enumeration**: Queries `Win32_PhysicalMemory` to retrieve:
  - Bank and Device Locator (`DIMM 1`, `Slot 1`)
  - Form Factor (`SODIMM`, `DIMM`)
  - Memory Type (DDR5, DDR4, DDR3, LPDDR5)
  - Configured Speed and Rated Clock Speed in MHz
  - Module Manufacturer, Part Number, and Serial Number
- **Memory Utilization**: Queries `GlobalMemoryStatusEx` (`kernel32.dll`) to obtain precise byte values for Total Physical RAM, Available RAM, and Pagefile Commit.

---

## 3. Graphics & Displays Collector (`GpuInfoCollector.cs`)

### GPU & Display Enumeration
- **Video Controllers**: Queries `Win32_VideoController` for Active WDDM Driver Version, Driver Date, Installed Dedicated VRAM, and Horizontal/Vertical Resolution.
- **Hardware Telemetry (NVIDIA / Dedicated Accelerators)**:
  - Executes `nvidia-smi` in the background with JSON/CSV output to query live dedicated GPU temperatures, fan speed percentages, power draw (watts), and memory usage.
  - Seamless fallback to driver-reported WDDM thermal interfaces when `nvidia-smi` is not present.
- **Connected Displays**: Queries `Win32_DesktopMonitor` and EDID registry keys to resolve monitor names, manufacturers, and active refresh rates.

---

## 4. Storage & Disks Collector (`StorageInfoCollector.cs`)

### Physical Drive & Volume Mapping
- **Physical Disk Drives**: Queries `Win32_DiskDrive` and `MSFT_PhysicalDisk` (Storage Management API) to identify:
  - Bus Type (`NVMe`, `SATA`, `USB`, `SCSI`)
  - Media Type (`SSD`, `HDD`, `SCM`)
  - Operational Status, Firmware Revision, and Serial Number
- **Logical Volumes**: Queries `Win32_Volume` to calculate exact free/used space percentages, filesystem types (`NTFS`, `ReFS`, `FAT32`, `exFAT`), and drive letters.

---

## 5. Security & TPM Collector (`OsSecurityInfoCollector.cs`)

### Multi-Tier TPM 2.0 & Secure Boot Detection
Because Windows restricts TPM namespaces under certain UAC configurations, XRAY ULTIMATE implements a 3-tier fallback architecture:

```
┌────────────────────────────────────────────────────────┐
│                   TPM Query Engine                     │
├────────────────────────────────────────────────────────┤
│ Tier 1: PnP Security Devices                           │
│   • Win32_PnPEntity WHERE PNPClass = 'SecurityDevices' │
│   • Zero permission failure risk; instant detection    │
├────────────────────────────────────────────────────────┤
│ Tier 2: ACPI Firmware Table Manifest                   │
│   • Direct probe of kernel table 'TPM2' (0x54504D32)   │
│   • Validates physical silicon presence from UEFI      │
├────────────────────────────────────────────────────────┤
│ Tier 3: Authenticated WMI Interface                    │
│   • root\CIMV2\Security\MicrosoftTpm                   │
│   • Uses PacketPrivacy + Impersonation                 │
│   • Extracts SpecVersion, ManufacturerID, IsActivated  │
└────────────────────────────────────────────────────────┘
```

- **Secure Boot State**: Reads `HKLM\SYSTEM\CurrentControlSet\Control\SecureBoot\State`. Differentiates between `"Enabled in Firmware"`, `"Disabled in Firmware (UEFI Supported)"`, and `"Not Supported (Legacy BIOS)"`.
- **BitLocker Drive Encryption**: Queries `root\CIMV2\Security\BitLocker` (`Win32_EncryptableVolume`) to extract encryption method (XTS-AES 128/256) and protection status for all fixed volumes.
- **Antivirus Products**: Queries `root\SecurityCenter2` (`AntiVirusProduct`) to report active endpoint protection engines, real-time scanning status, and definition update currency.

---

## 6. Device Manager Collector (`DeviceManagerCollector.cs`)

- Enumerates all physical and virtual devices via `Win32_PnPEntity`.
- Automatically maps Windows Setup Class GUIDs and class names (`Display`, `Net`, `DiskDrive`, `Ports`, `USB`, `Bluetooth`, `AudioEndpoint`, `System`) to 29 clean, human-readable groups.
- Resolves device problem error codes (e.g., Code 22 = Disabled, Code 10 = Failed to start, Code 28 = Missing driver) with color-coded status pills.

---

## 7. Cloud Identity & Entra ID Collector (`DsregcmdCollector.cs`)

- Executes `dsregcmd.exe /status` as a non-interactive child process.
- Parses diagnostic output blocks:
  - **Device State**: `AzureAdJoined`, `EnterpriseJoined`, `DomainJoined`, `DomainName`
  - **Tenant Details**: `TenantId`, `TenantName`, `Idp`
  - **User State**: `AzureAdPrt`, `UserPrincipalName`, `UserEmail`
  - **SSO Status**: Primary Refresh Token (PRT) validity and authentication authority.

---

## 8. Filter Drivers Collector (`FilterDriverCollector.cs`)

- Executes `fltmc.exe filters` and `fltmc.exe instances` to inspect the Windows Filter Manager subsystem.
- Extracts attached filesystem minifilters, active altitudes, number of instances, and frame numbers.
- Identifies antivirus filters, backup drivers, virtualization hooks, and disk encryption drivers.

---

## 9. Software Inventory Collector (`SoftwareInventoryCollector.cs`)

- Scans 64-bit and 32-bit (WOW64) uninstall keys:
  - `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall`
  - `HKLM\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall`
  - `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall`
- Extracts DisplayName, DisplayVersion, Publisher, InstallDate, InstallLocation, and UninstallString.
- Provides one-click uninstaller launching and CSV export.

---

## 10. Process & Service Collector (`ProcessServiceCollector.cs`)

- **Active Processes**: Enumerates running processes with PID, working set memory in megabytes, thread counts, and full binary file paths.
- **Windows Services**: Queries `ServiceController.GetServices()` to retrieve service names, display names, execution status, and startup modes.
- Integrated with `ServiceManagerHelper.cs` for starting, stopping, restarting, and configuring startup types in-process via `advapi32.dll`.
