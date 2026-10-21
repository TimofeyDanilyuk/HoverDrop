using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using HoverDrop.App.Hooks;
using Velopack;
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
        private TrayService? _tray;

        [STAThread]
        private static void Main(string[] args)
        {
            // Velopack обрабатывает вызовы установщика/обновления и при необходимости завершает процесс
            VelopackApp.Build().Run();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (_, ev) =>
            {
                ev.Handled = true;
            };

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
            var hook = _inputHookService;
            _tray = new TrayService();
            _tray.OpenSettingsRequested += ShowSettings;
            _tray.PausedChanged += paused => hook.Paused = paused;
            _tray.ExitRequested += Shutdown;
            _tray.CheckUpdatesRequested += CheckUpdatesManually;
            _tray.InstallUpdateRequested += UpdateService.Instance.InstallAndRestart;
            UpdateService.Instance.UpdateReady += v => Dispatcher.BeginInvoke(() => _tray?.SetUpdateReady(v));
            _ = AutoUpdateLoopAsync();

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
            if (MainWindow == null) return;

            var old = MainWindow;
            MainWindow = new MainWindow();
            old.Close();
        }

        private void OnHideOverlay()
        {
            if (MainWindow is MainWindow mw)
            {
                _ = mw.HideAnimatedAsync();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tray?.Dispose();
            _inputHookService?.Stop();
            base.OnExit(e);
        }

        // Проверка через 20 с после старта, потом раз в 6 часов, пока обновление не найдено
        private async Task AutoUpdateLoopAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(20));
            while (UpdateService.Instance.ReadyVersion == null)
            {
                if (AppSettings.AutoUpdate) await UpdateService.Instance.CheckAsync();
                await Task.Delay(TimeSpan.FromHours(6));
            }
        }

        private async void CheckUpdatesManually()
        {
            var result = await UpdateService.Instance.CheckAsync();
            string? text = result switch
            {
                UpdateCheckResult.UpToDate => LocalizationService.UpdateUpToDate,
                UpdateCheckResult.NotInstalled => LocalizationService.UpdateNotInstalled,
                UpdateCheckResult.Error => LocalizationService.UpdateError,
                _ => null // Ready: уведомление покажет сам трей
            };
            if (text != null) _tray?.ShowInfo(text);
        }
    }
}
