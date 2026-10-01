# WinUI 3 Design System & Layout Guidelines

## Overview

All 18 views in **XRAY ULTIMATE** adhere to a unified UI/UX design standard modeled after modern Microsoft Windows 11 Fluent design principles (Task Manager, Windows Settings, and Dev Home).

---

## 1. Universal Layout Hierarchy

Every view must implement the following outer container structure:

```xml
<Page
    x:Class="XRAY_ULTIMATE.Views.ExamplePage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled" Padding="28,20,28,40">
        <StackPanel Spacing="24" HorizontalAlignment="Stretch">
            <!-- [1] Header Banner Card -->
            <!-- [2] Top KPI Metric Cards (Grid ColumnDefinitions="*,*,*") -->
            <!-- [3] Content Sections / Lists / Expanders -->
            <!-- [4] Grouped Properties Matrices -->
        </StackPanel>
    </ScrollViewer>
</Page>
```

### Critical Rules:
1. **Never use `HorizontalAlignment="Left"` on the root `StackPanel`**: `HorizontalAlignment="Left"` gives child controls infinite horizontal measure space, causing grids to collapse to their text content and producing mismatched card widths.
2. **Never use restrictive `MaxWidth` (e.g. `MaxWidth="1400"`) with `HorizontalAlignment="Stretch"`**: In WinUI 3, combining `HorizontalAlignment="Stretch"` with a `MaxWidth` smaller than the window causes the element to **center** on wide screens. This creates horizontal shifting and centering drift when navigating between pages.
3. **Always set `HorizontalScrollBarVisibility="Disabled"`**: Prevents accidental horizontal scrolling gestures on touchpad and ensures controls wrap cleanly.

---

## 2. Standard Header Banner Card

The top banner card establishes visual identity and context:

```xml
<Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
        BorderThickness="1"
        CornerRadius="12"
        Padding="24,20">
    <Grid ColumnDefinitions="Auto,*,Auto">
        <!-- 64x64 Accent Icon Badge -->
        <Border Background="{ThemeResource AccentFillColorDefaultBrush}" CornerRadius="12" Width="64" Height="64" Margin="0,0,20,0" VerticalAlignment="Center">
            <FontIcon Glyph="&#xE950;" FontSize="32" Foreground="White" />
        </Border>

        <!-- Title, Subtitle, and Live Badge Pills -->
        <StackPanel Grid.Column="1" Spacing="4" VerticalAlignment="Center">
            <TextBlock Text="Primary Title" FontSize="22" FontWeight="Bold" />
            <TextBlock Text="Technical subtitle and subsystem description" FontSize="13" Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
            <StackPanel Orientation="Horizontal" Spacing="16" Margin="0,4,0,0">
                <Border Background="{ThemeResource AccentFillColorDefaultBrush}" CornerRadius="4" Padding="8,2" VerticalAlignment="Center">
                    <TextBlock Text="Status Pill" FontSize="11" FontWeight="Bold" Foreground="White" />
                </Border>
            </StackPanel>
        </StackPanel>

        <!-- Quick Action Buttons -->
        <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="8" VerticalAlignment="Center">
            <Button Content="Quick Action" />
        </StackPanel>
    </Grid>
</Border>
```

---

## 3. Large Multi-line Text Viewports (Code & Logs)

When displaying large text (e.g., ASL disassembly with 39,000+ lines, raw diagnostic logs, or hex dumps):
- **Never wrap `TextBox` in an outer `ScrollViewer`**: In WinUI 3 / DirectX, measuring a `TextBox` with infinite height creates a swapchain surface exceeding the DirectX 16,384-pixel texture limit, clipping all text past line ~800.
- **Set bounded height and internal scrolling on the `TextBox` directly**:
  ```xml
  <Border Background="#090d14"
          BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
          BorderThickness="1"
          CornerRadius="10"
          Padding="16">
      <TextBox x:Name="TxtSource"
               Height="600"
               IsReadOnly="True"
               AcceptsReturn="True"
               TextWrapping="NoWrap"
               FontFamily="Consolas"
               FontSize="12"
               Foreground="#e2e8f0"
               Background="Transparent"
               BorderThickness="0"
               ScrollViewer.VerticalScrollBarVisibility="Visible"
               ScrollViewer.HorizontalScrollBarVisibility="Auto"
               ScrollViewer.VerticalScrollMode="Enabled"
               ScrollViewer.HorizontalScrollMode="Enabled" />
  </Border>
  ```

---

## 4. Virtualization & Nested Collections

- **Avoid nested `ListView` controls within the same scroll axis**: In WinUI 3, putting a `ListView` inside an `Expander` inside another `ListView` causes the inner `ListView` to collapse to 0 height during layout passes.
- **Use `ItemsControl` for grouped content**: `ItemsControl` generates child elements without virtualized viewport measurement conflicts, rendering all cards and expanders reliably.
