using System.Runtime.InteropServices;
using System.Windows;namespace HoverDrop.App.Utils
{
    public static class MouseHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }        
        
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint); 

        public static Point GetCursorPosition()
        {
            GetCursorPos(out POINT point);
            return new Point(point.X, point.Y);
        }
    }
}