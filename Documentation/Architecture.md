# XRAY ULTIMATE — System Architecture & Design Specification

## Overview

**XRAY ULTIMATE** is an audit-grade hardware diagnostics, system telemetry, and firmware exploration workstation application built on **WinUI 3** and **.NET 10 LTS** for 64-bit Windows systems (`net10.0-windows10.0.19041.0`).

The application is structured to deliver deep silicon-level hardware visibility, multi-tier security inspection, native ACPI/DSDT decompilation, live telemetry polling, and audit reporting without requiring third-party drivers or external kernel extensions.

---

## 1. Application Host & Runtime Configuration

```
┌────────────────────────────────────────────────────────┐
│                   XRAY ULTIMATE                        │
├──────────────────────────┬─────────────────────────────┤
│ Target Framework         │ net10.0-windows10.0.19041.0 │
│ Architecture             │ x64                         │
│ UI Framework             │ WinUI 3 (Windows App SDK)   │
│ Packaging                │ Unpackaged (Win32 Native)   │
│ Execution Level          │ requireAdministrator        │
│ Deployment Mode          │ Self-Contained / Portable   │
└──────────────────────────┴─────────────────────────────┘
```

### Key Project Properties (`XRAY ULTIMATE.csproj`):
- `<WindowsPackageType>None</WindowsPackageType>`: Runs as a pure, native desktop process without MSIX containerization restrictions.
- `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`: Embeds Windows App SDK runtimes directly with the application binary.
- `<ApplicationManifest>app.manifest</ApplicationManifest>`: Configured with `level="requireAdministrator"` to allow querying privileged WMI namespaces, raw physical disk drives, TPM providers, and firmware tables.

---

## 2. Process Integrity & Security Model

```
                     ┌─────────────────────────────┐
                     │     Elevated Launch (UAC)   │
                     │    Integrity: High (Admin)  │
                     └──────────────┬──────────────┘
                                    │
       ┌────────────────────────────┼────────────────────────────┐
       ▼                            ▼                            ▼
┌──────────────┐             ┌──────────────┐             ┌──────────────┐
│  Kernel32    │             │  WMI / CIM   │             │   Services   │
│ Firmw. Table │             │ PacketPrivacy│             │  advapi32    │
│  (ACPI, DSDT)│             │ Impersonation│             │  Full Access │
└──────────────┘             └──────────────┘             └──────────────┘
```

Because XRAY ULTIMATE runs at **High Integrity** (`requireAdministrator`):
1. **Raw Firmware Table Access**: Invokes `GetSystemFirmwareTable` (`'ACPI'`) to read raw kernel ACPI tables including DSDT, SSDT, TPM2, and FACP directly from system memory.
2. **PacketPrivacy WMI**: Queries `root\CIMV2\Security\MicrosoftTpm` with `AuthenticationLevel.PacketPrivacy` and `ImpersonationLevel.Impersonate`, bypassing access denials that affect unprivileged utilities.
3. **PnP Device Tree**: Enumerates `Win32_PnPEntity` across 29 classes and resolves device status, error codes, hardware IDs, and service drivers.
4. **Service Control Manager**: Connects to `advapi32.dll` (`OpenSCManager` with `SC_MANAGER_ALL_ACCESS`) to enable starting, stopping, restarting, and configuring startup modes on all Windows services.
5. **In-Process Shell Dialogs**: Uses in-process Win32 COM `IFileSaveDialog` (`CLSID_FileSaveDialog`) with classic `comdlg32.dll` fallback, preventing UIPI (User Interface Privilege Isolation) communication blocks that break standard brokered UWP file pickers under administrator tokens.

---

## 3. High-Level Subsystem Architecture

