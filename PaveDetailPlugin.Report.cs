using System.Collections.Generic;
using System.Windows.Forms;
using PaveDetail.Cad;
using PaveDetail.Core;
using Topomatic.ApplicationPlatform.Plugins;
using Topomatic.Cad.Foundation;

namespace PaveDetail
{
    public partial class PaveDetailPlugin
    {
        [cmd("pavedetail_report")]
        private void BuildReport()
        {
            var cadView = CadView;

            var site = SiteReader.FindSiteModel();
            if (site == null)
            {
                MessageBox.Show("В проекте не найдена площадка.", "Детали покрытий");
                return;
            }

            var reader = new SiteReader(site, SiteReader.LoadMaterialsClassifier());
            var nodes = new List<NodeInput>();
            foreach (var region in reader.Regions())
                nodes.Add(reader.ReadNode(region));

            var rows = ReportBuilder.Build(BatchGrouping.Group(nodes));
            if (rows.Count == 0)
            {
                MessageBox.Show("Конструкций дорожной одежды в площадке не найдено.", "Детали покрытий");
                return;
            }

            var target = DrawTarget.Resolve(cadView);
            if (target == null)
            {
                MessageBox.Show(DrawTarget.NoTargetMessage, "Детали покрытий");
                return;
            }

            string prompt = "Укажите точку вставки ведомости"
                + (target.IsSheet ? " - лист «Чертёж»" : " - план площадки");
            Vector2D insert;
            if (!CadPick.TryPoint(cadView, prompt, out insert)) return;

            var options = new NodeOptions().Scaled(target.UnitScale);
            var shapes = ReportShapes(rows, options);
            new DrawingWriter(target.Drawing).Write(shapes, insert, target.UnitScale);

            cadView.Unlock();
            cadView.Invalidate();
        }

        // Таблица из линий сетки и текста: ширины граф подобраны под формат
        // «№ / наименование / состав / где применена». «Состав»/«Где применена» -
        // список слоёв и список регионов через « · », может быть длиннее графы:
        // текст переносится (TextWrap, как в TableBuilder), высота строки - под
        // самую длинную ячейку, иначе соседние строки наезжают друг на друга.
        private static IList<Shape> ReportShapes(IList<ReportRow> rows, NodeOptions options)
        {
            double h = options.TextHeightM;
            double pad = h * 0.5;
            double step = h * 1.6;
            double headerHeight = h * 2.0;
            double[] widths = { h * 3, h * 16, h * 46, h * 16 };
            double total = 0;
            foreach (var w in widths) total += w;

            var shapes = new List<Shape>();
            var headers = new[] { "№", "Наименование конструкции", "Состав, сверху вниз", "Где применена" };

            double y = 0;
            var rowBoundaries = new List<double> { y };
            AddCells(shapes, headers, widths, y, pad, step, h);
            y -= headerHeight;
            rowBoundaries.Add(y);

            foreach (var r in rows)
            {
                var cells = new[] { r.Number.ToString(), r.Name, r.Composition, r.Regions };
                y -= AddWrappedRow(shapes, cells, widths, y, pad, step, h);
                rowBoundaries.Add(y);
            }

            foreach (var ry in rowBoundaries)
                shapes.Add(new LineShape { X1 = 0, Y1 = ry, X2 = total, Y2 = ry });

            double x = 0;
            for (int i = 0; i <= widths.Length; i++)
            {
                shapes.Add(new LineShape { X1 = x, Y1 = 0, X2 = x, Y2 = y });
                if (i < widths.Length) x += widths[i];
            }

            return shapes;
        }

        // Строит строку с переносом по словам в каждой графе, возвращает высоту строки.
        private static double AddWrappedRow(List<Shape> shapes, string[] cells, double[] widths,
            double top, double pad, double step, double h)
        {
            int maxLines = 1;
            for (int i = 0; i < cells.Length; i++)
            {
                int count = TextWrap.Split(cells[i], widths[i] - pad * 2, h).Count;
                if (count > maxLines) maxLines = count;
            }
            double rowHeight = pad * 2 + maxLines * step;

            AddCells(shapes, cells, widths, top, pad, step, h);
            return rowHeight;
        }

        // Заголовок - одна строка (короче графы, переносить незачем); тело строки -
        // AddWrappedRow уже посчитал высоту под то же самое разбиение TextWrap.Split.
        private static void AddCells(List<Shape> shapes, string[] cells, double[] widths,
            double top, double pad, double step, double h)
        {
            double x = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                var lines = TextWrap.Split(cells[i], widths[i] - pad * 2, h);
                for (int l = 0; l < lines.Count; l++)
                    shapes.Add(new TextShape
                    {
                        X = x + pad, Y = top - pad - h * 0.7 - step * l,
                        Text = lines[l], Height = h
                    });
                x += widths[i];
            }
        }
    }
}
