using System;
using System.Windows;
using System.Windows.Media;
using HoverDrop.App.Utils;
using HoverDrop.App.ViewModels;

namespace HoverDrop.App
{
    public partial class MainWindow : Window
    {
        private OverlayViewModel ViewModel => (OverlayViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
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
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    // Пересчитываем наведение прямо в момент дропа
                    UpdateHoverFromCursor();
                    string? actionTitle = ViewModel.GetHoveredAction()?.Title;

                    System.Diagnostics.Debug.WriteLine($"[HoverDrop] Drop: '{actionTitle}' -> {files.Length} шт., первый: {files[0]}");

                    Hide();
                    ViewModel.Reset();

                    // Дроп над центром или мимо кольца: отмена
                    if (actionTitle != null)
                    {
                        if (files.Length == 1)
                            await Services.FileActionService.ExecuteActionAsync(files[0], actionTitle);
                        else
                            await Services.FileActionService.ExecuteGroupActionAsync(files, actionTitle);

                        if (AppSettings.PlaySound) System.Media.SystemSounds.Beep.Play();
                        if (actionTitle == "Удалить" && AppSettings.ConfirmDelete && !ConfirmDelete(files)) return;
                    }
                    return;
                }
            }

            Hide();
            ViewModel.Reset();
        }

        private static bool ConfirmDelete(string[] files)
        {
            string text = files.Length == 1
                ? $"Переместить в корзину «{System.IO.Path.GetFileName(files[0].TrimEnd('\\', '/'))}»?"
                : $"Переместить в корзину выбранное ({files.Length} шт.)?";

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