```
                                  MainWindow (Shell)
                 ┌────────────────────────┴────────────────────────┐
                 ▼                                                 ▼
        Navigation View                                  Content Frame (Pages)
  ┌──────────────────────────────┐                   ┌───────────────────────────┐
  │ • Dashboard   • GPU / Display│                   │ • DashboardPage           │
  │ • CPU         • Storage      │                   │ • CpuPage                 │
  │ • Memory      • Network      │                   │ • MemoryPage              │
  │ • Motherboard • Battery      │                   │ • AcpiPage                │
  │ • ACPI/DSDT   • Security/TPM │                   │ • DeviceManagerPage       │
  │ • Device Mgr  • Software     │                   │ • GpuPage                 │
  │ • Fltr Drivers• Processes    │                   │ • StoragePage             │
  │ • Sensors     • Benchmark    │                   │ • NetworkPage             │
  │ • dsregcmd    • Export       │                   │ • BatteryPage             │
  └──────────────────────────────┘                   │ • SecurityPage            │
                                                     │ • ... (18 Total Views)    │
                                                     └─────────────┬─────────────┘
                                                                   │
                                            SystemDiagnosticsService (Singleton)
                                    ┌──────────────────────────────┴──────────────────────────────┐
                                    ▼                                                             ▼
                             Hardware Engine                                               System Engine
                  ┌───────────────────────────────────┐                         ┌───────────────────────────────────┐
                  │ • CpuInfoCollector                │                         │ • OsSecurityInfoCollector         │
                  │ • MemoryInfoCollector             │                         │ • DeviceManagerCollector          │
                  │ • MotherboardInfoCollector        │                         │ • SoftwareInventoryCollector      │
                  │ • GpuInfoCollector                │                         │ • ProcessServiceCollector         │
                  │ • StorageInfoCollector            │                         │ • FilterDriverCollector           │
                  │ • NetworkInfoCollector            │                         │ • DsregcmdCollector               │
                  │ • BatteryInfoCollector            │                         │ • LiveSensorService               │
                  │ • AcpiService & Disassembler      │                         │ • BenchmarkService                │
                  └───────────────────────────────────┘                         └───────────────────────────────────┘
```

---

## 4. UI Architecture & Viewport Standards

All 18 views in XRAY ULTIMATE strictly implement the standardized viewport hierarchy:
- **Root Element**: `<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled" Padding="28,20,28,40">`
- **Main Container**: `<StackPanel Spacing="24" HorizontalAlignment="Stretch">` (WITHOUT restrictive `MaxWidth` constraints, ensuring full horizontal stretch across monitors up to 4K without centering drift).
- **Header Banner Card**: Elevated border card (`CornerRadius="12"`, `Padding="24,20"`) with a 64×64 accent icon badge, bold 22pt title, subtitle, live badge pill indicators, and quick-action tool buttons.
- **Card Matrix**: Key performance metrics grouped into 3–4 elevated stat cards (`CornerRadius="10"`, `Padding="16"`).
- **Grouped Properties**: Technical attributes organized into categorical elevated cards using `ItemsControl` and `PropertyGroup` bindings.

---

## 5. Directory Structure

