using SharpHook;
using SharpHook.Data;
using System;
using System.Windows;
using HoverDrop.App.Utils;

namespace HoverDrop.App.Hooks
{
    public class InputHookService : IDisposable
    {
        private readonly TaskPoolGlobalHook _hook;
        private volatile bool _isLButtonDown;
        private volatile bool _cancelled;
        private volatile bool _isTriggerDown;
        private bool _disposed;
        public bool Paused { get; set; }

        public event Action<Point>? ShowOverlayRequested;
        public event Action? HideOverlayRequested;

        public InputHookService()
        {
            _hook = new TaskPoolGlobalHook();
            _hook.MousePressed += OnMousePressed;
            _hook.MouseReleased += OnMouseReleased;
            _hook.KeyPressed += OnKeyPressed;
            _hook.KeyReleased += OnKeyReleased;
        }

        public void Start() => _hook.RunAsync();

        public void Stop() => Dispose();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _hook.Dispose();
        }

        private void OnMousePressed(object? sender, MouseHookEventArgs e)
        {
            if (e.Data.Button == MouseButton.Button1)
            {
                _isLButtonDown = true;
                _cancelled = false;
                CheckAndFireEvent();
            }
        }

        private void OnMouseReleased(object? sender, MouseHookEventArgs e)
        {
            if (e.Data.Button == MouseButton.Button1)
            {
                _isLButtonDown = false;
                // Не прячем сразу: окну нужно время, чтобы получить Drop.
                // Если Drop прошёл, окно скроется само; это страховка на случай отмены (Esc и т.п.).
                _ = Task.Delay(400).ContinueWith(_ =>
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!_isLButtonDown) HideOverlayRequested?.Invoke();
                    }));
            }
        }

        private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            if (e.Data.KeyCode == KeyCode.VcEscape)
            {
                // Esc отменяет текущее перетаскивание: прячем меню и не показываем его снова до следующего нажатия ЛКМ
                if (_isLButtonDown && _isTriggerDown)
                {
                    _cancelled = true;
                    Application.Current.Dispatcher.Invoke(() => HideOverlayRequested?.Invoke());
                }
                return;
            }

            if (IsMatchingTriggerKey(e.Data.KeyCode))
            {
                // Проверяем, не была ли кнопка УЖЕ нажата (защита от автоповтора Windows)
                if (!_isTriggerDown)
                {
                    _isTriggerDown = true;
                    CheckAndFireEvent();
                }
            }
        }

        private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
        {
            if (IsMatchingTriggerKey(e.Data.KeyCode))
            {
                // Сбрасываем флаг только при реальном отпускании клавиши
                if (_isTriggerDown)
                {
                    _isTriggerDown = false;
                    CheckAndFireEvent();
                }
            }
        }

        // Вспомогательный метод перевода наших настроек в коды SharpHook
        private bool IsMatchingTriggerKey(KeyCode code)
        {
            return AppSettings.TriggerKey switch
            {
                TriggerModifier.Alt => code == KeyCode.VcLeftAlt || code == KeyCode.VcRightAlt,
                TriggerModifier.Ctrl => code == KeyCode.VcLeftControl || code == KeyCode.VcRightControl,
                TriggerModifier.Shift => code == KeyCode.VcLeftShift || code == KeyCode.VcRightShift,
                _ => false
            };
        }

        private void CheckAndFireEvent()
        {
            if (_isLButtonDown && _isTriggerDown && !_cancelled && !Paused)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var cursorPosition = MouseHelper.GetCursorPosition();
                    ShowOverlayRequested?.Invoke(cursorPosition);
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    HideOverlayRequested?.Invoke();
                });
            }
        }
    }
}
