using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class AcpiService
{
    private const uint ACPI_PROVIDER = 0x41435049; // 'ACPI'

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint EnumSystemFirmwareTables(uint FirmwareTableProviderSignature, IntPtr pFirmwareTableEnumBuffer, uint BufferSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetSystemFirmwareTable(uint FirmwareTableProviderSignature, uint FirmwareTableID, IntPtr pFirmwareTableBuffer, uint BufferSize);

    public static AcpiReport Collect()
    {
        var report = new AcpiReport();

        // 1. Enumerate all ACPI tables
        var tableIds = EnumerateAcpiTableSignatures();
        foreach (var id in tableIds)
        {
            var tableItem = ReadAcpiTable(id);
            if (tableItem != null)
            {
                report.Tables.Add(tableItem);
            }
        }

        // 2. Fetch and parse DSDT (Differentiated System Description Table)
        var dsdt = ReadDsdt();
        if (dsdt != null)
        {
            report.Dsdt = dsdt;
            if (dsdt.Header != null && !report.Tables.Exists(t => t.TableSignature == "DSDT"))
            {
                report.Tables.Insert(0, dsdt.Header);
            }
        }

        // 3. Properties
        report.Properties.Add(new PropertyItem("ACPI Overview", "Total ACPI Firmware Tables", report.Tables.Count.ToString()));
        if (report.Dsdt?.Header != null)
        {
            report.Properties.Add(new PropertyItem("DSDT", "DSDT Table Size", report.Dsdt.Header.FormattedLength));
            report.Properties.Add(new PropertyItem("DSDT", "OEM ID", report.Dsdt.Header.OemId));
            report.Properties.Add(new PropertyItem("DSDT", "OEM Table ID", report.Dsdt.Header.OemTableId));
            report.Properties.Add(new PropertyItem("DSDT", "Creator ID", report.Dsdt.Header.CreatorId));
            report.Properties.Add(new PropertyItem("DSDT", "ACPI Revision", $"Revision {report.Dsdt.Header.Revision}"));
            report.Properties.Add(new PropertyItem("DSDT", "Objects & Devices Discovered", report.Dsdt.DiscoveredObjects.Count.ToString()));
        }

        foreach (var t in report.Tables)
        {
            report.Properties.Add(new PropertyItem("ACPI Tables", t.TableSignature, $"{t.TableDescription} ({t.FormattedLength}, OEM: {t.OemId})"));
        }

        return report;
    }

    private static List<uint> EnumerateAcpiTableSignatures()
    {
        var list = new List<uint>();
        try
        {
            uint size = EnumSystemFirmwareTables(ACPI_PROVIDER, IntPtr.Zero, 0);
            if (size == 0) return list;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                uint read = EnumSystemFirmwareTables(ACPI_PROVIDER, buffer, size);
                byte[] bytes = new byte[read];
                Marshal.Copy(buffer, bytes, 0, (int)read);

                for (int i = 0; i + 3 < bytes.Length; i += 4)
                {
                    uint id = BitConverter.ToUInt32(bytes, i);
                    list.Add(id);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch { }

        return list;
    }

    private static AcpiTableItem? ReadAcpiTable(uint tableId)
    {
        try
        {
            uint size = GetSystemFirmwareTable(ACPI_PROVIDER, tableId, IntPtr.Zero, 0);
            if (size == 0) return null;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                uint read = GetSystemFirmwareTable(ACPI_PROVIDER, tableId, buffer, size);
                if (read == 0) return null;

                byte[] bytes = new byte[read];
                Marshal.Copy(buffer, bytes, 0, (int)read);

                return ParseAcpiHeader(bytes);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch
        {
            return null;
        }
    }

    private static DsdtReport? ReadDsdt()
    {
        try
        {
            // DSDT ID: 'D' | ('S' << 8) | ('D' << 16) | ('T' << 24) = 0x54445344
            uint dsdtId = (uint)('D' | ('S' << 8) | ('D' << 16) | ('T' << 24));
            uint size = GetSystemFirmwareTable(ACPI_PROVIDER, dsdtId, IntPtr.Zero, 0);
            if (size == 0) return null;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                uint read = GetSystemFirmwareTable(ACPI_PROVIDER, dsdtId, buffer, size);
                if (read == 0) return null;

                byte[] bytes = new byte[read];
                Marshal.Copy(buffer, bytes, 0, (int)read);

                var dsdt = new DsdtReport
                {
                    RawAmlData = bytes,
                    Header = ParseAcpiHeader(bytes)
                };

                // Parse AML Objects and Strings
                ParseAmlObjects(bytes, dsdt);

                // Generate Hex Dump preview
                dsdt.HexDumpPreview = GenerateHexDump(bytes, Math.Min(bytes.Length, 1536));

                // Decompile AML bytecode to ASL/DSL source code
                dsdt.AslSourceCode = AcpiDisassembler.DisassembleToDsl(bytes, "DSDT");

                return dsdt;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch
        {
            return null;
        }
    }

    private static AcpiTableItem? ParseAcpiHeader(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 36) return null;

        var item = new AcpiTableItem
        {
            TableSignature = Encoding.ASCII.GetString(bytes, 0, 4),
            TableLength = BitConverter.ToUInt32(bytes, 4),
            Revision = bytes[8],
            Checksum = bytes[9],
            OemId = Encoding.ASCII.GetString(bytes, 10, 6).Trim(),
            OemTableId = Encoding.ASCII.GetString(bytes, 16, 8).Trim(),
            OemRevision = BitConverter.ToUInt32(bytes, 24),
            CreatorId = Encoding.ASCII.GetString(bytes, 28, 4).Trim(),
            CreatorRevision = BitConverter.ToUInt32(bytes, 32),
            RawBytes = bytes
        };

        item.TableDescription = GetTableDescription(item.TableSignature);
        item.AslDisassembly = AcpiDisassembler.DisassembleToDsl(bytes, item.TableSignature);
        return item;
    }

    private static void ParseAmlObjects(byte[] aml, DsdtReport dsdt)
    {
        var discoveredObjects = new Dictionary<string, AcpiDeviceObject>(StringComparer.OrdinalIgnoreCase);
        var discoveredStrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Scan printable ASCII strings and ACPI identifiers
        int minLen = 4;
        var sb = new StringBuilder();

        for (int i = 36; i < aml.Length; i++)
        {
            byte b = aml[i];
            if ((b >= 0x20 && b <= 0x7E))
            {
                sb.Append((char)b);
            }
            else
            {
                if (sb.Length >= minLen)
                {
                    string s = sb.ToString().Trim();
                    if (s.Length >= 4 && IsLikelyAcpiIdentifier(s))
                    {
                        discoveredStrings.Add(s);

                        // Classify known hardware devices
                        ClassifyAcpiDevice(s, discoveredObjects);
                    }
                }
                sb.Clear();
            }
        }

        // Also search for standard ACPI Hardware IDs (_HID) like PNP0A08, PNP0C0A, ACPI0003
        var amlText = Encoding.ASCII.GetString(aml);
        var hidMatches = Regex.Matches(amlText, @"(PNP[0-9A-Fa-f]{4}|ACPI[0-9A-Fa-f]{4})");
        foreach (Match m in hidMatches)
        {
            discoveredStrings.Add(m.Value);
            string desc = GetHardwareIdDescription(m.Value);
            if (!discoveredObjects.ContainsKey(m.Value))
            {
                discoveredObjects[m.Value] = new AcpiDeviceObject(m.Value, "Hardware ID (_HID)", desc);
            }
        }

        dsdt.DiscoveredObjects = new List<AcpiDeviceObject>(discoveredObjects.Values);
        dsdt.DiscoveredIdentifiers = new List<string>(discoveredStrings);
    }

    private static bool IsLikelyAcpiIdentifier(string s)
    {
        if (s.StartsWith("_SB") || s.StartsWith("_PR") || s.StartsWith("_TZ") || s.StartsWith("_GPE") || s.StartsWith("_SI")) return true;
        if (s.Contains("PCI0") || s.Contains("LPCB") || s.Contains("EC0") || s.Contains("BAT0") || s.Contains("ACAD")) return true;
        if (s.StartsWith("PNP") || s.StartsWith("ACPI")) return true;
        if (s.Length == 4 && s.All(c => char.IsLetterOrDigit(c) || c == '_')) return true;
        return false;
    }

    private static void ClassifyAcpiDevice(string ident, Dictionary<string, AcpiDeviceObject> dict)
    {
        if (dict.ContainsKey(ident)) return;

        if (ident.Contains("PCI0") || ident.Equals("PCI0"))
            dict[ident] = new AcpiDeviceObject(ident, "PCI Host Bridge", "Root PCI Express / PCI Bus");
        else if (ident.Contains("LPCB") || ident.Contains("LPC0"))
            dict[ident] = new AcpiDeviceObject(ident, "LPC Bus", "Low Pin Count / Legacy Bus Controller");
        else if (ident.Contains("BAT0") || ident.Contains("BAT1") || ident.Contains("BATT"))
            dict[ident] = new AcpiDeviceObject(ident, "Battery Object", "Primary Smart Battery Device Subsystem");
        else if (ident.Contains("ACAD") || ident.Contains("ADP1") || ident.Contains("AC0"))
            dict[ident] = new AcpiDeviceObject(ident, "AC Power Adapter", "AC Mains Power Source Controller");
        else if (ident.Contains("TZ0") || ident.Contains("THM0") || ident.Contains("_TZ"))
            dict[ident] = new AcpiDeviceObject(ident, "Thermal Zone", "ACPI System Thermal & Fan Management Zone");
        else if (ident.Contains("CPU0") || ident.Contains("PR00") || ident.Contains("_PR"))
            dict[ident] = new AcpiDeviceObject(ident, "Processor Core", "ACPI Processor P/C/T Power Management Core");
        else if (ident.Contains("EC0") || ident.Contains("EC__"))
            dict[ident] = new AcpiDeviceObject(ident, "Embedded Controller", "Hardware Embedded Controller (EC / KBC)");
        else if (ident.Contains("GFX0") || ident.Contains("PEG0") || ident.Contains("IGPU"))
            dict[ident] = new AcpiDeviceObject(ident, "Graphics Device", "PCIe Graphics Controller / iGPU Output");
        else if (ident.Contains("HDEF") || ident.Contains("HDAS") || ident.Contains("AZAL"))
            dict[ident] = new AcpiDeviceObject(ident, "High Definition Audio", "Intel/Realtek HD Audio Controller");
        else if (ident.Contains("XHC") || ident.Contains("USB0"))
            dict[ident] = new AcpiDeviceObject(ident, "USB 3.x Controller", "xHCI Extensible Host Controller");
        else if (ident.Contains("I2C0") || ident.Contains("I2C1"))
            dict[ident] = new AcpiDeviceObject(ident, "I2C Bus Controller", "Inter-Integrated Circuit Serial Bus");
        else if (ident.Contains("TPM") || ident.Contains("PTIS"))
            dict[ident] = new AcpiDeviceObject(ident, "TPM Security Module", "Trusted Platform Module ACPI Interface");
    }

    private static string GetHardwareIdDescription(string hid) => hid.ToUpperInvariant() switch
    {
        "PNP0A08" => "PCI Express Root Bridge",
        "PNP0A03" => "PCI Root Bridge",
        "PNP0C0A" => "Control Method Battery",
        "PNP0C09" => "Embedded Controller Device",
        "PNP0C0D" => "Lid Device",
        "PNP0C0C" => "Power Button Device",
        "PNP0C0E" => "Sleep Button Device",
        "PNP0000" => "AT Interrupt Controller",
        "PNP0100" => "System Timer",
        "PNP0103" => "High Precision Event Timer (HPET)",
        "PNP0200" => "AT DMA Controller",
        "PNP0B00" => "Real-Time Clock (RTC)",
        "PNP0303" => "IBM Enhanced (101/102-Key) Keyboard",
        "PNP0F13" => "PS/2 Mouse Port",
        "ACPI0003" => "Power Source / AC Adapter",
        "ACPI0004" => "Module Device",
        "ACPI0007" => "Processor Device",
        "ACPI000E" => "Time and Alarm Device",
        _ => "Standard ACPI Peripheral Device"
    };

    private static string GetTableDescription(string sig) => sig.ToUpperInvariant() switch
    {
        "DSDT" => "Differentiated System Description Table",
        "SSDT" => "Secondary System Description Table",
        "FACP" => "Fixed ACPI Description Table (FADT)",
        "APIC" => "Multiple APIC Description Table (MADT)",
        "MCFG" => "PCI Express Memory Mapped Configuration Space",
        "HPET" => "High Precision Event Timer Description Table",
        "BGRT" => "Boot Graphics Resource Table",
        "TPM2" => "Trusted Platform Module 2.0 Table",
        "DMAR" => "Direct Memory Access Remapping (VT-d / AMD-Vi)",
        "SLIC" => "Software Licensing Description Table",
        "MSDM" => "Microsoft Data Management Table (Windows Product Key)",
        "FPDT" => "Firmware Performance Data Table",
        "LPIT" => "Low Power Idle Table",
        "WSMT" => "Windows SMM Security Mitigation Table",
        "NHLT" => "Non-HDAudio Link Table",
        "DBGP" => "Debug Port Table",
        "DBG2" => "Debug Port Table 2",
        "BOOT" => "Simple Boot Flag Table",
        "WAET" => "Windows ACPI Emulated Devices Table",
        "ASF!" => "Alert Standard Format Table",
        _ => "ACPI Firmware Description Table"
    };

    private static string GenerateHexDump(byte[] bytes, int length)
    {
        var sb = new StringBuilder();
        int rows = (length + 15) / 16;

        for (int r = 0; r < rows; r++)
        {
            int offset = r * 16;
            sb.Append($"{offset:X8}  ");

            // Hex bytes
            for (int c = 0; c < 16; c++)
            {
                if (c == 8) sb.Append(" ");
                int idx = offset + c;
                if (idx < length)
                    sb.Append($"{bytes[idx]:X2} ");
                else
                    sb.Append("   ");
            }

            sb.Append(" |");

            // ASCII
            for (int c = 0; c < 16; c++)
            {
                int idx = offset + c;
                if (idx < length)
                {
                    byte b = bytes[idx];
                    sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
                }
            }

            sb.AppendLine("|");
        }

        return sb.ToString();
    }
}
