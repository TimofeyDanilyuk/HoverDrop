using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HoverDrop.App.Services
{
    public static class ToastService
    {
        public static ToastHandle Begin(string title) => new(title);
    }

    // Тост, который появляется только если действие идёт дольше 0,6 с
    public sealed class ToastHandle
    {
        private readonly Dispatcher _d = Application.Current.Dispatcher;
        private readonly string _title;
        private ToastWindow? _win;
        private bool _done;
        private double? _progress;
        private string _status = LocalizationService.InProgress;

        internal ToastHandle(string title)
        {
            _title = title;
            _ = ShowLaterAsync();
        }

        private async Task ShowLaterAsync()
        {
            await Task.Delay(600);
            await _d.InvokeAsync(() =>
            {
                if (_done) return;
                _win = new ToastWindow();
                _win.SetTitle(_title);
                Apply();
                _win.Show();
                _win.PlaceBottomRight();
            });
        }

        private void Apply()
        {
            _win?.SetStatus(_status);
            _win?.SetProgress(_progress);
        }

        // Можно вызывать из любого потока
        public void Report(double fraction)
        {
            _d.InvokeAsync(() =>
            {
                if (_done) return;
                _progress = Math.Clamp(fraction, 0, 1);
                _status = $"{(int)(_progress * 100)}%";
                Apply();
            });
        }

        public void Complete(string? message = null)
        {
            _d.InvokeAsync(() =>
            {
                _done = true;
                var w = _win;
                if (w == null) return;
                w.SetStatus(message ?? LocalizationService.Done);
                w.SetProgress(1);
                _ = CloseLater(w);
            });
        }

        public void Fail()
        {
            _d.InvokeAsync(() =>
            {
                _done = true;
                _win?.Close();
            });
        }

        private static async Task CloseLater(ToastWindow w)
        {
            await Task.Delay(1500);
            w.Close();
        }
    }
}
