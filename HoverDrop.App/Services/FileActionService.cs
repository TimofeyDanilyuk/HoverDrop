using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
                            if (isDir) Directory.Move(path, target);
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
                                RunFfmpeg($"-i {input} -vn -q:a 0 \"{OutPath(parent, name, ".mp3", path)}\" -y");
                                break;
                            case "В WAV":
                                RunFfmpeg($"-i {input} -vn \"{OutPath(parent, name, ".wav", path)}\" -y");
                                break;
                            case "В GIF":
                                RunFfmpeg($"-i {input} -vf \"fps=10,scale=480:-1:flags=lanczos\" \"{OutPath(parent, name, ".gif", path)}\" -y");
                                break;
                            case "Сжать (720p)":
                                RunFfmpeg($"-i {input} -vf scale=-2:720 -c:v libx264 -crf 26 -c:a copy \"{Path.Combine(parent, $"{name}_720p.mp4")}\" -y");
                                break;
                            case "Сжать (64kbps)":
                                RunFfmpeg($"-i {input} -vn -b:a 64k \"{Path.Combine(parent, $"{name}_64k.mp3")}\" -y");
                                break;
                            case "Удалить звук":
                                RunFfmpeg($"-i {input} -c copy -an \"{Path.Combine(parent, $"{name}_nosound{ext}")}\" -y");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"Ошибка при обработке файла: {ex.Message}", "HoverDrop",
                            MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }

        // Если итоговый путь совпал с исходным файлом, добавляем суффикс, чтобы не затереть оригинал
        private static string OutPath(string dir, string name, string newExt, string original)
        {
            string result = Path.Combine(dir, name + newExt);
            if (string.Equals(result, original, StringComparison.OrdinalIgnoreCase))
                result = Path.Combine(dir, $"{name}_converted{newExt}");
            return result;
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
        }

        private static void RunFfmpeg(string arguments)
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
                string err = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("FFmpeg завершился с ошибкой:\n" + err[^Math.Min(err.Length, 400)..]);
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

            void ForEachSafe(Action<string> action)
            {
                foreach (var p in paths)
                {
                    try { action(p); }
                    catch (Exception ex) { errors.Add($"{Path.GetFileName(p.TrimEnd('\\', '/'))}: {ex.Message}"); }
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