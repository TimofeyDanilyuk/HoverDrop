using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HoverDrop.App.Utils
{
    public static class ScreenGlass
    {
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr dest, int x, int y, int w, int h, IntPtr src, int sx, int sy, int rop);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);

        private const int SRCCOPY = 0x00CC0020;
        private const int CAPTUREBLT = 0x40000000;

        /// <summary>
        /// Снимает квадрат sizePx x sizePx вокруг точки (физические пиксели) и возвращает размытую копию.
        /// Вызывать ДО показа окна меню, иначе в снимок попадёт само меню.
        /// </summary>
        public static BitmapSource? CaptureBlurred(int centerX, int centerY, int sizePx,
            int block = 4, int radius = 5, int passes = 3)
        {
            IntPtr screen = GetDC(IntPtr.Zero);
            IntPtr mem = CreateCompatibleDC(screen);
            IntPtr bmp = CreateCompatibleBitmap(screen, sizePx, sizePx);
            IntPtr old = SelectObject(mem, bmp);

            try
            {
                BitBlt(mem, 0, 0, sizePx, sizePx, screen, centerX - sizePx / 2, centerY - sizePx / 2, SRCCOPY | CAPTUREBLT);
                var src = Imaging.CreateBitmapSourceFromHBitmap(bmp, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                return Blur(src, block, radius, passes);
            }
            catch
            {
                return null; // без стекла меню всё равно работает
            }
            finally
            {
                SelectObject(mem, old);
                DeleteObject(bmp);
                DeleteDC(mem);
                ReleaseDC(IntPtr.Zero, screen);
            }
        }

        // Уменьшаем картинку усреднением блоков, размываем уменьшенную копию (box blur),
        // а при отрисовке WPF плавно растягивает её обратно: получается дешёвое и мягкое размытие.
        private static BitmapSource Blur(BitmapSource src, int block, int radius, int passes)
        {
            if (src.Format.BitsPerPixel != 32)
                src = new FormatConvertedBitmap(src, PixelFormats.Bgr32, null, 0);

            int w = src.PixelWidth, h = src.PixelHeight, stride = w * 4;
            var px = new byte[stride * h];
            src.CopyPixels(px, stride, 0);

            int sw = Math.Max(1, w / block), sh = Math.Max(1, h / block);
            var small = new float[sw * sh * 3];

            for (int sy = 0; sy < sh; sy++)
            {
                for (int sx = 0; sx < sw; sx++)
                {
                    int x0 = sx * w / sw, x1 = (sx + 1) * w / sw;
                    int y0 = sy * h / sh, y1 = (sy + 1) * h / sh;
                    float b = 0, g = 0, r = 0; int n = 0;
                    for (int y = y0; y < y1; y++)
                    {
                        for (int x = x0; x < x1; x++)
                        {
                            int i = y * stride + x * 4;
                            b += px[i]; g += px[i + 1]; r += px[i + 2]; n++;
                        }
                    }
                    int o = (sy * sw + sx) * 3;
                    small[o] = b / n; small[o + 1] = g / n; small[o + 2] = r / n;
                }
            }

            for (int p = 0; p < passes; p++)
            {
                BoxBlur(small, sw, sh, radius, true);
                BoxBlur(small, sw, sh, radius, false);
            }

            var outPx = new byte[sw * sh * 4];
            for (int i = 0; i < sw * sh; i++)
            {
                outPx[i * 4] = (byte)small[i * 3];
                outPx[i * 4 + 1] = (byte)small[i * 3 + 1];
                outPx[i * 4 + 2] = (byte)small[i * 3 + 2];
                outPx[i * 4 + 3] = 255;
            }

            var result = BitmapSource.Create(sw, sh, 96, 96, PixelFormats.Bgra32, null, outPx, sw * 4);
            result.Freeze();
            return result;
        }

        private static void BoxBlur(float[] data, int w, int h, int radius, bool horizontal)
        {
            var copy = (float[])data.Clone();
            int len = horizontal ? w : h;
            int lines = horizontal ? h : w;

            for (int line = 0; line < lines; line++)
            {
                for (int i = 0; i < len; i++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        float sum = 0;
                        int cnt = 0;
                        for (int k = -radius; k <= radius; k++)
                        {
                            int j = Math.Clamp(i + k, 0, len - 1);
                            int idx = horizontal ? (line * w + j) * 3 + c : (j * w + line) * 3 + c;
                            sum += copy[idx];
                            cnt++;
                        }
                        int dst = horizontal ? (line * w + i) * 3 + c : (i * w + line) * 3 + c;
                        data[dst] = sum / cnt;
                    }
                }
            }
        }
    }
}