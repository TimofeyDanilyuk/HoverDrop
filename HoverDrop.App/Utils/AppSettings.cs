using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HoverDrop.App.Utils
{
    public enum TriggerModifier { Alt, Ctrl, Shift }

    // Порядок важен: он совпадает с порядком пунктов в ComboBox
    public enum ThemeMode { System, Light, Dark }

    public static class AppSettings
    {
        public static ThemeMode Theme { get; set; } = ThemeMode.System;
        public static TriggerModifier TriggerKey { get; set; } = TriggerModifier.Alt;
        public static bool StartWithWindows { get; set; }
        public static bool PlaySound { get; set; } = true;
        public static double MenuScale { get; set; } = 1.0;
        public static bool ConfirmDelete { get; set; } = true;

        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HoverDrop", "settings.json");

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private sealed class Data
        {
            public ThemeMode Theme { get; set; } = ThemeMode.System;
            public TriggerModifier TriggerKey { get; set; } = TriggerModifier.Alt;
            public bool ConfirmDelete { get; set; } = true;
            public bool StartWithWindows { get; set; }
            public bool PlaySound { get; set; } = true;
            public double MenuScale { get; set; } = 1.0;
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                var data = JsonSerializer.Deserialize<Data>(File.ReadAllText(FilePath), Options);
                if (data == null) return;

                Theme = data.Theme;
                TriggerKey = data.TriggerKey;
                StartWithWindows = data.StartWithWindows;
                PlaySound = data.PlaySound;
                MenuScale = Math.Clamp(data.MenuScale, 0.7, 1.5);
                ConfirmDelete = data.ConfirmDelete;
            }
            catch
            {
                // Битый файл настроек не должен мешать запуску: остаются значения по умолчанию
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                var data = new Data
                {
                    Theme = Theme,
                    TriggerKey = TriggerKey,
                    StartWithWindows = StartWithWindows,
                    PlaySound = PlaySound,
                    ConfirmDelete = ConfirmDelete,
                    MenuScale = MenuScale
                };
                File.WriteAllText(FilePath, JsonSerializer.Serialize(data, Options));
            }
            catch
            {
                // Не удалось сохранить: не падаем
            }
        }
    }
}