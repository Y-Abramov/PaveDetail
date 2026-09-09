using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PaveDetail.Ui
{
    // Образец штриховки для грида каталога. Своя отрисовка, не родная Robur:
    // показывает наклон, частоту и факт сплошной заливки, точного совпадения
    // с чертежом не обещает.
    internal static class HatchPreview
    {
        private static readonly Color Ink = Color.FromArgb(51, 65, 85);      // #334155
        private static readonly Color Accent = Color.FromArgb(8, 145, 178);  // #0891B2

        public static void Draw(Graphics g, Rectangle box, string pattern,
            double scale, double angle, bool asLine)
        {
            if (box.Width <= 2 || box.Height <= 2) return;

            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Геттер Graphics.Clip создаёт новый Region на каждое обращение,
            // освобождать его обязан вызывающий - иначе течёт GDI-хендл
            // на каждую перерисовку ячейки грида.
            var clip = g.Clip;
            g.SetClip(box);
            try
            {
                using (var back = new SolidBrush(Color.White))
                    g.FillRectangle(back, box);

                if (asLine) { DrawLineMaterial(g, box); return; }

                var name = (pattern ?? string.Empty).Trim().ToUpperInvariant();
                if (name.Length == 0 || name == "SOLID") { DrawSolid(g, box); return; }

                double step = Step(scale, box.Height);
                if (name == "GRAVEL") { DrawGravel(g, box, step); return; }
                if (name == "AR-SAND") { DrawSand(g, box, step); return; }
                if (name == "AR-CONC") { DrawConcrete(g, box, step, angle); return; }
                if (name == "EARTH") { DrawCross(g, box, step, angle); return; }

                // ANSI31 и всё незнакомое - наклонные линии под заданным углом.
                DrawParallel(g, box, step, angle == 0.0 ? 45.0 : angle);
            }
            finally
            {
                g.Clip = clip;
                clip.Dispose();
                g.SmoothingMode = old;
            }
        }

        // Масштаб каталога задан в единицах модели (типично 0.02-0.03 м).
        // Множитель 400 подобран так, чтобы это давало видимые 8-12 пикселей.
        private static double Step(double scale, int boxHeight)
        {
            double step = (scale <= 0.0 ? 0.02 : scale) * 400.0;
            if (step < 3.0) step = 3.0;
            if (step > boxHeight) step = boxHeight;
            return step;
        }

        private static void DrawSolid(Graphics g, Rectangle box)
        {
            using (var brush = new SolidBrush(Ink))
                g.FillRectangle(brush, box);
        }

        private static void DrawLineMaterial(Graphics g, Rectangle box)
        {
            using (var pen = new Pen(Accent, 3f))
                g.DrawLine(pen, box.Left + 2, box.Top + box.Height / 2,
                                box.Right - 2, box.Top + box.Height / 2);
        }

        private static void DrawParallel(Graphics g, Rectangle box, double step, double angleDeg)
        {
            using (var pen = new Pen(Ink, 1f))
            {
                double rad = angleDeg * Math.PI / 180.0;
                double dx = Math.Cos(rad), dy = -Math.Sin(rad);
                double span = box.Width + box.Height;
                double nx = -dy, ny = dx;
                double cx = box.Left + box.Width / 2.0, cy = box.Top + box.Height / 2.0;

                for (double t = -span; t <= span; t += step)
                {
                    var p1 = new PointF((float)(cx + nx * t - dx * span), (float)(cy + ny * t - dy * span));
                    var p2 = new PointF((float)(cx + nx * t + dx * span), (float)(cy + ny * t + dy * span));
                    g.DrawLine(pen, p1, p2);
                }
            }
        }

        private static void DrawCross(Graphics g, Rectangle box, double step, double angleDeg)
        {
            double a = angleDeg == 0.0 ? 45.0 : angleDeg;
            DrawParallel(g, box, step, a);
            DrawParallel(g, box, step, a + 90.0);
        }

        private static void DrawGravel(Graphics g, Rectangle box, double step)
        {
            // Щебень: угловатые зёрна вразброс. Позиции детерминированы - образец
            // не должен прыгать при каждой перерисовке грида.
            var rnd = new Random(17);
            int size = (int)Math.Max(2.0, step * 0.7);
            using (var pen = new Pen(Ink, 1f))
                for (int y = box.Top + 2; y < box.Bottom - 2; y += (int)Math.Max(3.0, step))
                    for (int x = box.Left + 2; x < box.Right - 2; x += (int)Math.Max(3.0, step))
                    {
                        int ox = rnd.Next(-1, 2), oy = rnd.Next(-1, 2);
                        g.DrawPolygon(pen, new[]
                        {
                            new Point(x + ox, y + oy + size / 2),
                            new Point(x + ox + size / 2, y + oy),
                            new Point(x + ox + size, y + oy + size / 2),
                            new Point(x + ox + size / 2, y + oy + size)
                        });
                    }
        }

        private static void DrawSand(Graphics g, Rectangle box, double step)
        {
            var rnd = new Random(29);
            using (var brush = new SolidBrush(Ink))
                for (int y = box.Top + 2; y < box.Bottom - 2; y += (int)Math.Max(3.0, step * 0.6))
                    for (int x = box.Left + 2; x < box.Right - 2; x += (int)Math.Max(3.0, step * 0.6))
                        g.FillEllipse(brush, x + rnd.Next(-1, 2), y + rnd.Next(-1, 2), 1.6f, 1.6f);
        }

        private static void DrawConcrete(Graphics g, Rectangle box, double step, double angle)
        {
            // Бетон: редкая наклонная штриховка плюс точки заполнителя.
            DrawParallel(g, box, step * 2.0, angle == 0.0 ? 45.0 : angle);
            DrawSand(g, box, step * 2.5);
        }
    }
}
