using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ImageMagick;

namespace HoverDrop.App.Services
{
    public static class FileActionService
    {
        public static async Task ExecuteActionAsync(string path, string actionName)
        {
            path = path.TrimEnd('\\', '/');
            bool isDir = Directory.Exists(path);
            string parent = Path.GetDirectoryName(path) ?? "";
            string name = isDir ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
            string ext = isDir ? "" : Path.GetExtension(path).ToLower();
            var toast = ToastService.Begin($"{actionName}: {Path.GetFileName(path)}");
            bool failed = false;

            await Task.Run(() =>
            {
                try
                {
                    // --- ОБЩИЕ ДЕЙСТВИЯ (файлы и папки) ---
                    switch (actionName)
                    {
                        case "Удалить":
                            if (isDir)
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(path,
                                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                            else
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(path,
                                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                            return;

                        case "Копировать путь":
                            Application.Current.Dispatcher.Invoke(() => Clipboard.SetText(path));
                            return;

                        case "Сделать копию":
                            if (isDir) CopyDirectory(path, Path.Combine(parent, $"{name}_copy"));
                            else File.Copy(path, Path.Combine(parent, $"{name}_copy{ext}"), true);
                            return;

                        case "На рабочий стол":
                        {
                            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            string target = Path.Combine(desktop, Path.GetFileName(path));
                            if (isDir)
                            {
                                try { Directory.Move(path, target); }
                                catch (IOException)
                                {
                                    // Directory.Move не работает при перемещении между дисками: копируем и удаляем
                                    CopyDirectory(path, target);
                                    Directory.Delete(path, true);
                                }
                            }
                            else File.Move(path, target, true);
                            return;
                        }

                        case "В ZIP-архив":
                        {
                            string zipPath = Path.Combine(parent, $"{name}.zip");
                            if (File.Exists(zipPath)) File.Delete(zipPath);
                            if (isDir)
                            {
                                ZipFile.CreateFromDirectory(path, zipPath, CompressionLevel.Optimal, true);
                            }
                            else
                            {
                                using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                                archive.CreateEntryFromFile(path, Path.GetFileName(path));
                            }
                            return;
                        }

                        case "Распаковать сюда" when ext == ".zip":
                            ZipFile.ExtractToDirectory(path, parent, true);
                            return;

                        case "Распаковать в папку" when ext == ".zip":
                        {
                            string extractDir = Path.Combine(parent, name);
                            Directory.CreateDirectory(extractDir);
                            ZipFile.ExtractToDirectory(path, extractDir, true);
                            return;
                        }

                        case "Распаковать сюда" when ext == ".rar" || ext == ".7z":
                            ExtractArchive(path, parent);
                            return;

                        case "Распаковать в папку" when ext == ".rar" || ext == ".7z":
                        {
                            string extractDir = Path.Combine(parent, name);
                            Directory.CreateDirectory(extractDir);
                            ExtractArchive(path, extractDir);
                            return;
                        }
                    }

                    // --- ИЗОБРАЖЕНИЯ (Magick.NET) ---
                    if (new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tiff" }.Contains(ext))
                    {
                        using var image = new MagickImage(path);
                        switch (actionName)
                        {
                            case "В JPG":
                                image.Format = MagickFormat.Jpeg;
                                image.Quality = 90;
                                image.Write(OutPath(parent, name, ".jpg", path));
                                break;
                            case "В PNG":
                                image.Format = MagickFormat.Png;
                                image.Write(OutPath(parent, name, ".png", path));
                                break;
                            case "В ICO":
                                image.Resize(new MagickGeometry(256, 256) { IgnoreAspectRatio = false });
                                image.Format = MagickFormat.Ico;
                                image.Write(OutPath(parent, name, ".ico", path));
                                break;
                            case "В PDF":
                                image.Format = MagickFormat.Pdf;
                                image.Write(OutPath(parent, name, ".pdf", path));
                                break;
                            case "Размыть":
                                image.Blur(0, 5);
                                image.Write(Path.Combine(parent, $"{name}_blurred{ext}"));
                                break;
                            case "Обесцветить":
                                image.Grayscale();
                                image.Write(Path.Combine(parent, $"{name}_bw{ext}"));
                                break;
                        }
                        return;
                    }

                    // --- ВИДЕО И АУДИО (FFmpeg: рядом с exe или в PATH) ---
                    if (new[] { ".mp4", ".mkv", ".avi", ".mov", ".webm", ".mp3", ".wav", ".flac", ".ogg" }.Contains(ext))
                    {
                        string input = $"\"{path}\"";
                        switch (actionName)
                        {
                            case "В MP3 (Аудио)":
                            case "В MP3":
                                RunFfmpeg(toast, $"-i {input} -vn -q:a 0 \"{OutPath(parent, name, ".mp3", path)}\" -y");
                                break;
                            case "В WAV":
                                RunFfmpeg(toast, $"-i {input} -vn \"{OutPath(parent, name, ".wav", path)}\" -y");
                                break;
                            case "В GIF":
                                RunFfmpeg(toast, $"-i {input} -vf \"fps=10,scale=480:-1:flags=lanczos\" \"{OutPath(parent, name, ".gif", path)}\" -y");
                                break;
                            case "Сжать (720p)":
                                RunFfmpeg(toast, $"-i {input} -vf scale=-2:720 -c:v libx264 -crf 26 -c:a copy \"{Path.Combine(parent, $"{name}_720p.mp4")}\" -y");
                                break;
                            case "Сжать (64kbps)":
                                RunFfmpeg(toast, $"-i {input} -vn -b:a 64k \"{Path.Combine(parent, $"{name}_64k.mp3")}\" -y");
                                break;
                            case "Удалить звук":
                                RunFfmpeg(toast, $"-i {input} -c copy -an \"{Path.Combine(parent, $"{name}_nosound{ext}")}\" -y");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    failed = true;
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"Ошибка при обработке файла: {ex.Message}", "HoverDrop",
                            MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });

            if (failed) toast.Fail(); else toast.Complete();
        }

        // Если итоговый путь совпал с исходным файлом, добавляем суффикс, чтобы не затереть оригинал
        private static string OutPath(string dir, string name, string newExt, string original)
        {
            string result = Path.Combine(dir, name + newExt);
            if (string.Equals(result, original, StringComparison.OrdinalIgnoreCase))
                result = Path.Combine(dir, $"{name}_converted{newExt}");
            return result;
        }

        // Распаковка .rar/.7z через SharpCompress (с защитой от выхода за пределы папки)
        private static void ExtractArchive(string archivePath, string targetDir)
        {
            string root = Path.GetFullPath(targetDir);
            using var archive = SharpCompress.Archives.ArchiveFactory.OpenArchive(archivePath);
            foreach (var entry in archive.Entries)
            {
                if (entry.IsDirectory || string.IsNullOrEmpty(entry.Key)) continue;

                string dest = Path.GetFullPath(Path.Combine(root, entry.Key.Replace('/', Path.DirectorySeparatorChar)));
                if (!dest.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;

                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                using var src = entry.OpenEntryStream();
                using var dst = File.Create(dest);
                src.CopyTo(dst);
            }
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
        }

        private static readonly Regex DurationRx = new(@"Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)", RegexOptions.Compiled);
        private static readonly Regex TimeRx = new(@"time=(\d+):(\d+):(\d+(?:\.\d+)?)", RegexOptions.Compiled);

        private static double ParseSeconds(Match m) =>
            int.Parse(m.Groups[1].Value) * 3600 + int.Parse(m.Groups[2].Value) * 60 +
            double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);

        private static void RunFfmpeg(ToastHandle toast, string arguments)
        {
            // Сначала ищем ffmpeg.exe рядом с приложением, иначе берём из PATH
            string local = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            var psi = new ProcessStartInfo
            {
                FileName = File.Exists(local) ? local : "ffmpeg",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            try
            {
                using var process = Process.Start(psi)!;
                var tail = new StringBuilder();
                double total = 0;

                // ffmpeg пишет прогресс в stderr, строки разделены \r: ReadLine это понимает
                string? line;
                while ((line = process.StandardError.ReadLine()) != null)
                {
                    tail.AppendLine(line);
                    if (tail.Length > 2000) tail.Remove(0, tail.Length - 2000);

                    if (total <= 0)
                    {
                        var d = DurationRx.Match(line);
                        if (d.Success) total = ParseSeconds(d);
                    }
                    else
                    {
                        var t = TimeRx.Match(line);
                        if (t.Success) toast.Report(ParseSeconds(t) / total);
                    }
                }

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    string err = tail.ToString();
                    throw new InvalidOperationException("FFmpeg завершился с ошибкой:\n" + err[^Math.Min(err.Length, 400)..]);
                }
            }
            catch (Win32Exception)
            {
                throw new InvalidOperationException("FFmpeg не найден. Положите ffmpeg.exe рядом с приложением или добавьте его в PATH.");
            }
        }

        // ---------- ГРУППОВЫЕ ДЕЙСТВИЯ ----------

        public static async Task ExecuteGroupActionAsync(string[] paths, string actionName)
        {
            string parent = Path.GetDirectoryName(paths[0].TrimEnd('\\', '/')) ?? "";
            var errors = new List<string>();
            var toast = ToastService.Begin($"{actionName}: {paths.Length} шт.");
            int done = 0;

            void ForEachSafe(Action<string> action)
            {
                foreach (var p in paths)
                {
                    try { action(p); }
                    catch (Exception ex) { errors.Add($"{Path.GetFileName(p.TrimEnd('\\', '/'))}: {ex.Message}"); }
                    toast.Report(++done / (double)paths.Length);
                }
            }

            await Task.Run(() =>
            {
                try
                {
                    switch (actionName)
                    {
                        case "В ZIP-архив":
                        {
                            string zipPath = UniquePath(Path.Combine(parent, $"Архив_{DateTime.Now:yyyyMMdd_HHmmss}.zip"));
                            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            ForEachSafe(p =>
                            {
                                string entryName = UniqueName(Path.GetFileName(p.TrimEnd('\\', '/')), used);
                                if (Directory.Exists(p)) AddDirectoryToZip(archive, p, entryName);
                                else archive.CreateEntryFromFile(p, entryName);
                            });
                            break;
                        }

                        case "Собрать в папку":
                        {
                            string folder = UniquePath(Path.Combine(parent, "Новая папка"));
                            Directory.CreateDirectory(folder);
                            ForEachSafe(p => MoveInto(p, folder));
                            break;
                        }

                        case "Копировать пути":
                            Application.Current.Dispatcher.Invoke(() =>
                                Clipboard.SetText(string.Join(Environment.NewLine, paths)));
                            break;

                        case "На рабочий стол":
                        {
                            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            ForEachSafe(p => MoveInto(p, desktop));
                            break;
                        }

                        case "Удалить":
                            ForEachSafe(p =>
                            {
                                if (Directory.Exists(p))
                                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(p,
                                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                                else
                                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(p,
                                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                            });
                            break;

                        case "В JPG":
                        case "В PNG":
                            ForEachSafe(p =>
                            {
                                string dir = Path.GetDirectoryName(p) ?? "";
                                string name = Path.GetFileNameWithoutExtension(p);
                                using var image = new MagickImage(p);
                                if (actionName == "В JPG")
                                {
                                    image.Format = MagickFormat.Jpeg;
                                    image.Quality = 90;
                                    image.Write(OutPath(dir, name, ".jpg", p));
                                }
                                else
                                {
                                    image.Format = MagickFormat.Png;
                                    image.Write(OutPath(dir, name, ".png", p));
                                }
                            });
                            break;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                }
            });

            if (errors.Count > 0) toast.Fail(); else toast.Complete();
            if (errors.Count > 0)
            {
                MessageBox.Show("Не всё удалось обработать:\n\n" + string.Join("\n", errors.Take(8)),
                    "HoverDrop", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Переносит файл или папку в каталог, не затирая существующее
        private static void MoveInto(string source, string targetDir)
        {
            source = source.TrimEnd('\\', '/');
            string target = UniquePath(Path.Combine(targetDir, Path.GetFileName(source)));
            if (Directory.Exists(source)) Directory.Move(source, target);
            else File.Move(source, target);
        }

        // Если путь занят, добавляет " (2)", " (3)" и т.д.
        private static string UniquePath(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return path;

            string dir = Path.GetDirectoryName(path) ?? "";
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            int i = 2;
            string candidate;
            do { candidate = Path.Combine(dir, $"{name} ({i++}){ext}"); }
            while (File.Exists(candidate) || Directory.Exists(candidate));
            return candidate;
        }

        // Уникальное имя записи внутри архива
        private static string UniqueName(string name, HashSet<string> used)
        {
            string candidate = name;
            int i = 2;
            while (!used.Add(candidate))
                candidate = $"{Path.GetFileNameWithoutExtension(name)} ({i++}){Path.GetExtension(name)}";
            return candidate;
        }

        private static void AddDirectoryToZip(ZipArchive archive, string dir, string entryRoot)
        {
            archive.CreateEntry(entryRoot + "/");
            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(dir, file).Replace('\\', '/');
                archive.CreateEntryFromFile(file, $"{entryRoot}/{rel}");
            }
        }
    }
}