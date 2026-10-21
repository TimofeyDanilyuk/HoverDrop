using System;
using System.IO;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace HoverDrop.App.Services
{
    // Значок в системном трее с меню: настройки, пауза, обновления, выход
    public sealed class TrayService : IDisposable
    {
        private readonly WinForms.NotifyIcon _icon;
        private readonly WinForms.ContextMenuStrip _menu;
        private readonly WinForms.ToolStripMenuItem _pauseItem;
        private readonly WinForms.ToolStripMenuItem _updateItem;
        private bool _updateReady;

        public event Action? OpenSettingsRequested;
        public event Action<bool>? PausedChanged;
        public event Action? ExitRequested;
        public event Action? CheckUpdatesRequested;
        public event Action? InstallUpdateRequested;

        public TrayService()
        {
            _menu = new WinForms.ContextMenuStrip();

            var openItem = new WinForms.ToolStripMenuItem("Настройки");
            openItem.Font = new Drawing.Font(_menu.Font, Drawing.FontStyle.Bold);
            openItem.Click += (_, _) => OpenSettingsRequested?.Invoke();

            _pauseItem = new WinForms.ToolStripMenuItem("Приостановить HoverDrop") { CheckOnClick = true };
            _pauseItem.CheckedChanged += (_, _) =>
            {
                _icon!.Text = _pauseItem.Checked ? "HoverDrop (приостановлен)" : "HoverDrop";
                PausedChanged?.Invoke(_pauseItem.Checked);
            };

            _updateItem = new WinForms.ToolStripMenuItem("Проверить обновления");
            _updateItem.Click += (_, _) =>
            {
                if (_updateReady) InstallUpdateRequested?.Invoke();
                else CheckUpdatesRequested?.Invoke();
            };

            var exitItem = new WinForms.ToolStripMenuItem("Выйти из HoverDrop");
            exitItem.Click += (_, _) => ExitRequested?.Invoke();

            _menu.Items.Add(openItem);
            _menu.Items.Add(_pauseItem);
            _menu.Items.Add(_updateItem);
            _menu.Items.Add(new WinForms.ToolStripSeparator());
            _menu.Items.Add(exitItem);

            _icon = new WinForms.NotifyIcon
            {
                Text = "HoverDrop",
                Icon = LoadIcon(),
                ContextMenuStrip = _menu,
                Visible = true
            };
            _icon.MouseClick += (_, e) =>
            {
                if (e.Button == WinForms.MouseButtons.Left) OpenSettingsRequested?.Invoke();
            };
            // Клик по уведомлению «Доступно обновление» ставит его
            _icon.BalloonTipClicked += (_, _) =>
            {
                if (_updateReady) InstallUpdateRequested?.Invoke();
            };
        }

        public void SetUpdateReady(string version)
        {
            _updateReady = true;
            _updateItem.Text = $"Установить обновление {version} и перезапустить";
            _updateItem.Font = new Drawing.Font(_menu.Font, Drawing.FontStyle.Bold);
            _icon.ShowBalloonTip(8000, "Доступно обновление",
                $"Версия {version} загружена. Нажмите, чтобы установить и перезапустить.",
                WinForms.ToolTipIcon.Info);
        }

        public void ShowInfo(string text) =>
            _icon.ShowBalloonTip(4000, "HoverDrop", text, WinForms.ToolTipIcon.Info);

        private static Drawing.Icon LoadIcon()
        {
            var res = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/HoverDrop.ico"));
            using Stream stream = res.Stream;
            return new Drawing.Icon(stream, WinForms.SystemInformation.SmallIconSize);
        }

        public void Dispose()
        {
            _icon.Visible = false; // иначе «призрак» иконки остаётся в трее до наведения мыши
            _icon.Dispose();
            _menu.Dispose();
        }
    }
}