using System.Collections.Generic;

namespace PaveDetail.Core
{
    // Лист «Детали покрытий»: сетка линиями и текст, как в PavePlan и Runoff -
    // сущности таблицы в Topomatic.Dwg нет, есть только стили таблиц.
    public sealed class TableBuilder
    {
        public static readonly string[] Headers =
        {
            "Тип покр.", "Наименование покрытия", "Сечение", "Материал слоя",
            "Толщ. слоя, мм", "Модуль упругости, МПа", "Примечание"
        };

        private readonly MaterialResolver _resolver;
        private readonly TableOptions _opt;

        public TableBuilder(MaterialResolver resolver, TableOptions options)
        {
            _resolver = resolver;
            _opt = options ?? new TableOptions();
        }

        public IList<Shape> Build(IList<TableRow> rows)
        {
            var shapes = new List<Shape>();
            if (rows == null || rows.Count == 0) return shapes;

            double h = _opt.TextHeightM;
            double pad = h * _opt.PaddingFactor;
            var widths = _opt.Widths();
            double totalWidth = 0;
            foreach (var w in widths) totalWidth += w;

            var x = new double[widths.Length + 1];
            for (int i = 0; i < widths.Length; i++) x[i + 1] = x[i] + widths[i];

            double y = 0;
            var rowBoundaries = new List<double> { y };

            // Шапка: подпись графы - одна строка целиком, без переноса по словам
            // (заголовки короткие, перенос вроде «Тип» / «покр.» только портит лист).
            double headerHeight = pad * 2 + h * _opt.LineStepFactor;
            for (int c = 0; c < Headers.Length; c++)
                shapes.Add(new TextShape
                {
                    X = x[c] + pad,
                    Y = y - pad - h * 0.7,
                    Text = Headers[c], Height = h, IsHeader = true
                });
            y -= headerHeight;
            rowBoundaries.Add(y);

            string currentSection = null;
            foreach (var row in rows)
            {
                if (!string.IsNullOrEmpty(row.Section) && row.Section != currentSection)
                {
                    currentSection = row.Section;
                    double sectionRow = h * 2.0;
                    shapes.Add(new TextShape
                    {
                        X = totalWidth / 2.0 - TextWrap.Width(currentSection, h) / 2.0,
                        Y = y - sectionRow + h * 0.6,
                        Text = currentSection, Height = h, IsSectionTitle = true
                    });
                    y -= sectionRow;
                    rowBoundaries.Add(y);
                }

                y -= AddRow(shapes, row, x, widths, y);
                rowBoundaries.Add(y);
            }

            AddGrid(shapes, x, rowBoundaries, y, totalWidth);
            return shapes;
        }

        // Возвращает высоту построенной строки.
        private double AddRow(List<Shape> shapes, TableRow row, double[] x, double[] widths, double top)
        {
            double h = _opt.TextHeightM;
            double pad = h * _opt.PaddingFactor;
            double step = h * _opt.LineStepFactor;

            // Сначала считаем, сколько строк текста занимает список слоёв: от этого
            // зависит высота всей строки таблицы.
            var layerLines = new List<IList<string>>();
            int textLines = 0;
            for (int i = 0; i < row.Layers.Count; i++)
            {
                var caption = (i + 1) + ". " + (row.Layers[i].Text ?? string.Empty);
                var lines = TextWrap.Split(caption, widths[3] - pad * 2, h);
                layerLines.Add(lines);
                textLines += lines.Count;
            }

            double listHeight = pad * 2 + textLines * step;
            double sectionHeight = pad * 2 + _opt.SectionHeightM;
            double rowHeight = listHeight > sectionHeight ? listHeight : sectionHeight;

            // Графы с одной величиной на строку
            AddCell(shapes, row.Code, x[0] + pad, top - pad - h * 0.4 - step * 0.5, widths[0] - pad * 2, h);
            AddCell(shapes, row.Name, x[1] + pad, top - pad - h * 0.4 - step * 0.5, widths[1] - pad * 2, h);
            AddCell(shapes, row.Modulus, x[5] + pad, top - pad - h * 0.4 - step * 0.5, widths[5] - pad * 2, h);
            AddCell(shapes, row.Note, x[6] + pad, top - pad - h * 0.4 - step * 0.5, widths[6] - pad * 2, h);

            // Мини-разрез: строится в своих координатах и переносится в графу «Сечение».
            var source = new List<LayerInput>();
            foreach (var l in row.Layers) if (l.Source != null) source.Add(l.Source);

            var compact = CompactSection.Build(source, _resolver, new CompactOptions
            {
                WidthM = widths[2] - pad * 4,
                HeightM = _opt.SectionHeightM,
                MinBandM = _opt.MinBandM,
                TextHeightM = h * 0.7,
                UnitScale = _opt.UnitScale
            });
            Translate(shapes, compact, x[2] + pad, top - pad);

            // Список слоёв и толщины: толщина стоит на первой строке своего пункта.
            double lineY = top - pad - h * 0.4;
            for (int i = 0; i < layerLines.Count; i++)
            {
                for (int l = 0; l < layerLines[i].Count; l++)
                {
                    lineY -= step;
                    shapes.Add(new TextShape
                    {
                        X = x[3] + pad, Y = lineY, Text = layerLines[i][l], Height = h
                    });

                    if (l == 0)
                        shapes.Add(new TextShape
                        {
                            X = x[4] + pad, Y = lineY,
                            Text = row.Layers[i].ThicknessMm ?? "-", Height = h
                        });
                }
            }

            return rowHeight;
        }

        private void AddCell(List<Shape> shapes, string text, double left, double top, double width, double h)
        {
            if (string.IsNullOrEmpty(text)) return;
            var lines = TextWrap.Split(text, width, h);
            for (int i = 0; i < lines.Count; i++)
                shapes.Add(new TextShape
                {
                    X = left, Y = top - h * _opt.LineStepFactor * i, Text = lines[i], Height = h
                });
        }

        private static void Translate(List<Shape> target, IList<Shape> source, double dx, double dy)
        {
            foreach (var s in source)
            {
                var rect = s as RectShape;
                if (rect != null)
                {
                    target.Add(new RectShape
                    {
                        Left = rect.Left + dx, Right = rect.Right + dx,
                        Top = rect.Top + dy, Bottom = rect.Bottom + dy,
                        Style = rect.Style, ColorArgb = rect.ColorArgb
                    });
                    continue;
                }

                var text = s as TextShape;
                if (text != null)
                {
                    target.Add(new TextShape
                    {
                        X = text.X + dx, Y = text.Y + dy, Text = text.Text,
                        Height = text.Height, IsNumber = text.IsNumber
                    });
                    continue;
                }

                var line = s as LineShape;
                if (line != null)
                    target.Add(new LineShape
                    {
                        X1 = line.X1 + dx, Y1 = line.Y1 + dy,
                        X2 = line.X2 + dx, Y2 = line.Y2 + dy,
                        Weight = line.Weight, IsLayer = line.IsLayer
                    });
            }
        }

        private static void AddGrid(List<Shape> shapes, double[] x, IList<double> rowBoundaries,
            double bottom, double totalWidth)
        {
            foreach (var y in rowBoundaries)
                shapes.Add(new LineShape { X1 = 0, Y1 = y, X2 = totalWidth, Y2 = y });

            foreach (var vx in x)
                shapes.Add(new LineShape { X1 = vx, Y1 = 0, X2 = vx, Y2 = bottom });
        }
    }
}
