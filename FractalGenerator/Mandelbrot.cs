using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FractalGenerator
{
    public class Mandelbrot
    {
        private static readonly double Log2 = Math.Log(2.0);

        // Створення зображення множини Мандельброта
        public Bitmap CreateBitmap(
            int width,
            int height,
            int maxIterations,
            Color baseColor,
            double centerReal,
            double centerImaginary,
            double zoom,
            CancellationToken cancellationToken)
        {
            // Визначаємо розмір області комплексної площини
            double viewHeight = 2.4 / zoom;
            double viewWidth = viewHeight * width / height;

            // Обчислюємо межі області перегляду
            double minReal = centerReal - viewWidth / 2.0;
            double maxImaginary = centerImaginary + viewHeight / 2.0;

            double stepX = viewWidth / width;
            double stepY = viewHeight / height;

            // Масив для збереження кольорів пікселів
            byte[] pixels = new byte[width * height * 4];

            ParallelOptions options = new ParallelOptions
            {
                CancellationToken = cancellationToken
            };

            // Перевіряємо кожен ряд пікселів паралельно на всіх ядрах процесора
            Parallel.For(0, height, options, y =>
            {
                double imaginary = maxImaginary - y * stepY;
                double imaginarySq = imaginary * imaginary;
                int rowOffset = y * width * 4;

                for (int x = 0; x < width; x++)
                {
                    // Перетворюємо координати пікселя на координати комплексної площини
                    double real = minReal + x * stepX;

                    // Швидка перевірка головної кардіоїди та лівого кола
                    double q = (real - 0.25) * (real - 0.25) + imaginarySq;
                    if (q * (q + (real - 0.25)) <= 0.25 * imaginarySq ||
                        (real + 1.0) * (real + 1.0) + imaginarySq <= 0.0625)
                    {
                        int idx = rowOffset + x * 4;
                        pixels[idx] = 30;
                        pixels[idx + 1] = 20;
                        pixels[idx + 2] = 15;
                        pixels[idx + 3] = 255;
                        continue;
                    }

                    // Початкове значення комплексного числа
                    double zReal = 0;
                    double zImaginary = 0;
                    double zRealSq = 0;
                    double zImaginarySq = 0;

                    int iteration = 0;

                    // Ітераційний алгоритм Мандельброта
                    while (zRealSq + zImaginarySq <= 4.0 && iteration < maxIterations)
                    {
                        zImaginary = 2.0 * zReal * zImaginary + imaginary;
                        zReal = zRealSq - zImaginarySq + real;

                        zRealSq = zReal * zReal;
                        zImaginarySq = zImaginary * zImaginary;

                        iteration++;
                    }

                    // Визначаємо колір пікселя
                    Color pixelColor = GetColor(
                        iteration,
                        maxIterations,
                        zRealSq + zImaginarySq,
                        baseColor);

                    // Записуємо колір у масив
                    int index = rowOffset + x * 4;

                    pixels[index] = pixelColor.B;
                    pixels[index + 1] = pixelColor.G;
                    pixels[index + 2] = pixelColor.R;
                    pixels[index + 3] = 255;
                }
            });

            // Створюємо зображення з масиву пікселів
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
                    // Копіюємо кольори в пам'ять зображення
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

        // Отримання кольору залежно від кількості ітерацій та палітри
        private Color GetColor(
            int iteration,
            int maxIterations,
            double magnitudeSq,
            Color baseColor)
        {
            // Точки всередині множини
            if (iteration == maxIterations)
            {
                return Color.FromArgb(15, 20, 30);
            }

            // Плавне значення кількості ітерацій без обчислення квадратного кореня
            double logZn = 0.5 * Math.Log(magnitudeSq);
            double smoothIteration = iteration + 1.0 -
                Math.Log(logZn / Log2) / Log2;

            // Хвильовий градієнт для плавних переливів
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