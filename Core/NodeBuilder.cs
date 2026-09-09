using System;
using System.Collections.Generic;
using System.Globalization;

namespace PaveDetail.Core
{
    public sealed class NodeBuilder
    {
        public const string SubgradeCaption = "Уплотнённый грунт основания";

        private readonly MaterialResolver _resolver;
        private readonly NodeOptions _opt;

        public NodeBuilder(MaterialResolver resolver, NodeOptions options)
        {
            _resolver = resolver;
            _opt = options ?? new NodeOptions();
        }

        public IList<Shape> Build(NodeInput input)
        {
            var shapes = new List<Shape>();
            var numbered = new List<Anchor>();

            if (input.Kind == NodeKind.Junction && input.Border != null)
            {
                // Борт стоит по нулю, левая конструкция уходит влево от него, правая - вправо.
                double borderLeft = -(input.Border.WidthM * _opt.UnitScale);
                double leftStackRight = borderLeft;
                double leftStackLeft = borderLeft - _opt.WidthM;

                double leftBottom = AddStack(shapes, numbered, input.Layers, leftStackLeft, leftStackRight, 0);
                AddBorder(shapes, numbered, input.Border, 0, leftBottom);
                AddStack(shapes, numbered, input.RightLayers, 0, _opt.WidthM, 0);

                shapes.Add(new TextShape
                {
                    X = leftStackLeft, Y = _opt.TextHeightM * 2.5,
                    Text = input.RegionName ?? string.Empty,
                    Height = _opt.TextHeightM, Bold = true
                });
                shapes.Add(new TextShape
                {
                    X = 0, Y = _opt.TextHeightM * 2.5,
                    Text = input.RightRegionName ?? string.Empty,
                    Height = _opt.TextHeightM, Bold = true
                });

                AddNumbersAndExplication(shapes, numbered, _opt.WidthM, input, addTitle: false);
                return shapes;
            }

            double bottomOfLayers = AddStack(shapes, numbered, input.Layers, 0, _opt.WidthM, 0);

            if (input.Kind == NodeKind.WithBorder && input.Border != null)
                AddBorder(shapes, numbered, input.Border, 0, bottomOfLayers);

            AddNumbersAndExplication(shapes, numbered, _opt.WidthM, input, addTitle: true);
            return shapes;
        }

        // Куда встаёт цифра номера: своя грань у каждой пачки узла.
        private sealed class Anchor
        {
            public string Caption;
            public double X;      // середина пачки
            public double Right;  // правая грань пачки - у неё стоит цифра номера
            public double Y;
        }

        // Один материал - один номер: повтор в другой половине узла ссылается на тот же номер.
        private static void AddNumbered(List<Anchor> numbered, string caption,
            double x, double right, double y)
        {
            foreach (var t in numbered)
                if (string.Equals(t.Caption, caption, StringComparison.OrdinalIgnoreCase))
                    return;
            numbered.Add(new Anchor { Caption = caption, X = x, Right = right, Y = y });
        }

        // Возвращает Y низа пачки, включая полосу грунта основания.
        private double AddStack(List<Shape> shapes, List<Anchor> numbered,
            IList<LayerInput> layers, double left, double right, double topY)
        {
            double y = topY;
            double anchorX = (left + right) / 2.0;   // середина СВОЕЙ пачки
            foreach (var layer in layers)
            {
                var style = _resolver.Resolve(layer);
                double thickness = layer.ThicknessM * _opt.UnitScale;
                if (thickness <= 0 || style.RenderAsLine)
                {
                    shapes.Add(new LineShape
                    {
                        X1 = left, Y1 = y, X2 = right, Y2 = y,
                        Weight = style.LineWeight, IsLayer = true
                    });
                    AddNumbered(numbered, Caption(layer), anchorX, right, y);
                }
                else
                {
                    double bottom = y - thickness;
                    shapes.Add(new RectShape
                    {
                        Left = left, Right = right, Top = y, Bottom = bottom,
                        Style = style, ColorArgb = layer.ColorArgb
                    });
                    AddNumbered(numbered, Caption(layer), anchorX, right, (y + bottom) / 2.0);
                    y = bottom;
                }
            }

            // Грунт основания - всегда последняя полоса, в модели его нет.
            double subgradeBottom = y - _opt.SubgradeThicknessM;
            shapes.Add(new RectShape
            {
                Left = left, Right = right, Top = y, Bottom = subgradeBottom,
                Style = new LayerStyle { PatternName = "EARTH", Scale = 0.03 }
            });
            AddNumbered(numbered, SubgradeCaption, anchorX, right, (y + subgradeBottom) / 2.0);
            return subgradeBottom;
        }

