using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HoverDrop.App.Services;
using HoverDrop.App.Utils;
using System;
using ThemeMode = HoverDrop.App.Utils.ThemeMode;

namespace HoverDrop.App.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty] private int _themeIndex;
        [ObservableProperty] private int _triggerIndex;
        [ObservableProperty] private bool _startWithWindows;
        [ObservableProperty] private bool _playSound;
        [ObservableProperty] private bool _confirmDelete;
        [ObservableProperty] private double _menuScalePercent;
        [ObservableProperty] private bool _autoUpdate;
        [ObservableProperty] private string _updateStatus = "";
        [ObservableProperty] private string _updateButtonText = "Проверить";

        public string VersionText => $"Версия {UpdateService.Instance.CurrentVersion}";

        public SettingsViewModel()
        {
            // Пишем в поля напрямую, чтобы не вызывать сохранение при загрузке
            _themeIndex = (int)AppSettings.Theme;
            _triggerIndex = (int)AppSettings.TriggerKey;
            _startWithWindows = AutostartService.IsEnabled();
            _playSound = AppSettings.PlaySound;
            _confirmDelete = AppSettings.ConfirmDelete;
            _menuScalePercent = AppSettings.MenuScale * 100;

            _autoUpdate = AppSettings.AutoUpdate;
            if (UpdateService.Instance.ReadyVersion is { } ready)
            {
                _updateStatus = $"Доступна версия {ready}";
                _updateButtonText = "Установить и перезапустить";
            }
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

            UpdateStatus = "Проверяю…";
            switch (await svc.CheckAsync())
            {
                case UpdateCheckResult.Ready:
                    UpdateStatus = $"Доступна версия {svc.ReadyVersion}";
                    UpdateButtonText = "Установить и перезапустить";
                    break;
                case UpdateCheckResult.UpToDate:
                    UpdateStatus = "У вас последняя версия";
                    break;
                case UpdateCheckResult.NotInstalled:
                    UpdateStatus = "Обновления работают только в установленной версии";
                    break;
                default:
                    UpdateStatus = "Не удалось проверить обновления";
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