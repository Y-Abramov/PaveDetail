using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PaveDetail.Core
{
    // Обратная сторона MaterialCatalog.Parse / BorderCatalog.Parse: модель -> JSON.
    // Свой писатель, а не JavaScriptSerializer, по двум причинам: сериализатор пишет
    // "scale":null для незаполненных полей и не переносит строки - файл, который
    // пользователь может открыть руками через «Открыть папку», становится нечитаемым.
    //
    // Числа - только через InvariantCulture: у русской локали разделитель запятая,
    // и такой JSON обратно не разберётся.
    public static class CatalogJson
    {
        public static string Materials(IList<MaterialRule> rules)
        {
            var sb = new StringBuilder();
            sb.Append("{\n  \"rules\": [\n");

            for (int i = 0; i < rules.Count; i++)
            {
                var r = rules[i];
                sb.Append("    { \"match\": [");
                var match = r.match ?? new string[0];
                for (int m = 0; m < match.Length; m++)
                {
                    if (m > 0) sb.Append(", ");
                    sb.Append(Str(match[m]));
                }
                sb.Append(']');

                if (!string.IsNullOrEmpty(r.pattern)) sb.Append(", \"pattern\": ").Append(Str(r.pattern));
                if (r.scale.HasValue) sb.Append(", \"scale\": ").Append(Num(r.scale.Value));
                if (r.angle.HasValue) sb.Append(", \"angle\": ").Append(Num(r.angle.Value));
                if (!string.IsNullOrEmpty(r.render)) sb.Append(", \"render\": ").Append(Str(r.render));
                if (r.lineWeight.HasValue) sb.Append(", \"lineWeight\": ").Append(Num(r.lineWeight.Value));
                if (!string.IsNullOrEmpty(r.fullName)) sb.Append(",\n      \"fullName\": ").Append(Str(r.fullName));

                sb.Append(" }");
                if (i < rules.Count - 1) sb.Append(',');
                sb.Append('\n');
            }

            sb.Append("  ]\n}\n");
            return sb.ToString();
        }

        public static string Borders(IList<BorderInput> borders)
        {
            var sb = new StringBuilder();
            sb.Append("{\n  \"borders\": [\n");

            for (int i = 0; i < borders.Count; i++)
            {
                var b = borders[i];
                sb.Append("    { \"name\": ").Append(Str(b.Name))
                  .Append(", \"width\": ").Append(Num(b.WidthM))
                  .Append(", \"height\": ").Append(Num(b.HeightM))
                  .Append(", \"rise\": ").Append(Num(b.RiseM))
                  .Append(",\n      \"bedding\": { \"material\": ").Append(Str(b.BeddingMaterial))
                  .Append(", \"thickness\": ").Append(Num(b.BeddingThicknessM))
                  .Append(", \"overhang\": ").Append(Num(b.BeddingOverhangM))
                  .Append(" } }");
                if (i < borders.Count - 1) sb.Append(',');
                sb.Append('\n');
            }

            sb.Append("  ]\n}\n");
            return sb.ToString();
        }

        private static string Num(double value)
        {
            return value.ToString("0.############", CultureInfo.InvariantCulture);
        }

        private static string Str(string value)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in value ?? string.Empty)
            {
                if (c == '"') sb.Append("\\\"");
                else if (c == '\\') sb.Append("\\\\");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }
    }
}
