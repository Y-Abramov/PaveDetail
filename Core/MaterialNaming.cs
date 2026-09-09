using System;

namespace PaveDetail.Core
{
    // Имя слоя для графы «Материал слоя». Три источника, порядок важен: то, что человек
    // вписал в этом проекте, всегда сильнее каталога, а каталог - сильнее модели, где
    // лежит короткое имя классификатора.
    public static class MaterialNaming
    {
        public static string Of(LayerInput layer, int index, ProjectConstruction project, MaterialCatalog catalog)
        {
            var fromModel = (layer == null ? null : layer.Material) ?? string.Empty;

            if (project != null)
            {
                var own = project.LayerText(index);
                if (!string.IsNullOrWhiteSpace(own)) return own.Trim();
            }

            if (catalog != null)
            {
                var probe = fromModel.ToLowerInvariant();
                foreach (var rule in catalog.Rules)
                {
                    if (rule == null || rule.match == null || string.IsNullOrWhiteSpace(rule.fullName)) continue;
                    foreach (var key in rule.match)
                    {
                        if (string.IsNullOrEmpty(key)) continue;
                        if (probe.IndexOf(key.ToLowerInvariant(), StringComparison.Ordinal) >= 0)
                            return rule.fullName;
                    }
                }
            }

            return fromModel.Trim();
        }
    }
}