        // Борт строится влево от stackLeft: правая грань камня совпадает с левой гранью пачки.
        private void AddBorder(List<Shape> shapes, List<Anchor> numbered,
            BorderInput border, double stackLeft, double stackBottom)
        {
            double k = _opt.UnitScale;
            double stoneRight = stackLeft;
            double stoneLeft = stackLeft - border.WidthM * k;
            double stoneTop = border.RiseM * k;
            double stoneBottom = stoneTop - border.HeightM * k;

            shapes.Add(new RectShape
            {
                Left = stoneLeft, Right = stoneRight, Top = stoneTop, Bottom = stoneBottom,
                Style = _resolver.Resolve(new LayerInput { Material = "Бетон", ThicknessM = border.HeightM })
            });
            AddNumbered(numbered, "Бортовой камень " + border.Name,
                (stoneLeft + stoneRight) / 2.0, stoneRight, (stoneTop + stoneBottom) / 2.0);

            double bedTop = stoneBottom;
            double bedBottom = bedTop - border.BeddingThicknessM * k;
            double bedLeft = stoneLeft - border.BeddingOverhangM * k;
            double bedRight = stoneRight + border.BeddingOverhangM * k;

            shapes.Add(new RectShape
            {
                Left = bedLeft, Right = bedRight, Top = bedTop, Bottom = bedBottom,
                Style = _resolver.Resolve(new LayerInput { Material = border.BeddingMaterial, ThicknessM = border.BeddingThicknessM })
            });
            AddNumbered(numbered, border.BeddingMaterial ?? "Обойма",
                (bedLeft + bedRight) / 2.0, bedRight, (bedTop + bedBottom) / 2.0);
        }

        // Номера слоёв ставятся голыми цифрами у правой грани своей пачки, строка расшифровки -
        // напротив своего слоя. Полок и стрелок нет: оформление повторяет табличный эталон
        // заказчика, где выносок не бывает.
        private void AddNumbersAndExplication(List<Shape> shapes,
            List<Anchor> numbered, double right, NodeInput input, bool addTitle)
        {
            double explicationX = right + _opt.ExplicationGapM;

            // В узле сопряжения заголовка нет - вместо него две подписи регионов.
            if (addTitle)
            {
                shapes.Add(new TextShape
                {
                    X = 0, Y = _opt.TextHeightM * 2.5,
                    Text = "Конструкция дорожной одежды. " + (input.RegionName ?? string.Empty),
                    Height = _opt.TextHeightM, Bold = true
                });
            }

            double h = _opt.TextHeightM;
            double minStep = h * 1.6;             // ближе строки расшифровки слипаются
            double previousLine = double.MaxValue;

            for (int i = 0; i < numbered.Count; i++)
            {
                int number = i + 1;
                double baseline = numbered[i].Y - h * 0.4;   // текст пишется от базовой линии

                shapes.Add(new TextShape
                {
                    // Цифра прижата к правой грани ТОЙ пачки, где материал лежит: в узле
                    // сопряжения у половин разные толщины, и общая колонка цифр без полок
                    // не дала бы понять, к какому слою относится номер.
                    X = numbered[i].Right + h * 0.5,
                    Y = baseline,
                    Text = number.ToString(CultureInfo.InvariantCulture),
                    Height = h,
                    IsNumber = true
                });

                // Строка расшифровки стоит напротив своего слоя. Слои идут сверху вниз,
                // поэтому на тонких слоях строка сдвигается вниз ровно настолько, чтобы
                // не наехать на предыдущую: порядок строк при этом сохраняется.
                double lineY = baseline;
                if (previousLine - lineY < minStep) lineY = previousLine - minStep;
                previousLine = lineY;

                shapes.Add(new TextShape
                {
                    X = explicationX,
                    Y = lineY,
                    Text = number.ToString(CultureInfo.InvariantCulture) + ". " + numbered[i].Caption,
                    Height = h,
                    IsExplication = true
                });
            }
        }

        private static string Caption(LayerInput layer)
        {
            var name = (layer.Material ?? string.Empty).Trim();
            if (layer.ThicknessM <= 0) return name;
            int mm = (int)Math.Round(layer.ThicknessM * 1000.0, MidpointRounding.AwayFromZero);
            return name + ", " + mm.ToString(CultureInfo.InvariantCulture) + " мм";
        }
    }
}
