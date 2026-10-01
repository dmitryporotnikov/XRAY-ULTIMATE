using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using XRAY_ULTIMATE.Models;

namespace XRAY_ULTIMATE.Services;

public static class AcpiDisassembler
{
    public static string DisassembleToDsl(byte[]? amlBytes, string defaultName = "DSDT")
    {
        if (amlBytes == null || amlBytes.Length < 36)
            return "/* Error: Invalid or empty AML bytecode stream */";

        // 1. Check if external iasl.exe is available for Intel-certified decompilation
        string? iaslDsl = TryDisassembleWithIasl(amlBytes);
        if (!string.IsNullOrEmpty(iaslDsl))
        {
            return iaslDsl;
        }

        // 2. Built-in clean AML-to-ASL disassembler engine
        return DisassembleBuiltIn(amlBytes, defaultName);
    }

    private static string? TryDisassembleWithIasl(byte[] amlBytes)
    {
        try
        {
            // Look for iasl.exe in app folder, tools folder, or PATH
            string baseDir = AppContext.BaseDirectory;
            string[] possiblePaths =
            {
                Path.Combine(baseDir, "iasl.exe"),
                Path.Combine(baseDir, "tools", "iasl.exe"),
                "iasl.exe"
            };

            string? iaslPath = null;
            foreach (var p in possiblePaths)
            {
                if (p == "iasl.exe" || File.Exists(p))
                {
                    iaslPath = p;
                    break;
                }
            }

            if (iaslPath == null) return null;

            string tempDir = Path.Combine(Path.GetTempPath(), "xray_acpi_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string amlFile = Path.Combine(tempDir, "table.aml");
            string dslFile = Path.Combine(tempDir, "table.dsl");

            File.WriteAllBytes(amlFile, amlBytes);

            var psi = new ProcessStartInfo
            {
                FileName = iaslPath,
                Arguments = $"-d \"{amlFile}\"",
                WorkingDirectory = tempDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit(10000);
                if (File.Exists(dslFile))
                {
                    string content = File.ReadAllText(dslFile, Encoding.UTF8);
                    try { Directory.Delete(tempDir, true); } catch { }
                    return content;
                }
            }

            try { Directory.Delete(tempDir, true); } catch { }
        }
        catch { }

        return null;
    }

    private static string DisassembleBuiltIn(byte[] aml, string tableName)
    {
        var sb = new StringBuilder();

        // Parse Table Header
        string sig = ReadFixedAscii(aml, 0, 4);
        uint length = BitConverter.ToUInt32(aml, 4);
        byte revision = aml[8];
        byte checksum = aml[9];
        string oemId = CleanString(ReadFixedAscii(aml, 10, 6));
        string oemTableId = CleanString(ReadFixedAscii(aml, 16, 8));
        uint oemRev = BitConverter.ToUInt32(aml, 24);
        string creatorId = CleanString(ReadFixedAscii(aml, 28, 4));
        uint creatorRev = BitConverter.ToUInt32(aml, 32);

        sb.AppendLine("/*");
        sb.AppendLine(" * Intel ACPI Component Architecture");
        sb.AppendLine(" * AML/ASL+ Disassembler (XRAY ULTIMATE Engine)");
        sb.AppendLine($" * Disassembly of {sig}, {DateTime.Now:ddd MMM dd HH:mm:ss yyyy}");
        sb.AppendLine(" *");
        sb.AppendLine($" * ACPI Data Table [{sig}]");
        sb.AppendLine($" * Table Length:     0x{length:X8} ({length} bytes)");
        sb.AppendLine($" * Table Revision:   0x{revision:X2}");
        sb.AppendLine($" * Checksum:         0x{checksum:X2}");
        sb.AppendLine($" * OEM ID:           \"{oemId}\"");
        sb.AppendLine($" * OEM Table ID:     \"{oemTableId}\"");
        sb.AppendLine($" * OEM Revision:     0x{oemRev:X8} ({oemRev})");
        sb.AppendLine($" * Compiler ID:      \"{creatorId}\"");
        sb.AppendLine($" * Compiler Version: 0x{creatorRev:X8} ({creatorRev})");
        sb.AppendLine(" */");
        sb.AppendLine();
        sb.AppendLine($"DefinitionBlock (\"{tableName.ToLowerInvariant()}.aml\", \"{sig}\", 0x{revision:X2}, \"{oemId}\", \"{oemTableId}\", 0x{oemRev:X8})");
        sb.AppendLine("{");

        int openBraces = 1;
        int closeBraces = 0;

        int offset = 36;
        int maxOffset = (int)Math.Min(length, (uint)aml.Length);

        DecompileBlock(aml, offset, maxOffset, 1, sb, ref openBraces, ref closeBraces);

        // Ensure all open scopes balance cleanly to 0
        while (openBraces > closeBraces + 1)
        {
            int indent = Math.Max(1, openBraces - closeBraces - 1);
            sb.AppendLine(new string(' ', Math.Min(indent, 10) * 4) + "}");
            closeBraces++;
        }

        sb.AppendLine("}");
        closeBraces++;
        return sb.ToString();
    }

    private static void DecompileBlock(byte[] aml, int start, int end, int indent, StringBuilder sb, ref int openBraces, ref int closeBraces)
    {
        if (indent > 12) return;
        int i = start;
        string pad = new string(' ', Math.Min(indent, 10) * 4);
        bool lastOpWasIf = false;

        while (i < end)
        {
            byte op = aml[i];

            // 0x15: ExternalOp
            if (op == 0x15)
            {
                lastOpWasIf = false;
                i++;
                string name = ReadNameString(aml, ref i, end);
                byte objType = i < end ? aml[i++] : (byte)0;
                byte numArgs = i < end ? aml[i++] : (byte)0;
                string typeStr = objType switch
                {
                    1 => "IntObj",
                    2 => "StrObj",
                    3 => "BuffObj",
                    4 => "PkgObj",
                    5 => "FieldUnitObj",
                    6 => "DeviceObj",
                    7 => "EventObj",
                    8 => "MethodObj",
                    9 => "MutexObj",
                    0x0A => "OpRegionObj",
                    0x0B => "PowerResObj",
                    0x0C => "ProcessorObj",
                    0x0D => "ThermalZoneObj",
                    _ => "UnknownObj"
                };
                if (objType == 8 && numArgs > 0)
                    sb.AppendLine($"{pad}External ({name}, {typeStr}, {numArgs})");
                else if (objType > 0)
                    sb.AppendLine($"{pad}External ({name}, {typeStr})");
                else
                    sb.AppendLine($"{pad}External ({name})");
                continue;
            }

            // 0x10: ScopeOp
            if (op == 0x10)
            {
                lastOpWasIf = false;
                i++;
                int blockEnd = ReadPkgEnd(aml, ref i, end);
                string name = ReadNameString(aml, ref i, blockEnd);

                sb.AppendLine();
                sb.AppendLine($"{pad}Scope ({name})");
                sb.AppendLine($"{pad}{{");
                openBraces++;
                DecompileBlock(aml, i, blockEnd, indent + 1, sb, ref openBraces, ref closeBraces);
                sb.AppendLine($"{pad}}}");
                closeBraces++;
                i = blockEnd;
                continue;
            }

            // 0xA0: IfOp
            if (op == 0xA0)
            {
                lastOpWasIf = true;
                i++;
                int blockEnd = ReadPkgEnd(aml, ref i, end);
                string pred = ReadDataRefObject(aml, ref i, blockEnd);
                sb.AppendLine();
                sb.AppendLine($"{pad}If ({pred})");
                sb.AppendLine($"{pad}{{");
                openBraces++;
                DecompileBlock(aml, i, blockEnd, indent + 1, sb, ref openBraces, ref closeBraces);
                sb.AppendLine($"{pad}}}");
                closeBraces++;
                i = blockEnd;
                continue;
            }

            // 0xA1: ElseOp
            if (op == 0xA1)
            {
                if (!lastOpWasIf)
                {
                    i++;
                    continue;
                }
                lastOpWasIf = false;
                i++;
                int blockEnd = ReadPkgEnd(aml, ref i, end);
                sb.AppendLine();
                sb.AppendLine($"{pad}Else");
                sb.AppendLine($"{pad}{{");
                openBraces++;
                DecompileBlock(aml, i, blockEnd, indent + 1, sb, ref openBraces, ref closeBraces);
                sb.AppendLine($"{pad}}}");
                closeBraces++;
                i = blockEnd;
                continue;
            }

            // 0x5B ExtOp Prefix
            if (op == 0x5B && i + 1 < end)
            {
                byte extOp = aml[i + 1];
                i += 2;

                // 0x5B 0x82: DeviceOp
                if (extOp == 0x82)
                {
                    int blockEnd = ReadPkgEnd(aml, ref i, end);
                    string name = ReadNameString(aml, ref i, blockEnd);

                    sb.AppendLine();
                    sb.AppendLine($"{pad}Device ({name})");
                    sb.AppendLine($"{pad}{{");
                    openBraces++;
                    DecompileBlock(aml, i, blockEnd, indent + 1, sb, ref openBraces, ref closeBraces);
                    sb.AppendLine($"{pad}}}");
                    closeBraces++;
                    i = blockEnd;
                    continue;
                }

                // 0x5B 0x85: ThermalZoneOp
                if (extOp == 0x85)
                {
                    int blockEnd = ReadPkgEnd(aml, ref i, end);
                    string name = ReadNameString(aml, ref i, blockEnd);

                    sb.AppendLine();
                    sb.AppendLine($"{pad}ThermalZone ({name})");
                    sb.AppendLine($"{pad}{{");
                    openBraces++;
                    DecompileBlock(aml, i, blockEnd, indent + 1, sb, ref openBraces, ref closeBraces);
                    sb.AppendLine($"{pad}}}");
                    closeBraces++;
                    i = blockEnd;
                    continue;
                }

                // 0x5B 0x83: ProcessorOp
                if (extOp == 0x83)
                {
                    int blockEnd = ReadPkgEnd(aml, ref i, end);
                    string name = ReadNameString(aml, ref i, blockEnd);
                    byte procId = i < blockEnd ? aml[i++] : (byte)0;
                    uint pblkAddr = ReadDWord(aml, ref i, blockEnd);
                    byte pblkLen = i < blockEnd ? aml[i++] : (byte)0;

                    sb.AppendLine();
                    sb.AppendLine($"{pad}Processor ({name}, 0x{procId:X2}, 0x{pblkAddr:X8}, 0x{pblkLen:X2})");
                    sb.AppendLine($"{pad}{{");
                    openBraces++;
                    DecompileBlock(aml, i, blockEnd, indent + 1, sb, ref openBraces, ref closeBraces);
                    sb.AppendLine($"{pad}}}");
                    closeBraces++;
                    i = blockEnd;
                    continue;
                }

                // 0x5B 0x80: OpRegionOp
                if (extOp == 0x80)
                {
                    string name = ReadNameString(aml, ref i, end);
                    byte space = i < end ? aml[i++] : (byte)0;
                    string spaceStr = space switch
                    {
                        0x00 => "SystemMemory",
                        0x01 => "SystemIO",
                        0x02 => "PCI_Config",
                        0x03 => "EmbeddedControl",
                        0x04 => "SMBus",
                        0x05 => "SystemCMOS",
                        0x06 => "PciBarTarget",
                        0x07 => "IPMI",
                        0x08 => "GeneralPurposeIO",
                        0x09 => "GenericSerialBus",
                        _ => $"0x{space:X2}"
                    };
                    string offsetExpr = ReadIntegerOrTerm(aml, ref i, end);
                    string lengthExpr = ReadIntegerOrTerm(aml, ref i, end);
                    sb.AppendLine($"{pad}OperationRegion ({name}, {spaceStr}, {offsetExpr}, {lengthExpr})");
                    continue;
                }

                // 0x5B 0x81: FieldOp
                if (extOp == 0x81)
                {
                    int blockEnd = ReadPkgEnd(aml, ref i, end);
                    string regionName = ReadNameString(aml, ref i, blockEnd);
                    byte flags = i < blockEnd ? aml[i++] : (byte)0;
                    string accStr = (flags & 0x0F) switch
                    {
                        0 => "AnyAcc",
                        1 => "ByteAcc",
                        2 => "WordAcc",
                        3 => "DWordAcc",
                        4 => "QWordAcc",
                        5 => "BufferAcc",
                        _ => "AnyAcc"
                    };
                    string lockStr = ((flags >> 4) & 1) != 0 ? "Lock" : "NoLock";
                    string updateStr = ((flags >> 5) & 3) switch
                    {
                        0 => "Preserve",
                        1 => "WriteAsOnes",
                        2 => "WriteAsZeros",
                        _ => "Preserve"
                    };

                    sb.AppendLine($"{pad}Field ({regionName}, {accStr}, {lockStr}, {updateStr})");
                    sb.AppendLine($"{pad}{{");
                    openBraces++;
                    while (i < blockEnd)
                    {
                        if (aml[i] == 0x00)
                        {
                            i++;
                            int bits = ReadPkgEnd(aml, ref i, blockEnd);
                            sb.AppendLine($"{pad}    Offset (0x{(bits / 8):X}),");
                        }
                        else
                        {
                            string fieldName = ReadNameSeg(aml, ref i, blockEnd);
                            int bits = ReadPkgEnd(aml, ref i, blockEnd);
                            sb.AppendLine($"{pad}    {fieldName},   {bits},");
                        }
                    }
                    sb.AppendLine($"{pad}}}");
                    closeBraces++;
                    i = blockEnd;
                    continue;
                }

                // 0x5B 0x86: IndexFieldOp
                if (extOp == 0x86)
                {
                    int blockEnd = ReadPkgEnd(aml, ref i, end);
                    string idxName = ReadNameString(aml, ref i, blockEnd);
                    string dataName = ReadNameString(aml, ref i, blockEnd);
                    byte flags = i < blockEnd ? aml[i++] : (byte)0;
                    sb.AppendLine($"{pad}IndexField ({idxName}, {dataName}, AnyAcc, NoLock, Preserve)");
                    sb.AppendLine($"{pad}{{");
                    openBraces++;
                    while (i < blockEnd)
                    {
                        if (aml[i] == 0x00) { i++; int bits = ReadPkgEnd(aml, ref i, blockEnd); sb.AppendLine($"{pad}    Offset (0x{(bits / 8):X}),"); }
                        else { string fn = ReadNameSeg(aml, ref i, blockEnd); int bits = ReadPkgEnd(aml, ref i, blockEnd); sb.AppendLine($"{pad}    {fn},   {bits},"); }
                    }
                    sb.AppendLine($"{pad}}}");
                    closeBraces++;
                    i = blockEnd;
                    continue;
                }

                // 0x5B 0x87: BankFieldOp
                if (extOp == 0x87)
                {
                    int blockEnd = ReadPkgEnd(aml, ref i, end);
                    string rgnName = ReadNameString(aml, ref i, blockEnd);
                    string bnkName = ReadNameString(aml, ref i, blockEnd);
                    string bnkVal = ReadIntegerOrTerm(aml, ref i, blockEnd);
                    sb.AppendLine($"{pad}BankField ({rgnName}, {bnkName}, {bnkVal}, AnyAcc, NoLock, Preserve)");
                    sb.AppendLine($"{pad}{{");
                    openBraces++;
                    while (i < blockEnd)
                    {
                        if (aml[i] == 0x00) { i++; int bits = ReadPkgEnd(aml, ref i, blockEnd); sb.AppendLine($"{pad}    Offset (0x{(bits / 8):X}),"); }
                        else { string fn = ReadNameSeg(aml, ref i, blockEnd); int bits = ReadPkgEnd(aml, ref i, blockEnd); sb.AppendLine($"{pad}    {fn},   {bits},"); }
                    }
                    sb.AppendLine($"{pad}}}");
                    closeBraces++;
                    i = blockEnd;
                    continue;
                }

                // 0x5B 0x01: MutexOp
                if (extOp == 0x01)
                {
                    string name = ReadNameString(aml, ref i, end);
                    byte sync = i < end ? aml[i++] : (byte)0;
                    sb.AppendLine($"{pad}Mutex ({name}, 0x{sync:X2})");
                    continue;
                }

                // 0x5B 0x02: EventOp
                if (extOp == 0x02)
                {
                    string name = ReadNameString(aml, ref i, end);
                    sb.AppendLine($"{pad}Event ({name})");
                    continue;
                }

                // 0x5B 0x1F: PowerResourceOp
                if (extOp == 0x1F)
                {
                    int blockEnd = ReadPkgEnd(aml, ref i, end);
                    string name = ReadNameString(aml, ref i, blockEnd);
                    byte sysLevel = i < blockEnd ? aml[i++] : (byte)0;
                    ushort resOrder = ReadWord(aml, ref i, blockEnd);
                    sb.AppendLine($"{pad}PowerResource ({name}, 0x{sysLevel:X2}, 0x{resOrder:X4})");
                    sb.AppendLine($"{pad}{{");
                    openBraces++;
                    DecompileBlock(aml, i, blockEnd, indent + 1, sb, ref openBraces, ref closeBraces);
                    sb.AppendLine($"{pad}}}");
                    closeBraces++;
                    i = blockEnd;
                    continue;
                }

                // 0x5B 0x27: SleepOp
                if (extOp == 0x27)
                {
                    string msec = ReadIntegerOrTerm(aml, ref i, end);
                    sb.AppendLine($"{pad}Sleep ({msec})");
                    continue;
                }

                // 0x5B 0x28: StallOp
                if (extOp == 0x28)
                {
                    string usec = ReadIntegerOrTerm(aml, ref i, end);
                    sb.AppendLine($"{pad}Stall ({usec})");
                    continue;
                }
            }

            // 0x14: MethodOp
            if (op == 0x14)
            {
                i++;
                int blockEnd = ReadPkgEnd(aml, ref i, end);
                string name = ReadNameString(aml, ref i, blockEnd);
                byte flags = i < blockEnd ? aml[i++] : (byte)0;
                int argCount = flags & 0x07;
                string serialized = (flags & 0x08) != 0 ? "Serialized" : "NotSerialized";
                int syncLevel = (flags >> 4) & 0x0F;

                sb.AppendLine();
                if (syncLevel > 0)
                    sb.AppendLine($"{pad}Method ({name}, {argCount}, {serialized}, {syncLevel})");
                else
                    sb.AppendLine($"{pad}Method ({name}, {argCount}, {serialized})");

                sb.AppendLine($"{pad}{{");
                openBraces++;
                DecompileStatements(aml, i, blockEnd, indent + 1, sb, ref openBraces, ref closeBraces);
                sb.AppendLine($"{pad}}}");
                closeBraces++;
                i = blockEnd;
                continue;
            }

            // 0x08: NameOp
            if (op == 0x08)
            {
                i++;
                string name = ReadNameString(aml, ref i, end);
                string val = ReadDataRefObject(aml, ref i, end);
                sb.AppendLine($"{pad}Name ({name}, {val})");
                continue;
            }

            // 0x70: StoreOp
            if (op == 0x70)
            {
                i++;
                string src = ReadDataRefObject(aml, ref i, end);
                string dst = ReadNameString(aml, ref i, end);
                sb.AppendLine($"{pad}{dst} = {src}");
                continue;
            }

            lastOpWasIf = false;
            i++;
        }
    }

    private static void DecompileStatements(byte[] aml, int start, int end, int indent, StringBuilder sb, ref int openBraces, ref int closeBraces)
    {
        if (indent > 12) return;
        int i = start;
        string pad = new string(' ', Math.Min(indent, 10) * 4);
        bool lastOpWasIf = false;

        while (i < end)
        {
            byte op = aml[i++];

            if (op == 0x15) // ExternalOp
            {
                lastOpWasIf = false;
                string name = ReadNameString(aml, ref i, end);
                byte objType = i < end ? aml[i++] : (byte)0;
                byte numArgs = i < end ? aml[i++] : (byte)0;
                string typeStr = objType switch
                {
                    1 => "IntObj",
                    2 => "StrObj",
                    3 => "BuffObj",
                    4 => "PkgObj",
                    5 => "FieldUnitObj",
                    6 => "DeviceObj",
                    7 => "EventObj",
                    8 => "MethodObj",
                    9 => "MutexObj",
                    0x0A => "OpRegionObj",
                    0x0B => "PowerResObj",
                    0x0C => "ProcessorObj",
                    0x0D => "ThermalZoneObj",
                    _ => "UnknownObj"
                };
                if (objType == 8 && numArgs > 0)
                    sb.AppendLine($"{pad}External ({name}, {typeStr}, {numArgs})");
                else if (objType > 0)
                    sb.AppendLine($"{pad}External ({name}, {typeStr})");
                else
                    sb.AppendLine($"{pad}External ({name})");
                continue;
            }

            if (op == 0xA4) // ReturnOp
            {
                lastOpWasIf = false;
                string val = ReadDataRefObject(aml, ref i, end);
                sb.AppendLine($"{pad}Return ({val})");
                continue;
            }
            if (op == 0x70) // StoreOp
            {
                lastOpWasIf = false;
                string src = ReadDataRefObject(aml, ref i, end);
                string dst = ReadNameString(aml, ref i, end);
                sb.AppendLine($"{pad}{dst} = {src}");
                continue;
            }
            if (op == 0xA0) // IfOp
            {
                lastOpWasIf = true;
                int blockEnd = ReadPkgEnd(aml, ref i, end);
                string pred = ReadDataRefObject(aml, ref i, blockEnd);
                sb.AppendLine($"{pad}If ({pred})");
                sb.AppendLine($"{pad}{{");
                openBraces++;
                DecompileStatements(aml, i, blockEnd, indent + 1, sb, ref openBraces, ref closeBraces);
                sb.AppendLine($"{pad}}}");
                closeBraces++;
                i = blockEnd;
                continue;
            }
            if (op == 0xA1) // ElseOp
            {
                if (!lastOpWasIf)
                {
                    continue;
                }
                lastOpWasIf = false;
                int blockEnd = ReadPkgEnd(aml, ref i, end);
                sb.AppendLine($"{pad}Else");
                sb.AppendLine($"{pad}{{");
                openBraces++;
                DecompileStatements(aml, i, blockEnd, indent + 1, sb, ref openBraces, ref closeBraces);
                sb.AppendLine($"{pad}}}");
                closeBraces++;
                i = blockEnd;
                continue;
            }
            if (op == 0xA2) // WhileOp
            {
                lastOpWasIf = false;
                int blockEnd = ReadPkgEnd(aml, ref i, end);
                string pred = ReadDataRefObject(aml, ref i, blockEnd);
                sb.AppendLine($"{pad}While ({pred})");
                sb.AppendLine($"{pad}{{");
                openBraces++;
                DecompileStatements(aml, i, blockEnd, indent + 1, sb, ref openBraces, ref closeBraces);
                sb.AppendLine($"{pad}}}");
                closeBraces++;
                i = blockEnd;
                continue;
            }
            if (op == 0x80) // NotifyOp
            {
                lastOpWasIf = false;
                string target = ReadNameString(aml, ref i, end);
                string val = ReadDataRefObject(aml, ref i, end);
                sb.AppendLine($"{pad}Notify ({target}, {val})");
                continue;
            }
            if (op == 0x86) // WaitOp
            {
                lastOpWasIf = false;
                string target = ReadNameString(aml, ref i, end);
                string timeout = ReadIntegerOrTerm(aml, ref i, end);
                sb.AppendLine($"{pad}Wait ({target}, {timeout})");
                continue;
            }
            if (op == 0x87) // ResetOp
            {
                lastOpWasIf = false;
                string target = ReadNameString(aml, ref i, end);
                sb.AppendLine($"{pad}Reset ({target})");
                continue;
            }
            if (op == 0x88) // ReleaseOp
            {
                lastOpWasIf = false;
                string target = ReadNameString(aml, ref i, end);
                sb.AppendLine($"{pad}Release ({target})");
                continue;
            }
            if (op == 0xCC) // BreakOp
            {
                lastOpWasIf = false;
                sb.AppendLine($"{pad}Break");
                continue;
            }
            if (op == 0xCD) // BreakPointOp
            {
                lastOpWasIf = false;
                sb.AppendLine($"{pad}BreakPoint");
                continue;
            }

            // Arithmetic
            if (op == 0x72) // AddOp
            {
                lastOpWasIf = false;
                string a = ReadDataRefObject(aml, ref i, end);
                string b = ReadDataRefObject(aml, ref i, end);
                string target = ReadNameString(aml, ref i, end);
                sb.AppendLine($"{pad}{target} = ({a} + {b})");
                continue;
            }
            if (op == 0x74) // SubtractOp
            {
                lastOpWasIf = false;
                string a = ReadDataRefObject(aml, ref i, end);
                string b = ReadDataRefObject(aml, ref i, end);
                string target = ReadNameString(aml, ref i, end);
                sb.AppendLine($"{pad}{target} = ({a} - {b})");
                continue;
            }
            if (op == 0x75) // IncrementOp
            {
                lastOpWasIf = false;
                string target = ReadNameString(aml, ref i, end);
                sb.AppendLine($"{pad}{target}++");
                continue;
            }
            if (op == 0x76) // DecrementOp
            {
                lastOpWasIf = false;
                string target = ReadNameString(aml, ref i, end);
                sb.AppendLine($"{pad}{target}--");
                continue;
            }
            if (op == 0x7B) // AndOp
            {
                lastOpWasIf = false;
                string a = ReadDataRefObject(aml, ref i, end);
                string b = ReadDataRefObject(aml, ref i, end);
                string target = ReadNameString(aml, ref i, end);
                sb.AppendLine($"{pad}{target} = ({a} & {b})");
                continue;
            }
            if (op == 0x7D) // OrOp
            {
                lastOpWasIf = false;
                string a = ReadDataRefObject(aml, ref i, end);
                string b = ReadDataRefObject(aml, ref i, end);
                string target = ReadNameString(aml, ref i, end);
                sb.AppendLine($"{pad}{target} = ({a} | {b})");
                continue;
            }
            if (op == 0x7F) // XOrOp
            {
                lastOpWasIf = false;
                string a = ReadDataRefObject(aml, ref i, end);
                string b = ReadDataRefObject(aml, ref i, end);
                string target = ReadNameString(aml, ref i, end);
                sb.AppendLine($"{pad}{target} = ({a} ^ {b})");
                continue;
            }

            lastOpWasIf = false;
        }
    }

    private static string ReadDataRefObject(byte[] aml, ref int i, int end)
    {
        if (i >= end) return "Zero";
        byte op = aml[i++];

        switch (op)
        {
            case 0x00: return "Zero";
            case 0x01: return "One";
            case 0xFF: return "Ones";
            case 0x0A: return $"0x{aml[i++]:X2}";
            case 0x0B: return $"0x{ReadWord(aml, ref i, end):X4}";
            case 0x0C: return $"0x{ReadDWord(aml, ref i, end):X8}";
            case 0x0E: return $"0x{ReadQWord(aml, ref i, end):X16}";
            case 0x0D:
                string str = ReadEscapedAsciiString(aml, ref i, end);
                return $"\"{str}\"";
            case 0x11: // Buffer
                int bufEnd = ReadPkgEnd(aml, ref i, end);
                int bufSize = ReadInteger(aml, ref i, bufEnd);
                int byteCount = bufEnd - i;
                string bufContent = "";
                if (byteCount > 0 && byteCount <= 16)
                {
                    var bytesList = new List<string>();
                    while (i < bufEnd)
                    {
                        bytesList.Add($"0x{aml[i++]:X2}");
                    }
                    bufContent = string.Join(", ", bytesList);
                }
                i = bufEnd;
                if (string.IsNullOrEmpty(bufContent))
                    return $"Buffer (0x{bufSize:X2}) {{ }}";
                return $"Buffer (0x{bufSize:X2}) {{ {bufContent} }}";
            case 0x12: // Package
            case 0x13: // VarPackage
                int pkgEnd = ReadPkgEnd(aml, ref i, end);
                byte elements = i < pkgEnd ? aml[i++] : (byte)0;
                i = pkgEnd;
                return $"Package (0x{elements:X2}) {{ }}";
            case 0x90: // LAnd
                return $"({ReadDataRefObject(aml, ref i, end)} && {ReadDataRefObject(aml, ref i, end)})";
            case 0x91: // LOr
                return $"({ReadDataRefObject(aml, ref i, end)} || {ReadDataRefObject(aml, ref i, end)})";
            case 0x92: // LNot
                return $"(!{ReadDataRefObject(aml, ref i, end)})";
            case 0x93: // LEqual
                return $"({ReadDataRefObject(aml, ref i, end)} == {ReadDataRefObject(aml, ref i, end)})";
            case 0x94: // LGreater
                return $"({ReadDataRefObject(aml, ref i, end)} > {ReadDataRefObject(aml, ref i, end)})";
            case 0x95: // LLess
                return $"({ReadDataRefObject(aml, ref i, end)} < {ReadDataRefObject(aml, ref i, end)})";
            case 0x89: // DerefOf
                return $"DerefOf ({ReadDataRefObject(aml, ref i, end)})";
            case 0x71: // RefOf
                return $"RefOf ({ReadNameString(aml, ref i, end)})";
            case 0x60: return "Arg0";
            case 0x61: return "Arg1";
            case 0x62: return "Arg2";
            case 0x63: return "Arg3";
            case 0x64: return "Arg4";
            case 0x65: return "Arg5";
            case 0x66: return "Arg6";
            case 0x68: return "Local0";
            case 0x69: return "Local1";
            case 0x6A: return "Local2";
            case 0x6B: return "Local3";
            case 0x6C: return "Local4";
            case 0x6D: return "Local5";
            case 0x6E: return "Local6";
            case 0x6F: return "Local7";
            default:
                i--;
                string name = ReadNameString(aml, ref i, end);
                if (!string.IsNullOrEmpty(name))
                {
                    if (name.StartsWith("PNP") || name.StartsWith("ACPI"))
                        return $"EisaId (\"{name}\")";
                    return name;
                }
                i++;
                return $"0x{op:X2}";
        }
    }

    private static string ReadIntegerOrTerm(byte[] aml, ref int i, int end)
    {
        if (i >= end) return "0x00";
        byte op = aml[i];
        if (op == 0x00) { i++; return "0x00"; }
        if (op == 0x01) { i++; return "0x01"; }
        if (op == 0x0A) { i++; return $"0x{aml[i++]:X2}"; }
        if (op == 0x0B) { i++; return $"0x{ReadWord(aml, ref i, end):X4}"; }
        if (op == 0x0C) { i++; return $"0x{ReadDWord(aml, ref i, end):X8}"; }
        return ReadDataRefObject(aml, ref i, end);
    }

    private static int ReadInteger(byte[] aml, ref int i, int end)
    {
        if (i >= end) return 0;
        byte op = aml[i++];
        if (op == 0x00) return 0;
        if (op == 0x01) return 1;
        if (op == 0x0A && i < end) return aml[i++];
        if (op == 0x0B && i + 1 < end) return ReadWord(aml, ref i, end);
        if (op == 0x0C && i + 3 < end) return (int)ReadDWord(aml, ref i, end);
        return op;
    }

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

    private static string ReadNameString(byte[] aml, ref int i, int end)
    {
        if (i >= end) return "";
        var sb = new StringBuilder();

        if (aml[i] == '\\') { sb.Append('\\'); i++; }
        while (i < end && aml[i] == '^') { sb.Append('^'); i++; }

        if (i >= end) return sb.ToString();

        if (aml[i] == 0x00) // NullName
        {
            i++;
            return sb.ToString();
        }

        if (aml[i] == 0x2E) // DualNamePrefix
        {
            i++;
            string seg1 = ReadNameSeg(aml, ref i, end);
            string seg2 = ReadNameSeg(aml, ref i, end);
            sb.Append(seg1).Append('.').Append(seg2);
            return sb.ToString();
        }

        if (aml[i] == 0x2F) // MultiNamePrefix
        {
            i++;
            byte count = i < end ? aml[i++] : (byte)0;
            for (int c = 0; c < count; c++)
            {
                if (c > 0) sb.Append('.');
                sb.Append(ReadNameSeg(aml, ref i, end));
            }
            return sb.ToString();
        }

        sb.Append(ReadNameSeg(aml, ref i, end));
        return sb.ToString();
    }

    private static string ReadNameSeg(byte[] aml, ref int i, int end)
    {
        if (i + 4 > end)
        {
            i = end;
            return "UNKN";
        }

        char[] chars = new char[4];
        bool valid = true;
        for (int k = 0; k < 4; k++)
        {
            byte b = aml[i + k];
            if ((b >= 'A' && b <= 'Z') || (b >= '0' && b <= '9') || b == '_')
            {
                chars[k] = (char)b;
            }
            else if (b >= 'a' && b <= 'z')
            {
                chars[k] = char.ToUpperInvariant((char)b);
            }
            else
            {
                valid = false;
                break;
            }
        }
        i += 4;
        return valid ? new string(chars) : "UNKN";
    }

    private static ushort ReadWord(byte[] aml, ref int i, int end)
    {
        if (i + 2 > end) { i = end; return 0; }
        ushort val = BitConverter.ToUInt16(aml, i);
        i += 2;
        return val;
    }

    private static uint ReadDWord(byte[] aml, ref int i, int end)
    {
        if (i + 4 > end) { i = end; return 0; }
        uint val = BitConverter.ToUInt32(aml, i);
        i += 4;
        return val;
    }

    private static ulong ReadQWord(byte[] aml, ref int i, int end)
    {
        if (i + 8 > end) { i = end; return 0; }
        ulong val = BitConverter.ToUInt64(aml, i);
        i += 8;
        return val;
    }

    private static string ReadEscapedAsciiString(byte[] aml, ref int i, int end)
    {
        var sb = new StringBuilder();
        while (i < end && aml[i] != 0x00)
        {
            byte b = aml[i++];
            if (b >= 32 && b <= 126)
            {
                if (b == '"') sb.Append("\\\"");
                else if (b == '\\') sb.Append("\\\\");
                else sb.Append((char)b);
            }
            else
            {
                sb.Append($"\\x{b:X2}");
            }
        }
        if (i < end && aml[i] == 0x00) i++;
        return sb.ToString();
    }

    private static string ReadFixedAscii(byte[] aml, int start, int length)
    {
        if (start + length > aml.Length) return "";
        var sb = new StringBuilder(length);
        for (int k = 0; k < length; k++)
        {
            byte b = aml[start + k];
            if (b >= 32 && b <= 126) sb.Append((char)b);
            else sb.Append(' ');
        }
        return sb.ToString();
    }

    private static string CleanString(string s) => s.Trim();
}
