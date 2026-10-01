# ACPI & DSDT Decompiler Specification

## Overview

The ACPI subsystem in **XRAY ULTIMATE** provides silicon-level visibility into system firmware tables (DSDT, SSDT, FACP, HPET, MCFG, MADT, TPM2, etc.) directly extracted from the running operating system kernel.

It features a built-in AML (ACPI Machine Language) bytecode to ASL/DSL (ACPI Source Language) disassembler engine (`AcpiDisassembler.cs`), engineered to produce clean, well-formed, and compilable source code.

---

## 1. Kernel ACPI Table Extraction

Firmware tables are read directly from physical memory using the Windows kernel firmware table provider:

```csharp
[DllImport("kernel32.dll", SetLastError = true)]
private static extern uint EnumSystemFirmwareTables(
    uint FirmwareTableProviderSignature, // 0x41435049 ('ACPI')
    IntPtr pFirmwareTableEnumBuffer,
    uint BufferSize);

[DllImport("kernel32.dll", SetLastError = true)]
private static extern uint GetSystemFirmwareTable(
    uint FirmwareTableProviderSignature,
    uint FirmwareTableID, // e.g. 0x54445344 ('DSDT')
    IntPtr pFirmwareTableBuffer,
    uint BufferSize);
```

### Table Signature Mapping
1. **DSDT** (`0x54445344`): Differentiated System Description Table — contains the primary hardware tree, power state definitions, and device scopes.
2. **SSDT**: Secondary System Description Tables.
3. **FACP**: Fixed ACPI Description Table.
4. **MADT**: Multiple APIC Description Table (interrupt routing & core topology).
5. **TPM2**: Trusted Platform Module 2.0 ACPI Hardware Interface Table.
6. **MCFG**: PCI Express Memory-Mapped Configuration Space Base Address Table.

---

## 2. AML Bytecode to ASL Decompilation Architecture

```
┌────────────────────────────────────────────────────────┐
│                   Raw AML Bytecode                     │
└──────────────────────────┬─────────────────────────────┘
                           │
       ┌───────────────────┴───────────────────┐
       ▼                                       ▼
External iasl.exe Available?           Built-in Engine
  (App folder, tools, or PATH)          (C# Native Engine)
       │                                       │
       ▼                                       ▼
  Execute iasl -d                         DecompileBlock()
  Intel Certified Output                  Full AML AST Recovery
```

### Key Technical Innovations in the Built-in Engine:

### A. Strict ACPI PkgLength Boundary Calculation
In the ACPI Specification (Section 20.2.4), `PkgLength` encoding specifies the length of a package **including the PkgLength encoding bytes themselves**:

```csharp
private static int ReadPkgEnd(byte[] aml, ref int i, int maxEnd)
{
    if (i >= aml.Length) return i;
    int pkgStart = i;
    byte b0 = aml[i++];
    int byteCount = (b0 >> 6) & 0x03;

    int length;
    if (byteCount == 0)
    {
        length = b0 & 0x3F;
    }
    else
    {
        length = b0 & 0x0F;
        if (byteCount >= 1 && i < aml.Length) length |= (aml[i++] << 4);
        if (byteCount >= 2 && i < aml.Length) length |= (aml[i++] << 12);
        if (byteCount >= 3 && i < aml.Length) length |= (aml[i++] << 20);
    }

    int targetEnd = pkgStart + length;
    return Math.Min(maxEnd, targetEnd);
}
```
*Why this matters*: Earlier implementations calculated offsets after reading the lead bytes, causing progressive desynchronization across nested scopes and skipping thousands of lines of bytecode. `ReadPkgEnd` eliminates boundary drift.

### B. Buffer and Package Isolation
Raw binary data blocks (e.g. WMI binary MOF buffers `XWDG`, embedded control byte arrays) are strictly bounded (`i = bufEnd;`). Raw binary bytes are never dumped into the opcode decoder, preventing data bytes from being misinterpreted as control flow statements.

### C. Intel ACPICA ExternalOp (`0x15`) Support
Modern OEM firmware (Dell, Lenovo, ASUS, HP) includes root-level `If (Zero)` blocks containing thousands of external symbol declarations:

```asl
If (Zero)
{
    External (LHIH)
    External (LLOW)
    External (IGDS)
    External (\_SB_.PCI0.GFX0, DeviceObj)
    ...
}
```
The disassembler recognizes `0x15` and parses the object type (`DeviceObj`, `MethodObj`, `IntObj`, etc.) and argument counts according to ACPICA grammar.

### D. Mathematical Brace Balance Guarantee
Tracks nesting depth across all control blocks (`Scope`, `Device`, `ThermalZone`, `Processor`, `Field`, `IndexField`, `BankField`, `PowerResource`, `Method`, `If`, `Else`, `While`). Open braces `{` and closing braces `}` match with a **difference of exactly 0**, ensuring the output file can be compiled back with Intel `iasl`.

---

## 3. Intel `iasl` Toolchain Compatibility

The generated `.dsl` file adheres to standard Intel ASL syntax:
- Starts with `DefinitionBlock ("dsdt.aml", "DSDT", <rev>, "<oemid>", "<oemtableid>", <oemrev>)`.
- Standard 4-character identifiers (`_SB_`, `PCI0`, `EC0_`, `_HID`, `_CRS`, `_STA`, `_INI`) are preserved with underscores.
- Zero unprintable or control characters (`0` null bytes or control symbols in output).
- Can be compiled directly with:
  ```cmd
  iasl.exe -ve dsdt.dsl
  ```
