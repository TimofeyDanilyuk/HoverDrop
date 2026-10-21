using System;
using System.Globalization;
using HoverDrop.App.Models;
using HoverDrop.App.Utils;

namespace HoverDrop.App.Services
{
    public enum AppLanguage
    {
        System = 0,
        Russian = 1,
        English = 2
    }

    public static class LocalizationService
    {
        public static event Action? LanguageChanged;

        public static void NotifyLanguageChanged() => LanguageChanged?.Invoke();

        public static bool IsRussian
        {
            get
            {
                if (AppSettings.Language == AppLanguage.Russian) return true;
                if (AppSettings.Language == AppLanguage.English) return false;
                var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
                return lang is "ru" or "be" or "uk" or "kk";
            }
        }

        public static string GetActionTitle(FileActionType action) => (action, IsRussian) switch
        {
            (FileActionType.Delete, true) => "Удалить",
            (FileActionType.Delete, false) => "Delete",

            (FileActionType.CopyPath, true) => "Копировать путь",
            (FileActionType.CopyPath, false) => "Copy path",

            (FileActionType.CopyPaths, true) => "Копировать пути",
            (FileActionType.CopyPaths, false) => "Copy paths",

            (FileActionType.MakeCopy, true) => "Сделать копию",
            (FileActionType.MakeCopy, false) => "Make a copy",

            (FileActionType.Desktop, true) => "На рабочий стол",
            (FileActionType.Desktop, false) => "To Desktop",

            (FileActionType.ToZip, true) => "В ZIP-архив",
            (FileActionType.ToZip, false) => "To ZIP archive",

            (FileActionType.ExtractHere, true) => "Распаковать сюда",
            (FileActionType.ExtractHere, false) => "Extract here",

            (FileActionType.ExtractToFolder, true) => "Распаковать в папку",
            (FileActionType.ExtractToFolder, false) => "Extract to folder",

            (FileActionType.CollectToFolder, true) => "Собрать в папку",
            (FileActionType.CollectToFolder, false) => "Group into folder",

            (FileActionType.ToJpg, true) => "В JPG",
            (FileActionType.ToJpg, false) => "To JPG",

            (FileActionType.ToPng, true) => "В PNG",
            (FileActionType.ToPng, false) => "To PNG",

            (FileActionType.ToIco, true) => "В ICO",
            (FileActionType.ToIco, false) => "To ICO",

            (FileActionType.ToPdf, true) => "В PDF",
            (FileActionType.ToPdf, false) => "To PDF",

            (FileActionType.Blur, true) => "Размыть",
            (FileActionType.Blur, false) => "Blur",

            (FileActionType.Grayscale, true) => "Обесцветить",
            (FileActionType.Grayscale, false) => "Grayscale",

            (FileActionType.ToMp3Audio, true) => "В MP3 (Аудио)",
            (FileActionType.ToMp3Audio, false) => "To MP3 (Audio)",

            (FileActionType.ToMp3, true) => "В MP3",
            (FileActionType.ToMp3, false) => "To MP3",

            (FileActionType.ToWav, true) => "В WAV",
            (FileActionType.ToWav, false) => "To WAV",

            (FileActionType.ToGif, true) => "В GIF",
            (FileActionType.ToGif, false) => "To GIF",

            (FileActionType.Compress720p, true) => "Сжать (720p)",
            (FileActionType.Compress720p, false) => "Compress (720p)",

            (FileActionType.Compress64k, true) => "Сжать (64kbps)",
            (FileActionType.Compress64k, false) => "Compress (64kbps)",

            (FileActionType.RemoveAudio, true) => "Удалить звук",
            (FileActionType.RemoveAudio, false) => "Remove audio",

            _ => action.ToString()
        };

        // Тексты оверлея
        public static string ReleaseFile => IsRussian ? "Отпустите файл" : "Release file";
        public static string FilesCount(int count) => IsRussian ? $"{count} файлов" : $"{count} files";

        public static string ConfirmDeleteSingle(string name) => IsRussian
            ? $"Переместить в корзину «{name}»?"
            : $"Move \"{name}\" to recycle bin?";

        public static string ConfirmDeleteGroup(int count) => IsRussian
            ? $"Переместить в корзину выбранное ({count} шт.)?"
            : $"Move {count} selected items to recycle bin?";

        // Тосты и сообщения
        public static string InProgress => IsRussian ? "Выполняется…" : "Processing…";
        public static string Done => IsRussian ? "Готово" : "Done";
        public static string NewFolder => IsRussian ? "Новая папка" : "New folder";
        public static string ArchivePrefix => IsRussian ? "Архив" : "Archive";

