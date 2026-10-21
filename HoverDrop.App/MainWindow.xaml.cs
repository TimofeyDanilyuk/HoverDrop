using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using HoverDrop.App.Models;
using HoverDrop.App.Services;
using HoverDrop.App.Utils;
using HoverDrop.App.ViewModels;

namespace HoverDrop.App
{
    public partial class MainWindow : Window
    {
        private OverlayViewModel ViewModel => (OverlayViewModel)DataContext;
        private readonly List<Grid> _sectorGrids = new();
        private bool _isClosingAnimated;
        private bool _hasPlayedShow;

        public MainWindow()
        {
            InitializeComponent();
            CenterHub.RenderTransform = new ScaleTransform(1, 1);
        }

        private void SectorGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Grid g && !_sectorGrids.Contains(g))
            {
                // Создаем индивидуальный НЕзамороженный ScaleTransform строго из центра (200, 200)
                var scale = new ScaleTransform(0, 0, 200, 200);
                g.RenderTransform = scale;
                g.Opacity = 0;
                _sectorGrids.Add(g);

                if (g.DataContext is FileAction action)
                {
                    action.PropertyChanged += (s, args) =>
                    {
                        if (args.PropertyName == nameof(FileAction.IsHovered) && !_isClosingAnimated)
                        {
                            OnSectorHoverChanged(g, scale, action.IsHovered);
                        }
                    };
                }
            }
        }

        private static void OnSectorHoverChanged(Grid g, ScaleTransform scale, bool isHovered)
        {
            Panel.SetZIndex(g, isHovered ? 10 : 0);

            double targetScale = isHovered ? 1.025 : 1.0;
            var anim = new DoubleAnimation(targetScale, new Duration(TimeSpan.FromMilliseconds(100)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        private void SectorGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Grid g)
            {
                _sectorGrids.Remove(g);
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    bool wasEmpty = ViewModel.Actions.Count == 0;
                    ViewModel.LoadActionsForFiles(files);
                    if (wasEmpty || !_hasPlayedShow)
                    {
                        _hasPlayedShow = true;
                        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, PlayShowAnimation);
                    }
                }
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

                    Hide();
                    ResetOverlayState();

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

        public void PlayShowAnimation()
        {
            // 1. Анимация появления центра
            var centerEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
            var centerScale = new DoubleAnimation(0.7, 1.0, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = centerEase
            };
            var centerOpacity = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(160)));
            CenterHub.BeginAnimation(UIElement.OpacityProperty, centerOpacity);
            ((ScaleTransform)CenterHub.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, centerScale);
            ((ScaleTransform)CenterHub.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, centerScale);

            // 2. Каскадная анимация секторов ПО ЧАСОВОЙ СТРЕЛКЕ (0, 1, 2, ...)
            int count = _sectorGrids.Count;
            for (int i = 0; i < count; i++)
            {
                var grid = _sectorGrids[i];
                if (grid.RenderTransform is not ScaleTransform scale) continue;

                int delayMs = i * 20; // Задержка каждого следующего сектора по часовой стрелке

                var animOpacity = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    BeginTime = TimeSpan.FromMilliseconds(delayMs),
                    Duration = new Duration(TimeSpan.FromMilliseconds(130))
                };

                var animScale = new DoubleAnimation
                {
                    From = 0.70,
                    To = 1.0,
                    BeginTime = TimeSpan.FromMilliseconds(delayMs),
                    Duration = new Duration(TimeSpan.FromMilliseconds(190)),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.32 }
                };

                grid.BeginAnimation(UIElement.OpacityProperty, animOpacity);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, animScale);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, animScale);
            }
        }

        public async Task HideAnimatedAsync()
        {
            if (!IsVisible || _isClosingAnimated) return;
            _isClosingAnimated = true;
            IsHitTestVisible = false;

            int count = _sectorGrids.Count;
            int maxDuration = 90;

            if (count > 0)
            {
                // Сворачивание секторов ПРОТИВ ЧАСОВОЙ СТРЕЛКИ (count - 1, count - 2, ..., 0)
                for (int i = 0; i < count; i++)
                {
                    int reverseIndex = count - 1 - i;
                    if (reverseIndex >= _sectorGrids.Count) continue;
                    var grid = _sectorGrids[reverseIndex];
                    if (grid.RenderTransform is not ScaleTransform scale) continue;

                    int delayMs = i * 20; // Против часовой стрелки
                    int totalMs = delayMs + 70;
                    if (totalMs > maxDuration) maxDuration = totalMs;

                    var animOpacity = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        BeginTime = TimeSpan.FromMilliseconds(delayMs),
                        Duration = new Duration(TimeSpan.FromMilliseconds(70))
                    };

                    var animScale = new DoubleAnimation
                    {
                        From = scale.ScaleX,
                        To = 0.75,
                        BeginTime = TimeSpan.FromMilliseconds(delayMs),
                        Duration = new Duration(TimeSpan.FromMilliseconds(70)),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };

                    grid.BeginAnimation(UIElement.OpacityProperty, animOpacity);
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, animScale);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, animScale);
                }
            }

            var centerOpacity = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(maxDuration)));
            CenterHub.BeginAnimation(UIElement.OpacityProperty, centerOpacity);

            await Task.Delay(maxDuration + 20);

            Hide();
            ResetOverlayState();
        }

        private void ResetOverlayState()
        {
            CenterHub.BeginAnimation(UIElement.OpacityProperty, null);
            CenterHub.Opacity = 1;
            ((ScaleTransform)CenterHub.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, null);
            ((ScaleTransform)CenterHub.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, null);
            ((ScaleTransform)CenterHub.RenderTransform).ScaleX = 1;
            ((ScaleTransform)CenterHub.RenderTransform).ScaleY = 1;

            foreach (var grid in _sectorGrids)
            {
                grid.BeginAnimation(UIElement.OpacityProperty, null);
                grid.Opacity = 1;
                if (grid.RenderTransform is ScaleTransform scale)
                {
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    scale.ScaleX = 1;
                    scale.ScaleY = 1;
                }
            }

            _sectorGrids.Clear();
            _hasPlayedShow = false;
            IsHitTestVisible = true;
            _isClosingAnimated = false;
            ViewModel.Reset();
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
