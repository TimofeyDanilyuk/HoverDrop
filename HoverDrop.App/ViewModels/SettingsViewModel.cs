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
        [ObservableProperty] private bool _startWithWindows;
        [ObservableProperty] private bool _playSound;

        public SettingsViewModel()
        {
            // Пишем в поля напрямую, чтобы не вызывать сохранение при загрузке
            _themeIndex = (int)AppSettings.Theme;
            _triggerIndex = (int)AppSettings.TriggerKey;
            _startWithWindows = AutostartService.IsEnabled();
            _playSound = AppSettings.PlaySound;
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