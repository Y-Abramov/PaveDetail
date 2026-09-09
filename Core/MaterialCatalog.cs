using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace PaveDetail.Core
{
    public sealed class MaterialRule
    {
        public string[] match;
        public string pattern;
        public double? scale;
        public double? angle;
        public string render;       // "line" - рисовать линией
        public double? lineWeight;
        public string fullName;     // формулировка по ГОСТ для графы «Материал слоя»
    }

    public sealed class MaterialCatalog
    {
        public List<MaterialRule> Rules = new List<MaterialRule>();
        public LayerStyle Fallback = new LayerStyle { PatternName = "SOLID", UseLayerColor = true };
        public string LoadError;    // непусто, если JSON не разобрался

        // Встроенные правила - тот же набор, что уезжает в Catalog/materials.json.
        // Имена штриховок подтверждены: ими рисует свои разрезы Topomatic.Culverts.dll.
        // Порядок значим: «асфальтобетон» должен стоять раньше «бетон».
        public static MaterialCatalog BuiltIn()
        {
            var c = new MaterialCatalog();
            c.Rules.Add(new MaterialRule { match = new[] { "асфальтобетон", "а/б" }, pattern = "ANSI31", scale = 0.02,
                fullName = "Горячая смесь для плотного асфальтобетона мелкозернистого тип А марки I ГОСТ 9128-2013" });
            c.Rules.Add(new MaterialRule { match = new[] { "щебен", "гравий" }, pattern = "GRAVEL", scale = 0.03,
                fullName = "Щебень, уложенный по способу заклинки, ГОСТ 8267-93" });
            c.Rules.Add(new MaterialRule { match = new[] { "песок", "псч", "пгс", "песчан" }, pattern = "AR-SAND", scale = 0.02,
                fullName = "Песок природный, зернистость 0...7 мм, ГОСТ 8736-2014" });
            c.Rules.Add(new MaterialRule { match = new[] { "бетон", "цементн", "плитк", "брусчат", "камень мощения" }, pattern = "AR-CONC", scale = 0.02,
                fullName = "Плитка тротуарная бетонная ГОСТ 17608-2017" });
            c.Rules.Add(new MaterialRule { match = new[] { "геотекстиль", "геосетк", "георешет", "плён", "плен", "мембран" }, render = "line", lineWeight = 3,
                fullName = "Геотекстиль нетканый иглопробивной" });
            c.Rules.Add(new MaterialRule { match = new[] { "грунт" }, pattern = "EARTH", scale = 0.03 });
            return c;
        }

        public static MaterialCatalog Parse(string json)
        {
            try
            {
                var ser = new JavaScriptSerializer();
                var dto = ser.Deserialize<Dto>(json);
                if (dto == null || dto.rules == null || dto.rules.Count == 0)
                    throw new InvalidOperationException("пустой каталог");

                var c = new MaterialCatalog();
                c.Rules = dto.rules;
                return c;
            }
            catch (Exception ex)
            {
                // Битый каталог не должен ронять модуль: работаем на встроенных правилах,
                // а факт сбоя показываем в диалоге каталогов.
                var c = BuiltIn();
                c.LoadError = ex.Message;
                return c;
            }
        }

        private sealed class Dto
        {
            public List<MaterialRule> rules { get; set; }
        }
    }
}
