using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HoverDrop.App.Utils
{
    // Достаёт «родную» иконку файла/папки из оболочки Windows (48x48)
    public static class ShellIcon
    {
        private const uint SHGFI_SYSICONINDEX = 0x4000;
        private const int SHIL_EXTRALARGE = 2;
        private const int ILD_TRANSPARENT = 1;
        private static Guid IID_IImageList = new("46EB5926-582E-4017-9FDF-E8998DAA0950");

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }

        [ComImport, Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig] int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
            [PreserveSig] int ReplaceIcon(int i, IntPtr hicon, ref int pi);
            [PreserveSig] int SetOverlayImage(int iImage, int iOverlay);
            [PreserveSig] int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
            [PreserveSig] int AddMasked(IntPtr hbmImage, int crMask, ref int pi);
            [PreserveSig] int Draw(IntPtr pimldp);
            [PreserveSig] int Remove(int i);
            [PreserveSig] int GetIcon(int i, int flags, ref IntPtr picon);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
            ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("shell32.dll", PreserveSig = false)]
        private static extern void SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static ImageSource? Get(string path)
        {
            try
            {
                var info = new SHFILEINFO();
                if (SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_SYSICONINDEX) == IntPtr.Zero)
                    return null;

                SHGetImageList(SHIL_EXTRALARGE, ref IID_IImageList, out var list);
                try
                {
                    IntPtr hIcon = IntPtr.Zero;
                    if (list.GetIcon(info.iIcon, ILD_TRANSPARENT, ref hIcon) != 0 || hIcon == IntPtr.Zero)
                        return null;
                    try
                    {
                        var src = Imaging.CreateBitmapSourceFromHIcon(
                            hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        src.Freeze();
                        return src;
                    }
                    finally { DestroyIcon(hIcon); }
                }
                finally { Marshal.ReleaseComObject(list); }
            }
            catch
            {
                return null; // не вышло: останется иконка SymbolRegular
            }
        }
    }
}