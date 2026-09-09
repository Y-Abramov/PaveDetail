using PaveDetail.Core;
using Topomatic.Cad.Foundation;
using Topomatic.Cad.View;
using Topomatic.Cad.View.Hints;
using Topomatic.Dwg;
using Topomatic.Dwg.Layer;
using Topomatic.Sites.SiteObjects;

namespace PaveDetail.Cad
{
    // Обёртка над точечным вводом Robur и поиском ближайшего региона по клику.
    internal static class CadPick
    {
        public static Drawing ActiveDrawing(CadView cadView)
        {
            var layer = DrawingLayer.GetDrawingLayer(cadView) as DrawingLayer;
            return layer == null ? null : layer.Drawing;
        }

        public static bool TryPoint(CadView cadView, string prompt, out Vector2D point)
        {
            Vector3D p3;
            if (CadCursors.GetPoint(cadView, out p3, prompt))
            {
                point = new Vector2D(p3.X, p3.Y);
                return true;
            }
            point = new Vector2D(0, 0);
            return false;
        }

        // Регион, чья Position ближе всего к указанной точке - у SiteRegion нет
        // произвольной геометрии контура, доступной для точного попадания в границы.
        public static SiteRegion NearestRegion(SiteReader reader, Vector2D point)
        {
            SiteRegion best = null;
            double bestDist = double.MaxValue;
            foreach (var r in reader.Regions())
            {
                double dx = r.Position.X - point.X;
                double dy = r.Position.Y - point.Y;
                double d = dx * dx + dy * dy;
                if (d < bestDist) { bestDist = d; best = r; }
            }
            return best;
        }
    }
}
