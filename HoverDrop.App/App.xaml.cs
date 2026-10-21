using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using HoverDrop.App.Hooks;
using HoverDrop.App.Services;
using HoverDrop.App.Utils;
using HoverDrop.App.ViewModels;

namespace HoverDrop.App
{
    public partial class App : Application
    {
        private const string MutexName = "HoverDrop.SingleInstance";
        private const string ShowSettingsEventName = "HoverDrop.ShowSettings";

        private Mutex? _mutex;
        private InputHookService? _inputHookService;
        private SettingsWindow? _settingsWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Приложение живёт в фоне и закрывается только явно (кнопка «Выйти»)
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mutex = new Mutex(true, MutexName, out bool isFirstInstance);
            if (!isFirstInstance)
            {
                // Уже запущено: просим работающий экземпляр показать настройки и выходим
                try { EventWaitHandle.OpenExisting(ShowSettingsEventName).Set(); } catch { }
                Shutdown();
                return;
            }

            AppSettings.Load();
            AppSettings.StartWithWindows = AutostartService.IsEnabled();
            ThemeService.Apply();

            _inputHookService = new InputHookService();
            _inputHookService.ShowOverlayRequested += OnShowOverlay;
            _inputHookService.HideOverlayRequested += OnHideOverlay;
            _inputHookService.Start();

            // Окно меню создаём, но не показываем
            MainWindow = new MainWindow();

            // Ждём сигнала от повторного запуска
            var showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
            var waiter = new Thread(() =>
            {
                while (true)
                {
                    showSettingsEvent.WaitOne();
                    Dispatcher.BeginInvoke(ShowSettings);
                }
            })
            { IsBackground = true };
            waiter.Start();

            // При автозапуске окно настроек не показываем
            if (!e.Args.Contains("--autostart"))
                ShowSettings();
        }

        private void ShowSettings()
        {
            if (_settingsWindow == null)
            {
                _settingsWindow = new SettingsWindow();
                _settingsWindow.Closed += (_, _) => _settingsWindow = null;
                _settingsWindow.Show();
            }
            else
            {
                if (_settingsWindow.WindowState == WindowState.Minimized)
                    _settingsWindow.WindowState = WindowState.Normal;
                _settingsWindow.Activate();
            }
        }

        private void OnShowOverlay(Point cursorPosition)
        {
            if (MainWindow.IsVisible) return;

            double size = OverlayViewModel.Center * 2 * AppSettings.MenuScale;
            MainWindow.Width = size;
            MainWindow.Height = size;

            var dpi = VisualTreeHelper.GetDpi(MainWindow);

            // Снимок и размытие фона под меню: строго ДО показа окна
            if (MainWindow.DataContext is OverlayViewModel vm)
            {
                int sizePx = (int)Math.Round(MainWindow.Width * dpi.DpiScaleX);
                vm.GlassImage = ScreenGlass.CaptureBlurred((int)cursorPosition.X, (int)cursorPosition.Y, sizePx);
            }

            // cursorPosition в физических пикселях, Left/Top окна в DIP
            MainWindow.Left = cursorPosition.X / dpi.DpiScaleX - MainWindow.Width / 2;
            MainWindow.Top = cursorPosition.Y / dpi.DpiScaleY - MainWindow.Height / 2;

            MainWindow.Show();
        }

        // Пересоздаёт окно оверлея (после смены темы WPF-UI портит фон старого окна)
        public void RecreateOverlay()
        {
            // При старте окна ещё нет, оно создаётся в OnStartup
            if (MainWindow == null) return;

            var old = MainWindow;
            MainWindow = new MainWindow();
            old.Close();
        }

        private void OnHideOverlay()
        {
            if (MainWindow == null) return;
            MainWindow.Hide();
            (MainWindow.DataContext as OverlayViewModel)?.Reset();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _inputHookService?.Stop();
            base.OnExit(e);
        }
    }
}