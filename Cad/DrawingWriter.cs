using System.Collections.Generic;
using PaveDetail.Core;
using Topomatic.Cad.Foundation;
using Topomatic.Dwg;
using Topomatic.Dwg.Entities;

namespace PaveDetail.Cad
{
    // Единственное место модуля, где Shape превращается в примитивы Robur.
    // Имена членов API подтверждены по рабочему коду PavePlan и runoff:
    // DwgText.Content (не Text), DwgHatch.BoundaryPath + PolylineBoundaryPath,
    // DwgPolyline.Add(BugleVector2D), DwgLayers.IsExists/Add.
    internal sealed class DrawingWriter
    {
        public const string LayerHatch = "PD_HATCH";
        public const string LayerLine = "PD_LINE";
        public const string LayerText = "PD_TEXT";

        private readonly Drawing _drawing;

        public DrawingWriter(Drawing drawing)
        {
            _drawing = drawing;
        }

        // patternScale домножает Scale штриховки из каталога материалов: каталог задаёт
        // плотность узора для чертежа в метрах (0.02-0.03), а геометрия на листе «Чертёж»
        // может быть увеличена в десятки раз - без этого множителя штриховка превращается
        // в почти сплошную заливку.
        public void Write(IList<Shape> shapes, Vector2D insert, double patternScale = 1.0)
        {
            if (shapes == null || shapes.Count == 0) return;

            _drawing.BeginUpdate();
            try
            {
                var hatchLayer = EnsureLayer(LayerHatch);
                var lineLayer = EnsureLayer(LayerLine);
                var textLayer = EnsureLayer(LayerText);

                // Примитивы сначала собираются целиком и только потом уходят в чертёж:
                // сбой на середине сборки не оставит в чертеже половины узла.
                var made = new List<DwgEntity>();
                foreach (var shape in shapes)
                {
                    var rect = shape as RectShape;
                    if (rect != null) { MakeRect(made, rect, insert, hatchLayer, lineLayer, patternScale); continue; }

                    var line = shape as LineShape;
                    if (line != null) { made.Add(MakeLine(line, insert, lineLayer)); continue; }

                    var text = shape as TextShape;
                    if (text != null) { made.Add(MakeText(text, insert, textLayer)); }
                }

                var space = _drawing.ActiveSpace;
                foreach (var entity in made) space.Add(entity);
            }
            finally
            {
                _drawing.EndUpdate();
            }
        }

        private DwgLayer EnsureLayer(string name)
        {
            return _drawing.Layers.IsExists(name) ? _drawing.Layers[name] : _drawing.Layers.Add(name);
        }

        private static void MakeRect(List<DwgEntity> made, RectShape r, Vector2D o,
            DwgLayer hatchLayer, DwgLayer lineLayer, double patternScale)
        {
            var corners = new[]
            {
                new Vector2D(o.X + r.Left,  o.Y + r.Bottom),
                new Vector2D(o.X + r.Right, o.Y + r.Bottom),
                new Vector2D(o.X + r.Right, o.Y + r.Top),
                new Vector2D(o.X + r.Left,  o.Y + r.Top)
            };

            var hatch = new DwgHatch();
            var style = r.Style ?? new LayerStyle();
            if (style.UseLayerColor)
            {
                // Правило материала не сработало: заливаем сплошняком цветом слоя из модели.
                hatch.PatternName = "SOLID";
                if (r.ColorArgb != 0)
                    hatch.Color = new CadColor(System.Drawing.Color.FromArgb(r.ColorArgb));
            }
            else
            {
                hatch.PatternName = style.PatternName;
                hatch.PatternScale = (style.Scale <= 0.0 ? 1.0 : style.Scale) * patternScale;
                hatch.PatternAngle = style.Angle;
            }

            var path = new PolylineBoundaryPath();
            foreach (var c in corners) path.Add(new BugleVector2D(c));
            path.IsClosed = true;
            hatch.BoundaryPath.Add(path);
            hatch.Layer = hatchLayer;
            made.Add(hatch);

            var outline = new DwgPolyline();
            foreach (var c in corners) outline.Add(new BugleVector2D(c));
            outline.Closed = true;
            outline.Layer = lineLayer;
            made.Add(outline);
        }

        private static DwgPolyline MakeLine(LineShape l, Vector2D o, DwgLayer layer)
        {
            var pl = new DwgPolyline();
            pl.Add(new BugleVector2D(new Vector2D(o.X + l.X1, o.Y + l.Y1)));
            pl.Add(new BugleVector2D(new Vector2D(o.X + l.X2, o.Y + l.Y2)));
            pl.Layer = layer;
            return pl;
        }

        private static DwgText MakeText(TextShape t, Vector2D o, DwgLayer layer)
        {
            var txt = new DwgText();
            txt.Content = t.Text;                       // свойство Content, не Text
            txt.Height = t.Height;
            txt.Position = new Vector3D(o.X + t.X, o.Y + t.Y, 0.0);
            txt.Layer = layer;
            return txt;
        }
    }
}
