using System;
using System.Collections.Generic;
using System.Text;

namespace PaveDetail.Core
{
    // Ключ уникальности конструкции: упорядоченный список пар «материал + толщина в мм».
    // Нужен, чтобы сгруппировать регионы с одинаковой одеждой в один узел и одну строку ведомости.
    public static class ConstructionKey
    {
        public static string Of(IEnumerable<LayerInput> layers)
        {
            if (layers == null) return string.Empty;

            var sb = new StringBuilder();
            foreach (var l in layers)
            {
                var name = (l.Material ?? string.Empty).Trim().ToLowerInvariant();
                var mm = (int)Math.Round(l.ThicknessM * 1000.0, MidpointRounding.AwayFromZero);
                if (sb.Length > 0) sb.Append('|');
                sb.Append(name).Append('#').Append(mm);
            }
            return sb.ToString();
        }
    }
}
