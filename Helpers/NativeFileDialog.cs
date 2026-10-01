using System;
using System.IO;
using System.Runtime.InteropServices;

namespace XRAY_ULTIMATE.Helpers;

public static class NativeFileDialog
{
    [ComImport]
    [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileSaveDialog
    {
        [PreserveSig]
        int Show([In] IntPtr parent);
        void SetFileTypes([In] uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
        void SetFileTypeIndex([In] uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise([In, MarshalAs(UnmanagedType.Interface)] IntPtr pfde, out uint pdwCookie);
        void Unadvise([In] uint dwCookie);
        void SetOptions([In] uint fos);
        void GetOptions(out uint pfos);
        void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
        void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
        void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, int fdap);
        void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close([MarshalAs(UnmanagedType.Error)] int hr);
        void SetClientGuid([In] ref Guid guid);
        void ClearClientData();
        void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
        void SetSaveAsItem([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
        void SetProperties([In, MarshalAs(UnmanagedType.Interface)] IntPtr pStore);
        void SetCollectedProperties([In, MarshalAs(UnmanagedType.Interface)] IntPtr pList, [In] int fAppendDefault);
        void GetProperties([MarshalAs(UnmanagedType.Interface)] out IntPtr ppStore);
        void ApplyProperties([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In, MarshalAs(UnmanagedType.Interface)] IntPtr pStore, [In] IntPtr hwnd, [In, MarshalAs(UnmanagedType.Interface)] IntPtr pSink);
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
        void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void GetDisplayName([In] uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);
        void Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct COMDLG_FILTERSPEC
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszSpec;
    }

    [ComImport]
    [Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B")]
    [ClassInterface(ClassInterfaceType.None)]
    private class FileSaveDialogRCW { }

    private const uint FOS_OVERWRITEPROMPT = 0x00000002;
    private const uint FOS_FORCEFILESYSTEM = 0x00000040;
    private const uint FOS_PATHMUSTEXIST = 0x00000800;
    private const uint SIGDN_FILESYSPATH = 0x80058000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct OPENFILENAME
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public IntPtr lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetSaveFileName([In, Out] ref OPENFILENAME ofn);

    private const int OFN_OVERWRITEPROMPT = 0x00000002;
    private const int OFN_PATHMUSTEXIST = 0x00000800;

    public static string? SaveFile(string title, string defaultFileName, string defaultExtension, params (string Description, string Pattern)[] filters)
    {
        IntPtr ownerHwnd = WindowHelper.MainWindowHandle;

        // Try modern IFileSaveDialog first
        try
        {
            var dialog = (IFileSaveDialog)new FileSaveDialogRCW();
            dialog.SetTitle(title);
            dialog.SetFileName(defaultFileName);
            if (!string.IsNullOrEmpty(defaultExtension))
            {
                dialog.SetDefaultExtension(defaultExtension.TrimStart('.'));
            }

            if (filters != null && filters.Length > 0)
            {
                var specs = new COMDLG_FILTERSPEC[filters.Length];
                for (int i = 0; i < filters.Length; i++)
                {
                    specs[i] = new COMDLG_FILTERSPEC
                    {
                        pszName = filters[i].Description,
                        pszSpec = filters[i].Pattern
                    };
                }
                dialog.SetFileTypes((uint)specs.Length, specs);
                dialog.SetFileTypeIndex(1);
            }

            dialog.SetOptions(FOS_OVERWRITEPROMPT | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST);

            int hr = dialog.Show(ownerHwnd);
            if (hr == 0) // S_OK
            {
                dialog.GetResult(out IShellItem item);
                if (item != null)
                {
                    item.GetDisplayName(SIGDN_FILESYSPATH, out string path);
                    return path;
                }
            }
            return null; // User cancelled
        }
        catch
        {
            // Fallback to classic GetSaveFileName
            return SaveFileWithClassicDialog(ownerHwnd, title, defaultFileName, defaultExtension, filters);
        }
    }

    private static string? SaveFileWithClassicDialog(IntPtr ownerHwnd, string title, string defaultFileName, string defaultExtension, (string Description, string Pattern)[] filters)
    {
        try
        {
            var ofn = new OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.hwndOwner = ownerHwnd;
            ofn.lpstrTitle = title;
            ofn.lpstrDefExt = defaultExtension.TrimStart('.');
            ofn.Flags = OFN_OVERWRITEPROMPT | OFN_PATHMUSTEXIST;

            // Build filter string formatted with null terminators
            string filterStr = "";
            if (filters != null && filters.Length > 0)
            {
                foreach (var f in filters)
                {
                    filterStr += $"{f.Description}\0{f.Pattern}\0";
                }
            }
            else
            {
                filterStr = "All Files (*.*)\0*.*\0";
            }
            filterStr += "\0";
            ofn.lpstrFilter = filterStr;

            int maxFileLength = 1024;
            IntPtr fileBuffer = Marshal.StringToHGlobalAuto(defaultFileName.PadRight(maxFileLength, '\0'));
            ofn.lpstrFile = fileBuffer;
            ofn.nMaxFile = maxFileLength;

            try
            {
                if (GetSaveFileName(ref ofn))
                {
                    return Marshal.PtrToStringAuto(ofn.lpstrFile);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(fileBuffer);
            }
        }
        catch { }

        return null;
    }
}
