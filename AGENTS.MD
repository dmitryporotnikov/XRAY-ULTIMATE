# Developer & AI Agent Guidelines for XRAY ULTIMATE

This guide provides technical context, architecture rules, build instructions, and development patterns for AI agents (Opencode, Claude, Cursor, Copilot, etc.) working on the **XRAY ULTIMATE** codebase.

---

## 1. Project Overview & Environment

- **Solution Type**: WinUI 3 Desktop Application (Windows App SDK 1.6 / 2.5)
- **Target Framework**: `net10.0-windows10.0.19041.0`
- **Target Architecture**: `x64` (strictly 64-bit; hardware inspection requires x64 kernel APIs)
- **Application Model**: Unpackaged Win32 Native (`<WindowsPackageType>None</WindowsPackageType>`)
- **Process Elevation**: `requireAdministrator` (configured in `app.manifest`)

---

## 2. Mandatory Build & Compilation Rules

### Build Command
Always pass the explicit platform parameter:
```powershell
dotnet build -p:Platform=x64
```
*Never* run `dotnet build` without `-p:Platform=x64`, as the project targets x64 explicitly and will default to AnyCPU or fail assembly resolution.

### Locked Executable Handling
Because the application runs elevated, if an instance of `XRAY ULTIMATE.exe` is running, `dotnet build` will fail with `MSB3026: The process cannot access the file because it is being used by another process`.
- Check running instances: `Get-Process "XRAY*" -ErrorAction SilentlyContinue`
- If running under another user token or admin: Rename the existing binary before building:
  ```powershell
  $old = "bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\XRAY ULTIMATE.exe"
  if (Test-Path $old) { Move-Item -LiteralPath $old -Destination "$old.old.$([Guid]::NewGuid().ToString('N'))" -Force }
  ```

---

## 3. UI/UX Rules & WinUI 3 Layout Standards

### Outer Container Architecture
All 18 views must strictly adhere to the unified layout:
```xml
<Page ...>
    <ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled" Padding="28,20,28,40">
        <StackPanel Spacing="24" HorizontalAlignment="Stretch">
            <!-- Header Banner Card -->
            <!-- Content Elements -->
        </StackPanel>
    </ScrollViewer>
</Page>
```

### Prohibited Patterns:
- ❌ **DO NOT use `HorizontalAlignment="Left"` on the root `StackPanel`**: This breaks child `*` column measurements in infinite space and makes tabs appear misaligned.
- ❌ **DO NOT use `MaxWidth="1400"` with `HorizontalAlignment="Stretch"`**: WinUI 3 centers any stretched control with a restrictive `MaxWidth`. This causes pages to jump or drift to the center on wide displays.
- ❌ **DO NOT nest `ListView` inside `ListView` or `Expander`**: Nested virtualized collections collapse to height 0. Always use `ItemsControl` for outer collections or class groups (e.g. `DeviceManagerPage`, `NetworkPage`, `SecurityPage`).
- ❌ **DO NOT wrap large multi-line `TextBox` inside a `ScrollViewer`**: DirectWrite and DirectX clip swapchain surfaces exceeding 16,384 pixels. Apply `Height="580"` and `ScrollViewer.VerticalScrollBarVisibility="Visible"` directly to the `TextBox` to enable native virtualization.

---

## 4. Security & Win32 Interop Rules

### In-Process File Dialogs (Never Use UWP Pickers)
- ❌ **DO NOT use `Windows.Storage.Pickers.FileSavePicker` or `FileOpenPicker`**: In an unpackaged application running with administrative elevation (`requireAdministrator`), Windows UIPI blocks cross-integrity COM calls to the medium-integrity `explorer.exe` shell broker. This causes pickers to crash, throw `COMException`, or silently fail.
- ✔️ **ALWAYS use `NativeFileDialog.SaveFile(...)`**: Located in `Helpers/NativeFileDialog.cs`. It invokes the in-process Win32 `IFileSaveDialog` (`CLSID_FileSaveDialog`) with classic `comdlg32.dll` fallback.

### Privileged WMI Queries
- All WMI queries should route through `Services/WmiService.cs` or configure:
  ```csharp
  var scope = new ManagementScope(@"\\.\root\CIMV2")
  {
      Options = {
          Authentication = AuthenticationLevel.PacketPrivacy,
          Impersonation = ImpersonationLevel.Impersonate
      }
  };
  ```

### Windows Services Control
- Use `Helpers/ServiceManagerHelper.cs` to start, stop, restart, or change startup modes via Win32 `advapi32.dll` (`ChangeServiceConfig` and `System.ServiceProcess`).

---

## 5. ACPI & DSDT Decompiler Standards

When maintaining `Services/AcpiDisassembler.cs`:
1. **Package Length**: In the ACPI specification, `PkgLength` includes the lead and trailing length bytes. Always use `ReadPkgEnd` (`pkgStart + length`) to calculate boundaries.
2. **Name Segments**: Name segments are strictly 4 characters (`[A-Z0-9_]`). Never strip trailing underscores (e.g., `_SB_` must remain `_SB_`).
3. **External Declarations**: Support `0x15` (`ExternalOp`) for ACPICA compatibility.
4. **Brace Balance**: Every block opened (`{`) must have an exact matching closing brace (`}`). Mathematical balance must always equal 0.
5. **No Control Characters**: Output must be 100% clean UTF-8 text with zero unprintable control characters.

---

## 6. How to Add a New Page / Tab

1. Create `Views/NewFeaturePage.xaml` and `Views/NewFeaturePage.xaml.cs`.
2. Follow the container standard: `ScrollViewer (Disabled)` -> `StackPanel (Stretch)`.
3. Add the header banner card with a 64×64 accent badge and subtitle.
4. Add the navigation item in `MainWindow.xaml` inside `NavView.MenuItems`:
   ```xml
   <NavigationViewItem x:Name="NavNewFeature" Content="New Feature" Tag="NewFeature">
       <NavigationViewItem.Icon>
           <FontIcon Glyph="&#xE...;" />
       </NavigationViewItem.Icon>
   </NavigationViewItem>
   ```
5. Register the type switch in `MainWindow.xaml.cs` (`NavView_SelectionChanged`).
6. Run `dotnet build -p:Platform=x64` to verify clean compilation.
