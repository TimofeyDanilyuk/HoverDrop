using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HoverDrop.App.Models;
using HoverDrop.App.Services;
using HoverDrop.App.Utils;
using Wpf.Ui.Controls;

namespace HoverDrop.App.ViewModels
{
    public partial class OverlayViewModel : ObservableObject
    {
        // Геометрия (окно 400x400)
        public const double Center = 200;
        public const double HubRadius = 105;   // видимый внутренний радиус кольца
        public const double RingOuter = 185;   // видимый внешний радиус кольца
        private const double Corner = 7;       // радиус скругления углов сектора
        private const double VisualGap = 5;    // видимый зазор между секторами, px
        private const double EdgeOffset = VisualGap / 2 + Corner;

        private static readonly HashSet<string> ImageExts = new(System.StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tiff" };
        private static readonly HashSet<string> VideoExts = new(System.StringComparer.OrdinalIgnoreCase) { ".mp4", ".mkv", ".avi", ".mov", ".webm" };
        private static readonly HashSet<string> AudioExts = new(System.StringComparer.OrdinalIgnoreCase) { ".mp3", ".wav", ".flac", ".ogg" };
        private static readonly HashSet<string> ArchiveExts = new(System.StringComparer.OrdinalIgnoreCase) { ".zip", ".rar", ".7z" };
        private static readonly HashSet<string> TextExts = new(System.StringComparer.OrdinalIgnoreCase) { ".txt", ".md", ".csv", ".json", ".xml", ".cs" };

        public ObservableCollection<FileAction> Actions { get; } = new();

        // Размытый снимок фона под меню; выставляется в App перед показом окна
        public ImageSource? GlassImage { get; set; }

        private string? _loadedKey;

        [ObservableProperty]
        private string _centerText = LocalizationService.ReleaseFile;

        [ObservableProperty]
        private string _fileName = "";

        [ObservableProperty]
        private SymbolRegular _fileIcon = SymbolRegular.Document24;

        // Системная иконка файла; если null, показывается SymbolRegular
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFileImage))]
        [NotifyPropertyChangedFor(nameof(HasNoFileImage))]
        private ImageSource? _fileImage;

        public bool HasFileImage => FileImage != null;
        public bool HasNoFileImage => FileImage == null;

        // Для группы файлов: число под иконкой в центре
        [ObservableProperty]
        private bool _isGroup;

        [ObservableProperty]
        private string _countText = "";

        public void LoadActionsForFiles(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;

            // DragEnter может прилетать много раз: не пересобираем меню для того же набора
            string key = string.Join("|", paths);
            if (_loadedKey == key && Actions.Count > 0) return;
            _loadedKey = key;

            Actions.Clear();

            var types = paths.Length == 1 ? GetSingleActions(paths[0]) : GetGroupActions(paths);

            for (int i = 0; i < types.Count; i++)
            {
                var action = new FileAction(types[i], GetActionIcon(types[i])) { Glass = GlassImage };
                CalculateGeometry(action, types.Count, i, Center, HubRadius + Corner, RingOuter - Corner);
                Actions.Add(action);
            }
        }

        private List<FileActionType> GetSingleActions(string filePath)
        {
            IsGroup = false;
            CountText = "";

            bool isDir = Directory.Exists(filePath);
            string ext = isDir ? "" : Path.GetExtension(filePath).ToLowerInvariant();
            FileName = Path.GetFileName(filePath.TrimEnd('\\', '/'));
            FileIcon = isDir ? SymbolRegular.Folder24 : GetFileIcon(ext);
            FileImage = ShellIcon.Get(filePath);

            var list = new List<FileActionType>();

            if (isDir)
                list.AddRange(new[] { FileActionType.ToZip, FileActionType.CopyPath, FileActionType.MakeCopy, FileActionType.Desktop, FileActionType.Delete });
            else if (ImageExts.Contains(ext))
                list.AddRange(new[] { FileActionType.ToJpg, FileActionType.ToPng, FileActionType.ToIco, FileActionType.ToPdf, FileActionType.Blur, FileActionType.Grayscale });
            else if (VideoExts.Contains(ext))
                list.AddRange(new[] { FileActionType.ToMp3Audio, FileActionType.ToGif, FileActionType.Compress720p, FileActionType.RemoveAudio });
            else if (AudioExts.Contains(ext))
                list.AddRange(new[] { FileActionType.ToMp3, FileActionType.ToWav, FileActionType.Compress64k });
            else if (ArchiveExts.Contains(ext))
                list.AddRange(new[] { FileActionType.ExtractHere, FileActionType.ExtractToFolder, FileActionType.Delete });
            else if (TextExts.Contains(ext))
                list.AddRange(new[] { FileActionType.ToZip, FileActionType.CopyPath, FileActionType.MakeCopy, FileActionType.Delete });
            else if (ext == ".pdf")
                list.AddRange(new[] { FileActionType.ToZip, FileActionType.CopyPath, FileActionType.Delete });
            else
                list.AddRange(new[] { FileActionType.ToZip, FileActionType.CopyPath, FileActionType.Desktop, FileActionType.Delete });

            return list;
        }

        private List<FileActionType> GetGroupActions(string[] paths)
        {
            IsGroup = true;
            CountText = paths.Length.ToString();
            FileName = LocalizationService.FilesCount(paths.Length);
            FileIcon = SymbolRegular.Copy24;
            FileImage = null;

            var list = new List<FileActionType>
            {
                FileActionType.ToZip, FileActionType.CollectToFolder, FileActionType.CopyPaths, FileActionType.Desktop, FileActionType.Delete
            };

            // Все файлы - изображения: добавляем пакетную конвертацию
            bool allImages = paths.All(p => !Directory.Exists(p) && ImageExts.Contains(Path.GetExtension(p).ToLowerInvariant()));
            if (allImages) list.InsertRange(2, new[] { FileActionType.ToJpg, FileActionType.ToPng });

            return list;
        }

        private static SymbolRegular GetActionIcon(FileActionType action) => action switch
        {
            FileActionType.ToJpg or FileActionType.ToPng or FileActionType.ToIco => SymbolRegular.Image24,
            FileActionType.ToPdf => SymbolRegular.DocumentPdf24,
            FileActionType.Blur => SymbolRegular.ImageEdit24,
            FileActionType.Grayscale => SymbolRegular.Color24,
            FileActionType.ToMp3Audio or FileActionType.ToMp3 or FileActionType.ToWav => SymbolRegular.MusicNote124,
            FileActionType.ToGif => SymbolRegular.Gif24,
            FileActionType.Compress720p or FileActionType.Compress64k => SymbolRegular.ArrowMinimize24,
            FileActionType.RemoveAudio => SymbolRegular.SpeakerMute24,
            FileActionType.ExtractHere => SymbolRegular.ArrowDownload24,
            FileActionType.ExtractToFolder => SymbolRegular.Folder24,
            FileActionType.CollectToFolder => SymbolRegular.FolderAdd24,
            FileActionType.ToZip => SymbolRegular.FolderZip24,
            FileActionType.CopyPath or FileActionType.CopyPaths => SymbolRegular.Link24,
            FileActionType.MakeCopy => SymbolRegular.Copy24,
            FileActionType.Delete => SymbolRegular.Delete24,
            FileActionType.Desktop => SymbolRegular.ArrowRight24,
            _ => SymbolRegular.Document24
        };

        private static SymbolRegular GetFileIcon(string ext)
        {
            if (ImageExts.Contains(ext)) return SymbolRegular.Image24;
            if (VideoExts.Contains(ext)) return SymbolRegular.Video24;
            if (AudioExts.Contains(ext)) return SymbolRegular.MusicNote124;
            if (ArchiveExts.Contains(ext)) return SymbolRegular.FolderZip24;
            if (ext == ".pdf") return SymbolRegular.DocumentPdf24;
            return SymbolRegular.Document24;
        }

        private static System.Windows.Point Polar(double center, double radius, double angleDeg)
        {
            double rad = angleDeg * System.Math.PI / 180.0;
            return new System.Windows.Point(center + radius * System.Math.Cos(rad), center + radius * System.Math.Sin(rad));
        }

        private static double AsinDeg(double x) => System.Math.Asin(x) * 180.0 / System.Math.PI;

        private void CalculateGeometry(FileAction action, int totalItems, int index, double center, double innerRadius, double outerRadius)
        {
            double sweep = 360.0 / totalItems;
            double a0 = index * sweep - 90;
            double a1 = a0 + sweep;

            // Края смещены от радиальных линий на постоянное расстояние: зазор одинаковой ширины на любом радиусе
            double innerShift = AsinDeg(EdgeOffset / innerRadius);
            double outerShift = AsinDeg(EdgeOffset / outerRadius);

            var p1 = Polar(center, innerRadius, a0 + innerShift);
            var p2 = Polar(center, outerRadius, a0 + outerShift);
            var p3 = Polar(center, outerRadius, a1 - outerShift);
            var p4 = Polar(center, innerRadius, a1 - innerShift);

            bool outerLarge = (sweep - 2 * outerShift) > 180;
            bool innerLarge = (sweep - 2 * innerShift) > 180;

            // Сжатая форма сектора
            var inset = new StreamGeometry();
            using (var ctx = inset.Open())
            {
                ctx.BeginFigure(p1, true, true);
                ctx.LineTo(p2, true, false);
                ctx.ArcTo(p3, new System.Windows.Size(outerRadius, outerRadius), 0, outerLarge, SweepDirection.Clockwise, true, false);
                ctx.LineTo(p4, true, false);
                ctx.ArcTo(p1, new System.Windows.Size(innerRadius, innerRadius), 0, innerLarge, SweepDirection.Counterclockwise, true, false);
            }
            inset.Freeze();

            // ...раздутая на Corner с круглыми углами: скруглённый сектор одной геометрией, без обводки
            var pen = new Pen(Brushes.Black, Corner * 2) { LineJoin = PenLineJoin.Round };
            var band = inset.GetWidenedPathGeometry(pen);
            var shape = Geometry.Combine(inset, band, GeometryCombineMode.Union, null);
            shape.Freeze();
            action.Shape = shape;

            // Центр сектора (для иконки и подписи)
            var mid = Polar(center, (innerRadius + outerRadius) / 2, a0 + sweep / 2);
            action.TextX = mid.X;
            action.TextY = mid.Y;
        }

        // distance: расстояние курсора от центра меню в DIP. Внутри центра и за кольцом ничего не подсвечивается.
        public void UpdateHover(double currentAngle, double distance)
        {
            if (Actions.Count == 0) return;

            int hoveredIndex = -1;
            if (distance >= HubRadius && distance <= RingOuter + 10)
            {
                double sectorSize = 360.0 / Actions.Count;
                hoveredIndex = (int)(currentAngle / sectorSize) % Actions.Count;
            }

            for (int i = 0; i < Actions.Count; i++)
            {
                bool hovered = i == hoveredIndex;
                if (Actions[i].IsHovered != hovered) Actions[i].IsHovered = hovered;
                if (hovered) CenterText = Actions[i].Title;
            }

            if (hoveredIndex == -1)
            {
                CenterText = LocalizationService.ReleaseFile;
            }
        }

        public FileAction? GetHoveredAction() => Actions.FirstOrDefault(a => a.IsHovered);

        public void Reset()
        {
            _loadedKey = null;
            Actions.Clear();
            FileName = "";
            IsGroup = false;
            CountText = "";
            CenterText = LocalizationService.ReleaseFile;
            FileImage = null;
        }
    }
}
