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

        public SettingsViewModel()
        {
            // Пишем в поля напрямую, чтобы не вызывать сохранение при загрузке
            _themeIndex = (int)AppSettings.Theme;
            _triggerIndex = (int)AppSettings.TriggerKey;
            _startWithWindows = AutostartService.IsEnabled();
            _playSound = AppSettings.PlaySound;
            _confirmDelete = AppSettings.ConfirmDelete;
            _menuScalePercent = AppSettings.MenuScale * 100;
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