using System;
using System.IO;

namespace HoverDrop.App.Utils
{
    // Временный лог в %APPDATA%\HoverDrop\log.txt
    public static class Log
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HoverDrop", "log.txt");
        private static readonly object Sync = new();

        public static void Write(string message)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                    File.AppendAllText(FilePath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
                }
            }
            catch { }
        }
    }
}