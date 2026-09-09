using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PaveDetail.Core
{
    // Состав дорожной одежды Robur хранит строкой формата Topomatic.Sites.MaterialsLayers:
    // "{толщина}|{материал};{толщина}|{материал};...". Толщина - float в текущей культуре
    // (у русской локали разделитель - запятая), материал - либо код классификатора
    // ("SmdxGenplanSmallAsphalt"), либо уже готовое русское имя.
    //
    // Ищем такую строку по СОДЕРЖИМОМУ, а не по тегу: тег и имя поля отличаются между
    // источниками (модификатор «Слои ДО» пишет в семантику слоя SitePavement, поле
    // «Контроллер КДО» - в семантику региона и патча поверхности) и между версиями Robur.
    internal static class MaterialsLayersScan
    {
        // Тег в схеме семантики, под которым лежит «Контроллер КДО» - для прямого доступа
        // через SemanticDataSet.TryGetValue(tag, ...), когда он срабатывает.
        public const string Tag = "MaterialsLayers";

        private static readonly Regex Pattern =
            new Regex(@"^\s*\d+([.,]\d+)?\|[^;|]+(\s*;\s*\d+([.,]\d+)?\|[^;|]+)*\s*;?\s*$");

        public static bool IsMaterialsLayers(string value)
        {
            return !string.IsNullOrEmpty(value) && Pattern.IsMatch(value);
        }

        public static string Pick(IEnumerable<string> candidates)
        {
            if (candidates == null) return null;
            foreach (var candidate in candidates)
                if (IsMaterialsLayers(candidate)) return candidate;
            return null;
        }
    }
}
