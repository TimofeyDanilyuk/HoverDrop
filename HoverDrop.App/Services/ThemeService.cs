using System;
using System.Windows;
using HoverDrop.App.Utils;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using ThemeMode = HoverDrop.App.Utils.ThemeMode;

namespace HoverDrop.App.Services
{
    public static class ThemeService
    {
        private static ResourceDictionary? _overlayPalette;

        // Применяет тему к окнам WPF-UI и подменяет палитру «стекла» в меню
        public static void Apply()
        {
            bool dark = AppSettings.Theme switch
            {
                ThemeMode.Dark => true,
                ThemeMode.Light => false,
                _ => IsSystemDark()
            };

            ApplicationThemeManager.Apply(dark ? ApplicationTheme.Dark : ApplicationTheme.Light);

            var merged = Application.Current.Resources.MergedDictionaries;
            if (_overlayPalette != null) merged.Remove(_overlayPalette);

            _overlayPalette = new ResourceDictionary
            {
                Source = new Uri(dark ? "Themes/OverlayDark.xaml" : "Themes/OverlayLight.xaml", UriKind.Relative)
            };
            merged.Add(_overlayPalette);

            // WPF-UI испортил фон уже созданного оверлея, поэтому делаем новое окно
            (Application.Current as App)?.RecreateOverlay();
        }

        private static bool IsSystemDark()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
    }
}