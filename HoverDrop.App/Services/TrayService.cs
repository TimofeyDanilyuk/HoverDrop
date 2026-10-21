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
        private readonly WinForms.ToolStripMenuItem _openItem;
        private readonly WinForms.ToolStripMenuItem _pauseItem;
        private readonly WinForms.ToolStripMenuItem _updateItem;
        private readonly WinForms.ToolStripMenuItem _exitItem;
        private bool _updateReady;
        private string? _readyVersion;

        public event Action? OpenSettingsRequested;
        public event Action<bool>? PausedChanged;
        public event Action? ExitRequested;
        public event Action? CheckUpdatesRequested;
        public event Action? InstallUpdateRequested;

        public TrayService()
        {
            _menu = new WinForms.ContextMenuStrip();

            _openItem = new WinForms.ToolStripMenuItem(LocalizationService.TraySettings);
            _openItem.Font = new Drawing.Font(_menu.Font, Drawing.FontStyle.Bold);
            _openItem.Click += (_, _) => OpenSettingsRequested?.Invoke();

            _pauseItem = new WinForms.ToolStripMenuItem(LocalizationService.TrayPause) { CheckOnClick = true };
            _pauseItem.CheckedChanged += (_, _) =>
            {
                UpdateTrayText();
                PausedChanged?.Invoke(_pauseItem.Checked);
            };

            _updateItem = new WinForms.ToolStripMenuItem(LocalizationService.TrayCheckUpdates);
            _updateItem.Click += (_, _) =>
            {
                if (_updateReady) InstallUpdateRequested?.Invoke();
                else CheckUpdatesRequested?.Invoke();
            };

            _exitItem = new WinForms.ToolStripMenuItem(LocalizationService.TrayExit);
            _exitItem.Click += (_, _) => ExitRequested?.Invoke();

            _menu.Items.Add(_openItem);
            _menu.Items.Add(_pauseItem);
            _menu.Items.Add(_updateItem);
            _menu.Items.Add(new WinForms.ToolStripSeparator());
            _menu.Items.Add(_exitItem);

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

            LocalizationService.LanguageChanged += UpdateLabels;
        }

        private void UpdateTrayText()
        {
            _icon.Text = _pauseItem.Checked ? LocalizationService.TrayPausedTitle : "HoverDrop";
        }

        private void UpdateLabels()
        {
            _openItem.Text = LocalizationService.TraySettings;
            _pauseItem.Text = LocalizationService.TrayPause;
            _exitItem.Text = LocalizationService.TrayExit;
            UpdateTrayText();

            if (_updateReady && _readyVersion != null)
            {
                _updateItem.Text = LocalizationService.TrayInstallUpdate(_readyVersion);
            }
            else
            {
                _updateItem.Text = LocalizationService.TrayCheckUpdates;
            }
        }

        public void SetUpdateReady(string version)
        {
            _updateReady = true;
            _readyVersion = version;
            _updateItem.Text = LocalizationService.TrayInstallUpdate(version);
            _updateItem.Font = new Drawing.Font(_menu.Font, Drawing.FontStyle.Bold);
            _icon.ShowBalloonTip(8000, LocalizationService.UpdateReadyTitle,
                LocalizationService.UpdateReadyBody(version),
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
            LocalizationService.LanguageChanged -= UpdateLabels;
            _icon.Visible = false; // иначе «призрак» иконки остаётся в трее до наведения мыши
            _icon.Dispose();
            _menu.Dispose();
        }
    }
}
