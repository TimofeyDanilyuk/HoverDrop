using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace HoverDrop.App.Models
{
    public partial class FileAction : ObservableObject
    {
        public string Title { get; }
        public SymbolRegular Icon { get; }

        // Размытый снимок фона для «стеклянной» заливки (общий для всех секторов)
        public ImageSource? Glass { get; set; }

        // Готовая геометрия сектора
        [ObservableProperty]
        private Geometry? _shape;

        // Центр сектора (для иконки + подписи)
        [ObservableProperty]
        private double _textX;

        [ObservableProperty]
        private double _textY;

        [ObservableProperty]
        private bool _isHovered;

        public FileAction(string title, SymbolRegular icon)
        {
            Title = title;
            Icon = icon;
        }
    }
}