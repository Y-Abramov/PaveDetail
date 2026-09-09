using System;
using System.Collections.Generic;
using System.Globalization;

namespace PaveDetail.Core
{
    // Группы конструкций площадки + данные проекта → строки листа.
    public static class TableRowFactory
    {
        public static IList<TableRow> Build(BatchResult batch, ProjectData data, MaterialCatalog catalog)
        {
            var rows = new List<TableRow>();
            if (batch == null) return rows;

            foreach (var group in batch.Groups)
            {
                var saved = data == null ? null : data.Find(group.Key);
                var row = new TableRow
                {
                    Key = group.Key,
                    Code = saved == null ? string.Empty : (saved.code ?? string.Empty),
                    Section = saved == null ? string.Empty : (saved.section ?? string.Empty),
                    Modulus = saved == null ? string.Empty : (saved.modulus ?? string.Empty),
                    Note = saved == null ? string.Empty : (saved.note ?? string.Empty)
                };

                row.Name = saved != null && !string.IsNullOrWhiteSpace(saved.name)
                    ? saved.name
                    : (group.RegionNames.Count > 0 ? group.RegionNames[0] : "Конструкция");

                for (int i = 0; i < group.Sample.Layers.Count; i++)
                {
                    var layer = group.Sample.Layers[i];
                    row.Layers.Add(new TableLayer
                    {
                        Text = MaterialNaming.Of(layer, i + 1, saved, catalog),
                        ThicknessMm = Millimetres(layer.ThicknessM),
                        Source = layer
                    });
                }

                rows.Add(row);
            }

            return SortBySection(rows);
        }

        public static string Millimetres(double thicknessM)
        {
            if (thicknessM <= 0) return "-";
            int mm = (int)Math.Round(thicknessM * 1000.0, MidpointRounding.AwayFromZero);
            return mm.ToString(CultureInfo.InvariantCulture);
        }

        // Разделы идут в порядке первого появления, внутри раздела - порядок групп.
        private static IList<TableRow> SortBySection(IList<TableRow> rows)
        {
            var order = new List<string>();
            foreach (var r in rows)
            {
                var key = r.Section ?? string.Empty;
                if (!order.Contains(key)) order.Add(key);
            }

            var sorted = new List<TableRow>();
            foreach (var section in order)
                foreach (var r in rows)
                    if ((r.Section ?? string.Empty) == section) sorted.Add(r);
            return sorted;
        }
    }
}
