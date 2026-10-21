using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HoverDrop.App.Models;
using HoverDrop.App.Services;
using HoverDrop.App.Utils;
using HoverDrop.App.ViewModels;

namespace HoverDrop.App
{
    public partial class MainWindow : Window
    {
        private OverlayViewModel ViewModel => (OverlayViewModel)DataContext;
        private bool _isClosingAnimated;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0) ViewModel.LoadActionsForFiles(files);
                e.Effects = PickEffect(e.AllowedEffects);
            }
            e.Handled = true;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = PickEffect(e.AllowedEffects);
                UpdateHoverFromCursor();
            }
            else e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    // Пересчитываем наведение прямо в момент дропа
                    UpdateHoverFromCursor();
                    var hovered = ViewModel.GetHoveredAction();

                    await HideAnimatedAsync();

                    // Дроп над центром или мимо кольца: отмена
                    if (hovered != null)
                    {
                        var action = hovered.Type;

                        // Подтверждение удаления ДО выполнения действия
                        if (action == FileActionType.Delete && AppSettings.ConfirmDelete && !ConfirmDelete(files))
                            return;

                        if (files.Length == 1)
                            await FileActionService.ExecuteActionAsync(files[0], action);
                        else
                            await FileActionService.ExecuteGroupActionAsync(files, action);

                        if (AppSettings.PlaySound) System.Media.SystemSounds.Beep.Play();
                    }
                    return;
                }
            }

            await HideAnimatedAsync();
        }

        public async Task HideAnimatedAsync()
        {
            if (!IsVisible || _isClosingAnimated) return;
            _isClosingAnimated = true;
            IsHitTestVisible = false;

            try
            {
                var animOpacity = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(90)));
                var animScale = new DoubleAnimation(1, 0.90, new Duration(TimeSpan.FromMilliseconds(90)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                var transform = (ScaleTransform)RootRing.RenderTransform;
                RootRing.BeginAnimation(UIElement.OpacityProperty, animOpacity);
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, animScale);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, animScale);

                await Task.Delay(90);
            }
            catch
            {
                // Если анимация прервана
            }
            finally
            {
                Hide();
                RootRing.Opacity = 1;
                var transform = (ScaleTransform)RootRing.RenderTransform;
                transform.ScaleX = 1;
                transform.ScaleY = 1;
                IsHitTestVisible = true;
                _isClosingAnimated = false;
                ViewModel.Reset();
            }
        }

        private static bool ConfirmDelete(string[] files)
        {
            string text = files.Length == 1
                ? LocalizationService.ConfirmDeleteSingle(Path.GetFileName(files[0].TrimEnd('\\', '/')))
                : LocalizationService.ConfirmDeleteGroup(files.Length);

            return MessageBox.Show(text, "HoverDrop", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        // Считаем по экранным координатам (физические пиксели): не зависит от DPI и от e.GetPosition
        private void UpdateHoverFromCursor()
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            Point cursorPx = MouseHelper.GetCursorPosition();
            Point centerPx = PointToScreen(new Point(ActualWidth / 2, ActualHeight / 2));

            double k = ActualWidth / (OverlayViewModel.Center * 2);
            double dx = (cursorPx.X - centerPx.X) / dpi.DpiScaleX / k;
            double dy = (cursorPx.Y - centerPx.Y) / dpi.DpiScaleY / k;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            double angle = Math.Atan2(dy, dx) * (180 / Math.PI) + 90; // 0° строго вверх
            if (angle < 0) angle += 360;

            ViewModel.UpdateHover(angle, distance);
        }

        private static DragDropEffects PickEffect(DragDropEffects allowed)
        {
            if ((allowed & DragDropEffects.Copy) != 0) return DragDropEffects.Copy;
            if ((allowed & DragDropEffects.Link) != 0) return DragDropEffects.Link;
            if ((allowed & DragDropEffects.Move) != 0) return DragDropEffects.Move;
            return DragDropEffects.None;
        }
    }
}
