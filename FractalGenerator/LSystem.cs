
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace FractalGenerator
{
    public class LSystem
    {
        // Побудова та малювання кривої дракона
        public void Draw(
            Graphics graphics,
            int width,
            int height,
            int depth,
            Color color)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            // Початкова послідовність
            string axiom = "FX";

            // Правила заміни символів
            string ruleX = "X+YF+";
            string ruleY = "-FX-Y";

            string current = axiom;

            // Послідовно застосовуємо правила
            for (int i = 0; i < depth; i++)
            {
                StringBuilder next = new StringBuilder();

                foreach (char symbol in current)
                {
                    if (symbol == 'X')
                    {
                        next.Append(ruleX);
                    }
                    else if (symbol == 'Y')
                    {
                        next.Append(ruleY);
                    }
                    else
                    {
                        next.Append(symbol);
                    }
                }

                current = next.ToString();
            }

            // Будуємо шлях за отриманою послідовністю
            List<PointF> points = new List<PointF>();

            double x = 0;
            double y = 0;
            double angle = 0;

            points.Add(new PointF(0, 0));

            foreach (char symbol in current)
            {
                if (symbol == 'F')
                {
                    double radians = angle * Math.PI / 180.0;

                    x += Math.Cos(radians);
                    y += Math.Sin(radians);

                    points.Add(new PointF((float)x, (float)y));
                }
                else if (symbol == '+')
                {
                    angle += 90;
                }
                else if (symbol == '-')
                {
                    angle -= 90;
                }
            }

            if (points.Count < 2)
            {
                return;
            }

            // Знаходимо межі побудованого шляху
            float minX = points[0].X;
            float maxX = points[0].X;
            float minY = points[0].Y;
            float maxY = points[0].Y;

            foreach (PointF point in points)
            {
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
            }

            // Визначаємо масштаб для розміщення кривої у вікні
            float figureWidth = Math.Max(maxX - minX, 1);
            float figureHeight = Math.Max(maxY - minY, 1);

            float scaleX = (width - 40) / figureWidth;
            float scaleY = (height - 40) / figureHeight;

            float scale = Math.Min(scaleX, scaleY);

            // Перетворюємо координати шляху на координати екрана
            PointF[] screenPoints = new PointF[points.Count];

            float offsetX = (width - figureWidth * scale) / 2;
            float offsetY = (height - figureHeight * scale) / 2;

            for (int i = 0; i < points.Count; i++)
            {
                screenPoints[i] = new PointF(
                    offsetX + (points[i].X - minX) * scale,
                    offsetY + (points[i].Y - minY) * scale);
            }

            // Малюємо готову криву
            using (Pen pen = new Pen(color, 2))
            {
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                graphics.DrawLines(pen, screenPoints);
            }
        }
    }
}
