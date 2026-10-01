using System;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;

namespace XRAY_ULTIMATE.Helpers;

public static class WindowHelper
{
    public static IntPtr MainWindowHandle { get; set; }

    public static void InitializePicker(FileSavePicker picker)
    {
        if (MainWindowHandle != IntPtr.Zero)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, MainWindowHandle);
        }
    }
}
