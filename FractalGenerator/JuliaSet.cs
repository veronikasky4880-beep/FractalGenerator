using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FractalGenerator
{
    public class JuliaSet
    {
        private static readonly double Log2 = Math.Log(2.0);

        // Створення зображення множини Жюліа
        public Bitmap CreateBitmap(
            int width,
            int height,
            int maxIterations,
            Color baseColor,
            double centerReal,
            double centerImaginary,
            double zoom,
            double cReal,
            double cImaginary,
            CancellationToken cancellationToken)
        {
            // Визначаємо область комплексної площини
            double viewHeight = 2.4 / zoom;
            double viewWidth = viewHeight * width / height;

            double minReal = centerReal - viewWidth / 2.0;
            double maxImaginary = centerImaginary + viewHeight / 2.0;

            double stepX = viewWidth / width;
            double stepY = viewHeight / height;

            // Масив для кольорів пікселів
            byte[] pixels = new byte[width * height * 4];

            ParallelOptions options = new ParallelOptions
            {
                CancellationToken = cancellationToken
            };

            // Обчислюємо кожен рядок паралельно
            Parallel.For(0, height, options, y =>
            {
                double imaginary = maxImaginary - y * stepY;
                int rowOffset = y * width * 4;

                for (int x = 0; x < width; x++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Початкове комплексне число задається координатами пікселя
                    double zReal = minReal + x * stepX;
                    double zImaginary = imaginary;

                    double zRealSq = zReal * zReal;
                    double zImaginarySq = zImaginary * zImaginary;

                    int iteration = 0;

                    // Ітераційний алгоритм множини Жюліа
                    while (zRealSq + zImaginarySq <= 4.0 &&
                           iteration < maxIterations)
                    {
                        double newReal = zRealSq - zImaginarySq + cReal;
                        double newImaginary =
                            2.0 * zReal * zImaginary + cImaginary;

                        zReal = newReal;
                        zImaginary = newImaginary;

                        zRealSq = zReal * zReal;
                        zImaginarySq = zImaginary * zImaginary;

                        iteration++;
                    }

                    Color pixelColor = GetColor(
                        iteration,
                        maxIterations,
                        zRealSq + zImaginarySq,
                        baseColor);

                    int index = rowOffset + x * 4;

                    pixels[index] = pixelColor.B;
                    pixels[index + 1] = pixelColor.G;
                    pixels[index + 2] = pixelColor.R;
                    pixels[index + 3] = 255;
                }
            });

            // Створюємо Bitmap із масиву пікселів
            Bitmap bitmap = new Bitmap(
                width,
                height,
                PixelFormat.Format32bppArgb);

            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                try
                {
                    for (int y = 0; y < height; y++)
                    {
                        IntPtr destination = IntPtr.Add(
                            bitmapData.Scan0,
                            y * bitmapData.Stride);

                        Marshal.Copy(
                            pixels,
                            y * width * 4,
                            destination,
                            width * 4);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }

            return bitmap;
        }

        // Визначення кольору пікселя
        private Color GetColor(
            int iteration,
            int maxIterations,
            double magnitudeSq,
            Color baseColor)
        {
            // Точки, які не вийшли за межі множини
            if (iteration == maxIterations)
            {
                return Color.FromArgb(15, 20, 30);
            }

            // Плавне значення кількості ітерацій
            double logZn = 0.5 * Math.Log(magnitudeSq);

            double smoothIteration = iteration + 1.0 -
                Math.Log(logZn / Log2) / Log2;

            // Створення плавного градієнта
            double wave = 0.5 - 0.5 *
                Math.Cos(smoothIteration * 0.25);

            int red = (int)(15 + (baseColor.R - 15) * wave);
            int green = (int)(20 + (baseColor.G - 20) * wave);
            int blue = (int)(30 + (baseColor.B - 30) * wave);

            return Color.FromArgb(
                Math.Clamp(red, 0, 255),
                Math.Clamp(green, 0, 255),
                Math.Clamp(blue, 0, 255));
        }
    }
}
