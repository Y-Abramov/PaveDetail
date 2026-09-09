using System;
using System.Collections.Generic;

namespace PaveDetail.Core
{
    // Слой → оформление. Промахи не глотаются: диалог показывает их пользователю
    // и предлагает выбрать штриховку вручную.
    public sealed class MaterialResolver
    {
        private readonly MaterialCatalog _catalog;
        private readonly List<string> _misses = new List<string>();

        public MaterialResolver(MaterialCatalog catalog)
        {
            _catalog = catalog ?? MaterialCatalog.BuiltIn();
        }

        public IList<string> Misses { get { return _misses; } }

        public LayerStyle Resolve(LayerInput layer)
        {
            var name = (layer == null ? null : layer.Material) ?? string.Empty;
            var probe = name.ToLowerInvariant();

            foreach (var rule in _catalog.Rules)
            {
                if (rule == null || rule.match == null) continue;
                foreach (var key in rule.match)
                {
                    if (string.IsNullOrEmpty(key)) continue;
                    if (probe.IndexOf(key.ToLowerInvariant(), StringComparison.Ordinal) < 0) continue;

                    var style = new LayerStyle();
                    if (string.Equals(rule.render, "line", StringComparison.OrdinalIgnoreCase))
                    {
                        style.RenderAsLine = true;
                        style.LineWeight = rule.lineWeight ?? 3;
                    }
                    else
                    {
                        style.PatternName = rule.pattern ?? "SOLID";
                        style.Scale = rule.scale ?? 0.02;
                        style.Angle = rule.angle ?? 0;
                    }
                    return style;
                }
            }

            if (!string.IsNullOrWhiteSpace(name) && !_misses.Contains(name))
                _misses.Add(name);

            return new LayerStyle { PatternName = "SOLID", UseLayerColor = true };
        }
    }
}
