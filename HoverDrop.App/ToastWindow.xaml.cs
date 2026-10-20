using System.Windows;

namespace HoverDrop.App
{
    public partial class ToastWindow : Window
    {
        public ToastWindow()
        {
            InitializeComponent();
        }

        public void SetTitle(string text) => TitleText.Text = text;
        public void SetStatus(string text) => StatusText.Text = text;

        // null = неопределённый прогресс, иначе доля от 0 до 1
        public void SetProgress(double? fraction)
        {
            if (fraction == null)
            {
                Bar.IsIndeterminate = true;
            }
            else
            {
                Bar.IsIndeterminate = false;
                Bar.Value = fraction.Value * 100;
            }
        }

        public void PlaceBottomRight()
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - Width - 8;
            Top = area.Bottom - Height - 8;
        }
    }
}