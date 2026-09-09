using System.Collections.Generic;

namespace PaveDetail.Core
{
    // Перенос текста в графе таблицы. Ширина символа оценивается долей от высоты
    // текста - тот же приём, что в PavePlan/Report/ReportTablePlacer: точных метрик
    // шрифта у ядра нет, а завышенная оценка безопаснее заниженной (текст не вылезет).
    public static class TextWrap
    {
        public const double CharWidthFactor = 0.62;

        public static double Width(string text, double textHeight)
        {
            return (text ?? string.Empty).Length * textHeight * CharWidthFactor;
        }

        public static IList<string> Split(string text, double columnWidth, double textHeight)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) { result.Add(string.Empty); return result; }

            var words = text.Split(' ');
            var line = string.Empty;

            foreach (var word in words)
            {
                if (word.Length == 0) continue;

                var candidate = line.Length == 0 ? word : line + " " + word;
                if (Width(candidate, textHeight) <= columnWidth) { line = candidate; continue; }

                if (line.Length > 0) result.Add(line);

                // Слово шире графы переносить некуда: кладём целиком, пусть вылезет,
                // но не зациклимся на попытке его разрезать.
                line = word;
            }

            if (line.Length > 0) result.Add(line);
            if (result.Count == 0) result.Add(string.Empty);
            return result;
        }
    }
}
