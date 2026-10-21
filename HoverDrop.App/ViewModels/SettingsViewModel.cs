using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HoverDrop.App.Services;
using HoverDrop.App.Utils;
using ThemeMode = HoverDrop.App.Utils.ThemeMode;

namespace HoverDrop.App.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty] private int _themeIndex;
        [ObservableProperty] private int _triggerIndex;
        [ObservableProperty] private int _languageIndex;
        [ObservableProperty] private bool _startWithWindows;
        [ObservableProperty] private bool _playSound;
        [ObservableProperty] private bool _confirmDelete;
        [ObservableProperty] private double _menuScalePercent;
        [ObservableProperty] private bool _autoUpdate;
        [ObservableProperty] private string _updateStatus = "";
        [ObservableProperty] private string _updateButtonText = LocalizationService.CheckButton;

        public string VersionText => LocalizationService.IsRussian
            ? $"Версия {UpdateService.Instance.CurrentVersion}"
            : $"Version {UpdateService.Instance.CurrentVersion}";

        public string SettingsHeader => LocalizationService.SettingsTitle;
        public string ThemeTitle => LocalizationService.ThemeTitle;
        public string ThemeHint => LocalizationService.ThemeHint;
        public string HotkeyTitle => LocalizationService.HotkeyTitle;
        public string HotkeyHint => LocalizationService.HotkeyHint;
        public string MenuScaleTitle => LocalizationService.MenuScaleTitle;
        public string MenuScaleHint => LocalizationService.MenuScaleHint;
        public string LanguageTitle => LocalizationService.LanguageTitle;
        public string LanguageHint => LocalizationService.LanguageHint;
        public string AutostartTitle => LocalizationService.AutostartTitle;
        public string AutostartHint => LocalizationService.AutostartHint;
        public string SoundTitle => LocalizationService.SoundTitle;
        public string SoundHint => LocalizationService.SoundHint;
        public string UpdatesTitle => LocalizationService.UpdatesTitle;
        public string AutoUpdateTitle => LocalizationService.AutoUpdateTitle;
        public string AutoUpdateHint => LocalizationService.AutoUpdateHint;
        public string ConfirmDeleteTitle => LocalizationService.ConfirmDeleteTitle;
        public string ConfirmDeleteHint => LocalizationService.ConfirmDeleteHint;
        public string FooterHint => LocalizationService.FooterHint;
        public string ExitButton => LocalizationService.ExitButton;

        public SettingsViewModel()
        {
            // Пишем в поля напрямую, чтобы не вызывать сохранение при загрузке
            _themeIndex = (int)AppSettings.Theme;
            _triggerIndex = (int)AppSettings.TriggerKey;
            _languageIndex = (int)AppSettings.Language;
            _startWithWindows = AutostartService.IsEnabled();
            _playSound = AppSettings.PlaySound;
            _confirmDelete = AppSettings.ConfirmDelete;
            _menuScalePercent = AppSettings.MenuScale * 100;

            _autoUpdate = AppSettings.AutoUpdate;
            if (UpdateService.Instance.ReadyVersion is { } ready)
            {
                _updateStatus = LocalizationService.UpdateAvailable(ready);
                _updateButtonText = LocalizationService.UpdateInstallRestart;
            }
        }

        partial void OnLanguageIndexChanged(int value)
        {
            AppSettings.Language = (AppLanguage)value;
            AppSettings.Save();
            LocalizationService.NotifyLanguageChanged();
            UpdateButtonText = UpdateService.Instance.ReadyVersion != null
                ? LocalizationService.UpdateInstallRestart
                : LocalizationService.CheckButton;
            OnPropertyChanged(string.Empty); // обновить все bindings
        }

        partial void OnAutoUpdateChanged(bool value)
        {
            AppSettings.AutoUpdate = value;
            AppSettings.Save();
        }

        [RelayCommand]
        private async Task CheckUpdates()
        {
            var svc = UpdateService.Instance;
            if (svc.ReadyVersion != null) { svc.InstallAndRestart(); return; }

            UpdateStatus = LocalizationService.CheckingUpdates;
            switch (await svc.CheckAsync())
            {
                case UpdateCheckResult.Ready:
                    UpdateStatus = LocalizationService.UpdateAvailable(svc.ReadyVersion!);
                    UpdateButtonText = LocalizationService.UpdateInstallRestart;
                    break;
                case UpdateCheckResult.UpToDate:
                    UpdateStatus = LocalizationService.UpdateUpToDate;
                    break;
                case UpdateCheckResult.NotInstalled:
                    UpdateStatus = LocalizationService.UpdateNotInstalled;
                    break;
                default:
                    UpdateStatus = LocalizationService.UpdateError;
                    break;
            }
        }

        partial void OnMenuScalePercentChanged(double value)
        {
            AppSettings.MenuScale = Math.Clamp(value / 100.0, 0.7, 1.5);
            AppSettings.Save();
        }

        partial void OnConfirmDeleteChanged(bool value)
        {
            AppSettings.ConfirmDelete = value;
            AppSettings.Save();
        }

        partial void OnThemeIndexChanged(int value)
        {
            AppSettings.Theme = (ThemeMode)value;
            ThemeService.Apply();
            AppSettings.Save();
        }

        partial void OnTriggerIndexChanged(int value)
        {
            AppSettings.TriggerKey = (TriggerModifier)value;
            AppSettings.Save();
        }

        partial void OnStartWithWindowsChanged(bool value)
        {
            AutostartService.SetEnabled(value);
            AppSettings.StartWithWindows = value;
            AppSettings.Save();
        }

        partial void OnPlaySoundChanged(bool value)
        {
            AppSettings.PlaySound = value;
            AppSettings.Save();
        }

        [RelayCommand]
        private void Exit() => Application.Current.Shutdown();
    }
}