        public static string ErrorProcessing(string msg) => IsRussian
            ? $"Ошибка при обработке файла: {msg}"
            : $"Error processing file: {msg}";

        public static string PartialErrors => IsRussian
            ? "Не всё удалось обработать:\n\n"
            : "Some items could not be processed:\n\n";

        // Трей
        public static string TraySettings => IsRussian ? "Настройки" : "Settings";
        public static string TrayPause => IsRussian ? "Приостановить HoverDrop" : "Pause HoverDrop";
        public static string TrayPausedTitle => IsRussian ? "HoverDrop (приостановлен)" : "HoverDrop (paused)";
        public static string TrayCheckUpdates => IsRussian ? "Проверить обновления" : "Check for updates";
        public static string TrayInstallUpdate(string v) => IsRussian
            ? $"Установить обновление {v} и перезапустить"
            : $"Install update {v} and restart";
        public static string TrayExit => IsRussian ? "Выйти из HoverDrop" : "Exit HoverDrop";
        public static string UpdateReadyTitle => IsRussian ? "Доступно обновление" : "Update available";
        public static string UpdateReadyBody(string v) => IsRussian
            ? $"Версия {v} загружена. Нажмите, чтобы установить и перезапустить."
            : $"Version {v} is downloaded. Click to install and restart.";

        // Настройки
        public static string SettingsTitle => IsRussian ? "Настройки" : "Settings";
        public static string SettingsWindowHeader => IsRussian ? "Настройки HoverDrop" : "HoverDrop Settings";

        public static string ThemeTitle => IsRussian ? "Тема оформления" : "Appearance theme";
        public static string ThemeHint => IsRussian ? "Влияет на это окно и на цвет «стекла» в меню" : "Affects this window and the glass tint in overlay";

        public static string HotkeyTitle => IsRussian ? "Горячая клавиша" : "Trigger key";
        public static string HotkeyHint => IsRussian
            ? "Удерживай её при перетаскивании файла, чтобы открыть меню. Alt мешает меньше всего."
            : "Hold it while dragging a file to open the menu. Alt is recommended.";

        public static string MenuScaleTitle => IsRussian ? "Размер меню" : "Menu size";
        public static string MenuScaleHint => IsRussian ? "Применится при следующем появлении меню" : "Applies on next overlay show";

        public static string LanguageTitle => IsRussian ? "Язык интерфейса" : "Interface language";
        public static string LanguageHint => IsRussian ? "Язык меню действий и окна настроек" : "Language of actions and settings window";

        public static string AutostartTitle => IsRussian ? "Запускать вместе с Windows" : "Start with Windows";
        public static string AutostartHint => IsRussian ? "Приложение стартует в фоне, без этого окна" : "Starts minimized to system tray";

        public static string SoundTitle => IsRussian ? "Звук после действия" : "Sound after action";
        public static string SoundHint => IsRussian ? "Короткий сигнал, когда действие выполнено" : "Short sound cue upon completion";

        public static string UpdatesTitle => IsRussian ? "Обновления" : "Updates";
        public static string AutoUpdateTitle => IsRussian ? "Проверять обновления автоматически" : "Check for updates automatically";
        public static string AutoUpdateHint => IsRussian ? "Обновление скачивается само, а установить решаете вы" : "Downloads in background, you decide when to restart";

        public static string ConfirmDeleteTitle => IsRussian ? "Подтверждать удаление" : "Confirm deletion";
        public static string ConfirmDeleteHint => IsRussian ? "Спрашивать перед тем, как отправить файл в корзину" : "Ask before moving files to recycle bin";

        public static string FooterHint => IsRussian
            ? "Закрытие этого окна не останавливает HoverDrop: он продолжает работать в фоне. Настройки можно открыть из значка в системном трее."
            : "Closing this window does not quit HoverDrop: it continues running in the tray.";

        public static string ExitButton => IsRussian ? "Выйти из HoverDrop" : "Exit HoverDrop";
        public static string CheckButton => IsRussian ? "Проверить" : "Check";
        public static string CheckingUpdates => IsRussian ? "Проверяю…" : "Checking…";
        public static string UpdateUpToDate => IsRussian ? "У вас последняя версия" : "You have the latest version";
        public static string UpdateNotInstalled => IsRussian ? "Обновления работают только в установленной версии" : "Updates only work in installed version";
        public static string UpdateError => IsRussian ? "Не удалось проверить обновления" : "Failed to check for updates";
        public static string UpdateAvailable(string v) => IsRussian ? $"Доступна версия {v}" : $"Version {v} available";
        public static string UpdateInstallRestart => IsRussian ? "Установить и перезапустить" : "Install and restart";
    }
}

