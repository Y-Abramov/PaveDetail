using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PaveDetail.Core
{
    public sealed class ReportRow
    {
        public int Number;
        public string Name;
        public string Composition;
        public string Regions;
    }

    public static class ReportBuilder
    {
        public static IList<ReportRow> Build(BatchResult batch)
        {
            var rows = new List<ReportRow>();
            for (int i = 0; i < batch.Groups.Count; i++)
            {
                var g = batch.Groups[i];
                rows.Add(new ReportRow
                {
                    Number = i + 1,
                    Name = g.RegionNames.Count > 0 ? g.RegionNames[0] : "Конструкция",
                    Composition = Compose(g.Sample),
                    Regions = string.Join(", ", g.RegionNames.ToArray())
                });
            }
            return rows;
        }

        private static string Compose(NodeInput node)
        {
            var sb = new StringBuilder();
            foreach (var l in node.Layers)
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append((l.Material ?? string.Empty).Trim());
                if (l.ThicknessM > 0)
                {
                    int mm = (int)Math.Round(l.ThicknessM * 1000.0, MidpointRounding.AwayFromZero);
                    sb.Append(' ').Append(mm.ToString(CultureInfo.InvariantCulture)).Append(" мм");
                }
            }
            return sb.ToString();
        }
    }
}