```
XRAY ULTIMATE/
├── Assets/                        # Icons, application badges, splash screen
├── Helpers/                       # Interop, formatting, and Win32 helpers
│   ├── NativeFileDialog.cs        # In-process COM IFileSaveDialog & comdlg32
│   ├── PowerPlanHelper.cs         # Windows power scheme queries and activation
│   ├── ServiceManagerHelper.cs    # Win32 Service Control Manager (advapi32)
│   ├── UnitFormatter.cs           # Human-readable byte, speed, uptime formatting
│   └── WindowHelper.cs            # Window handle tracking and native bindings
├── Models/                        # Strongly-typed data transfer models
│   ├── AcpiInfo.cs                # ACPI tables, DSDT descriptors, device objects
│   ├── BenchmarkInfo.cs           # Compute and memory benchmark telemetry
│   ├── CpuInfo.cs                 # Microarchitecture, caches, instruction sets
│   ├── DeviceManagerInfo.cs       # PnP devices, classes, problem error codes
│   ├── GpuInfo.cs                 # Graphics adapters, monitors, resolutions
│   ├── MemoryInfo.cs              # Physical DIMMs, paging files, utilization
│   ├── NetworkInfo.cs             # Network interfaces, Wi-Fi telemetry, IPs
│   ├── ProcessServiceInfo.cs      # Active working sets and service controllers
│   ├── PropertyItem.cs            # Key-value property models and grouping helpers
│   ├── SecurityInfo.cs            # TPM 2.0, Secure Boot, BitLocker, Hotfixes
│   └── StorageInfo.cs             # NVMe/SATA disks, SMART health, logical volumes
├── Services/                      # Hardware and diagnostic data collectors
│   ├── AcpiDisassembler.cs        # AML to ASL/DSL disassembler engine
│   ├── AcpiService.cs             # Firmware table reader (kernel32)
│   ├── BenchmarkService.cs        # Multi-threaded mathematical benchmark
│   ├── CpuInfoCollector.cs        # CPU topology and instruction set probes
│   ├── DeviceManagerCollector.cs  # PnP hardware tree enumerator
│   ├── DsregcmdCollector.cs       # Entra ID / Azure AD hybrid join parser
│   ├── FilterDriverCollector.cs   # Filesystem minifilters and network filters
│   ├── GpuInfoCollector.cs        # DirectX/WDDM and nvidia-smi telemetry
│   ├── LiveSensorService.cs       # Real-time hardware polling timers
│   ├── MemoryInfoCollector.cs     # SMBIOS DIMM inspection & memory load
│   ├── NetworkInfoCollector.cs    # Network adapters and native Wi-Fi WLAN API
│   ├── OsSecurityInfoCollector.cs # Multi-tier TPM, Secure Boot, Defender probe
│   ├── ProcessServiceCollector.cs # Process working sets and Windows services
│   ├── ReportExportService.cs     # JSON, Text, and interactive HTML generation
│   ├── SoftwareInventoryCollector.cs # 64-bit and 32-bit registry software catalog
│   ├── StorageInfoCollector.cs    # Physical drive and volume inspection
│   ├── SystemDiagnosticsService.cs# Central orchestrator and report aggregator
│   └── WmiService.cs              # Authenticated WMI query engine
├── Views/                         # WinUI 3 pages (XAML + C# code-behind)
│   ├── AcpiPage.xaml              # Firmware table manifest & DSDT ASL editor
│   ├── BatteryPage.xaml           # Battery health, capacity, power plan switcher
│   ├── BenchmarkPage.xaml         # Multi-threaded CPU & memory speed benchmark
│   ├── CpuPage.xaml               # Processor specifications, caches, ISA
│   ├── DashboardPage.xaml         # Central system overview & telemetry cards
│   ├── DeviceManagerPage.xaml     # Categorized PnP hardware hierarchy
│   ├── DsregcmdPage.xaml          # Cloud identity and directory diagnostics
│   ├── ExportPage.xaml            # Audit report exporter (HTML, JSON, TXT)
│   ├── FiltersPage.xaml           # Minifilters, altitudes, network filter drivers
│   ├── GpuPage.xaml               # Video accelerators and connected displays
│   ├── MemoryPage.xaml            # Physical DIMMs, clock speeds, pagefile
│   ├── MotherboardPage.xaml       # SMBIOS board, chipset, and BIOS firmware
│   ├── NetworkPage.xaml           # Expandable network adapter diagnostic cards
│   ├── ProcessesPage.xaml         # Process manager & service controller
│   ├── SecurityPage.xaml          # Silicon TPM 2.0, Secure Boot, BitLocker
│   ├── SensorsPage.xaml           # Real-time live performance gauges
│   ├── SoftwarePage.xaml          # Installed application inventory
│   └── StoragePage.xaml           # Physical drives, partitions, SMART status
├── Documentation/                 # Architectural and developer documentation
├── MainWindow.xaml                # App shell, custom titlebar, navigation view
├── App.xaml                       # Application entry point and theme resources
└── app.manifest                   # Elevation manifest (requireAdministrator)
```
