using System.Collections.Generic;
using System.Globalization;

namespace PaveDetail.Core
{
    public sealed class CompactOptions
    {
        public double WidthM = 0.60;      // ширина графы «Сечение» за вычетом полей
        public double HeightM = 0.30;     // высота разреза в ячейке
        public double MinBandM = 0.03;    // тоньше полосу не рисуем: слой станет невидим
        public double TextHeightM = 0.05; // высота цифры номера
        public double UnitScale = 1.0;   // множитель для реальной толщины слоя из модели
    }

    // Разрез для ячейки таблицы: высота нормируется под строку, поэтому вертикальный
    // масштаб здесь не натуральный - как и в эталонном листе заказчика, где разрезы всех
    // типов покрытий одной высоты. Настоящие толщины стоят в графе «Толщ. слоя».
    public static class CompactSection
    {
        public static IList<Shape> Build(IList<LayerInput> layers, MaterialResolver resolver, CompactOptions options)
        {
            var opt = options ?? new CompactOptions();
            var shapes = new List<Shape>();
            if (layers == null || layers.Count == 0) return shapes;

            var heights = Heights(layers, opt);

            double y = 0;
            for (int i = 0; i < layers.Count; i++)
            {
                double bottom = y - heights[i];
                var style = resolver.Resolve(layers[i]);

                shapes.Add(new RectShape
                {
                    Left = 0, Right = opt.WidthM, Top = y, Bottom = bottom,
                    Style = style, ColorArgb = layers[i].ColorArgb
                });

                shapes.Add(new TextShape
                {
                    X = opt.WidthM + opt.TextHeightM * 0.4,
                    Y = (y + bottom) / 2.0 - opt.TextHeightM * 0.4,
                    Text = (i + 1).ToString(CultureInfo.InvariantCulture),
                    Height = opt.TextHeightM,
                    IsNumber = true
                });

                y = bottom;
            }

            return shapes;
        }

        // Тонкие слои забирают минимальную полосу, остальные делят то, что осталось,
        // пропорционально своим толщинам. Если минимумы съели всю высоту - все полосы
        // становятся равными, разрез всё равно остаётся в границах ячейки.
        private static double[] Heights(IList<LayerInput> layers, CompactOptions opt)
        {
            var result = new double[layers.Count];
            double thickSum = 0;
            int thinCount = 0;

            for (int i = 0; i < layers.Count; i++)
            {
                if (IsThin(layers[i], opt)) thinCount++;
                else thickSum += Thickness(layers[i], opt);
            }

            double reserved = thinCount * opt.MinBandM;
            double rest = opt.HeightM - reserved;

            if (rest <= 0 || thickSum <= 0)
            {
                double equal = opt.HeightM / layers.Count;
                for (int i = 0; i < layers.Count; i++) result[i] = equal;
                return result;
            }

            for (int i = 0; i < layers.Count; i++)
            {
                result[i] = IsThin(layers[i], opt)
                    ? opt.MinBandM
                    : rest * Thickness(layers[i], opt) / thickSum;
            }
            return result;
        }

        private static double Thickness(LayerInput layer, CompactOptions opt)
        {
            return layer.ThicknessM * opt.UnitScale;
        }

        private static bool IsThin(LayerInput layer, CompactOptions opt)
        {
            double thickness = Thickness(layer, opt);
            return thickness <= 0 || thickness < opt.MinBandM;
        }
    }
}
