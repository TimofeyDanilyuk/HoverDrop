using System;
using Microsoft.Win32;

namespace HoverDrop.App.Services
{
    public static class AutostartService
    {
        private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "HoverDrop";

        public static bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue(ValueName) != null;
        }

        public static void SetEnabled(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true)
                            ?? Registry.CurrentUser.CreateSubKey(KeyPath);

            if (enable)
                key.SetValue(ValueName, $"\"{Environment.ProcessPath}\" --autostart");
            else
                key.DeleteValue(ValueName, false);
        }
    }
}