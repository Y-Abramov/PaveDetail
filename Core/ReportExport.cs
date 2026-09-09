using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PaveDetail.Core
{
    // Та же ведомость файлом. Чистый текст, без ссылок на Topomatic - тестируется без Robur.
    public static class ReportExport
    {
        private static readonly string[] Headers =
            { "№", "Наименование конструкции", "Состав, сверху вниз", "Где применена" };

        private static readonly string[] TableHeaders =
        {
            "Тип покр.", "Раздел", "Наименование покрытия", "Материал слоя",
            "Толщ. слоя, мм", "Модуль упругости, МПа", "Примечание"
        };

        public static string ToCsv(IList<ReportRow> rows)
        {
            var sb = new StringBuilder();
            sb.Append(string.Join(";", Headers)).Append('\n');
            foreach (var r in rows)
            {
                sb.Append(Csv(r.Number.ToString(CultureInfo.InvariantCulture))).Append(';')
                  .Append(Csv(r.Name)).Append(';')
                  .Append(Csv(r.Composition)).Append(';')
                  .Append(Csv(r.Regions)).Append('\n');
            }
            return sb.ToString();
        }

        public static string ToHtml(IList<ReportRow> rows)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>\n<html lang=\"ru\">\n<head>\n<meta charset=\"utf-8\">\n");
            sb.Append("<title>Ведомость состава конструкций</title>\n");
            sb.Append("<style>body{font:14px/1.5 Segoe UI,Arial,sans-serif;margin:24px}");
            sb.Append("table{border-collapse:collapse;width:100%}");
            sb.Append("th,td{border:1px solid #94a3b8;padding:6px 8px;text-align:left;vertical-align:top}");
            sb.Append("th{background:#0891B2;color:#fff}</style>\n</head>\n<body>\n");
            sb.Append("<h1>Ведомость состава конструкций</h1>\n<table>\n<thead><tr>");
            foreach (var h in Headers) sb.Append("<th>").Append(Html(h)).Append("</th>");
            sb.Append("</tr></thead>\n<tbody>\n");

            foreach (var r in rows)
            {
                sb.Append("<tr><td>").Append(r.Number.ToString(CultureInfo.InvariantCulture))
                  .Append("</td><td>").Append(Html(r.Name))
                  .Append("</td><td>").Append(Html(r.Composition))
                  .Append("</td><td>").Append(Html(r.Regions))
                  .Append("</td></tr>\n");
            }

            sb.Append("</tbody>\n</table>\n<p>ABR | Детали покрытий, ")
              .Append(DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
              .Append("</p>\n</body>\n</html>\n");
            return sb.ToString();
        }

        // RFC 4180: поле берётся в кавычки, если содержит разделитель, кавычку или перевод строки.
        private static string Csv(string value)
        {
            var v = value ?? string.Empty;
            if (v.IndexOf(';') < 0 && v.IndexOf('"') < 0 && v.IndexOf('\n') < 0) return v;
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }

        private static string Html(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        // Строка файла = один слой: шифр, раздел, наименование, модуль и примечание
        // относятся к конструкции целиком и повторяются в каждой её строке, чтобы файл
        // открывался в Excel как плоская таблица и фильтровался по любой графе.
        // Графа «Сечение» не выгружается - это рисунок.
        public static string TableToCsv(IList<TableRow> rows)
        {
            var sb = new StringBuilder();
            sb.Append(string.Join(";", TableHeaders)).Append('\n');

            if (rows != null)
                foreach (var r in rows)
                    foreach (var l in r.Layers)
                        sb.Append(Csv(r.Code)).Append(';')
                          .Append(Csv(r.Section)).Append(';')
                          .Append(Csv(r.Name)).Append(';')
                          .Append(Csv(l.Text)).Append(';')
                          .Append(Csv(l.ThicknessMm)).Append(';')
                          .Append(Csv(r.Modulus)).Append(';')
                          .Append(Csv(r.Note)).Append('\n');

            return sb.ToString();
        }

        public static string TableToHtml(IList<TableRow> rows)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>\n<html lang=\"ru\">\n<head>\n<meta charset=\"utf-8\">\n");
            sb.Append("<title>Детали покрытий</title>\n");
            sb.Append("<style>body{font:14px/1.5 Segoe UI,Arial,sans-serif;margin:24px}");
            sb.Append("table{border-collapse:collapse;width:100%}");
            sb.Append("th,td{border:1px solid #94a3b8;padding:6px 8px;text-align:left;vertical-align:top}");
            sb.Append("th{background:#0891B2;color:#fff}</style>\n</head>\n<body>\n");
            sb.Append("<h1>Детали покрытий</h1>\n<table>\n<thead><tr>");
            foreach (var h in TableHeaders) sb.Append("<th>").Append(Html(h)).Append("</th>");
            sb.Append("</tr></thead>\n<tbody>\n");

            if (rows != null)
                foreach (var r in rows)
                    foreach (var l in r.Layers)
                        sb.Append("<tr><td>").Append(Html(r.Code))
                          .Append("</td><td>").Append(Html(r.Section))
                          .Append("</td><td>").Append(Html(r.Name))
                          .Append("</td><td>").Append(Html(l.Text))
                          .Append("</td><td>").Append(Html(l.ThicknessMm))
                          .Append("</td><td>").Append(Html(r.Modulus))
                          .Append("</td><td>").Append(Html(r.Note))
                          .Append("</td></tr>\n");

            sb.Append("</tbody>\n</table>\n<p>ABR | Детали покрытий, ")
              .Append(DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
              .Append("</p>\n</body>\n</html>\n");
            return sb.ToString();
        }
    }
}
