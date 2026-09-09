using System;
using System.Collections.Generic;
using System.IO;
using PaveDetail.Core;
using Topomatic.ApplicationPlatform;
using Topomatic.ApplicationPlatform.Core;
using Topomatic.ApplicationPlatform.Plugins;
using Topomatic.Sfc;
using Topomatic.Sites;
using Topomatic.Sites.Core;
using Topomatic.Sites.SiteObjects;
using Topomatic.Smt;

namespace PaveDetail.Cad
{
    // Единственный класс модуля, который знает про Topomatic.Sites. Ядро (Core/) от него не зависит.
    //
    // Живой факт с проекта юзера (2026-08-27): состав слоёв дорожной одежды хранится СТРОКОЙ формата
    // Topomatic.Sites.MaterialsLayers ("{толщина}|{id};..."), но в ТРЁХ разных местах в зависимости
    // от того, каким инструментом Robur пользовался проектировщик:
    //   - модификатор "Слои ДО" пишет строку в семантику ОДНОГО из слоёв связанного SitePavement
    //     (тег/имя нестабильны между версиями - у региона тег "MaterialsLayers"/имя "Контроллер КДО",
    //     у слоя это был просто числовой ключ 15 без читаемого имени), Id там - код классификатора
    //     ("SmdxGenplanSmallAsphalt");
    //   - поле "Контроллер КДО" в семантике самого региона пишет ту же строку, но Id там уже готовое
    //     русское имя материала, без кода;
    //   - семантика ПАТЧА поверхности - именно оттуда "Контроллер КДО" читает сам Robur, когда
    //     формирует 3D тела и нижнюю поверхность (Topomatic.Sites.Controller, cmd create_bottom_surface:
    //     surface.Patchs[handle].Semantic.TryGetValue("MaterialsLayers", ...)).
    // SiteReader ищет строку по СОДЕРЖИМОМУ (см. MaterialsLayersScan), а не по конкретному тегу -
    // это переживёт расхождения между версиями/продуктами Robur. SitePavement проверяется первым:
    // он привязан к патчу поверхности и обычно означает реально построенную конструкцию.
    internal sealed class SiteReader
    {
        private readonly Site _site;
        private readonly Surface _surface;
        private readonly SmdxClassifier _materials;

        public SiteReader(SiteModel model, SmdxClassifier materials)
        {
            _site = model == null ? null : model.Site;
            _surface = model == null ? null : model.Surface;
            _materials = materials ?? SmdxClassifier.Empty();
        }

        public static SiteModel FindSiteModel()
        {
            SiteModel found = null;
            try
            {
                PluginCoreOps.FilterOpenedModels((Predicate<IProjectModel>)delegate (IProjectModel pm)
                {
                    if (found != null) return false;
                    try
                    {
                        var sm = pm == null ? null : pm.LockRead() as SiteModel;
                        if (sm != null && sm.Site != null) found = sm;
                    }
                    catch { }
                    return false;
                });
            }
            catch { }
            return found;
        }

        // Путь к файлу проекта знает IProjectModel (pm), а не SiteModel (pm.LockRead()) -
        // эта перегрузка запоминает владельца площадки для Cad/ProjectPaths.
        public static SiteModel FindSiteModel(out IProjectModel projectModel)
        {
            SiteModel found = null;
            IProjectModel owner = null;
            try
            {
                PluginCoreOps.FilterOpenedModels((Predicate<IProjectModel>)delegate (IProjectModel pm)
                {
                    if (found != null) return false;
                    try
                    {
                        var sm = pm == null ? null : pm.LockRead() as SiteModel;
                        if (sm != null && sm.Site != null) { found = sm; owner = pm; }
                    }
                    catch { }
                    return false;
                });
            }
            catch { }
            projectModel = owner;
            return found;
        }

        public static SmdxClassifier LoadMaterialsClassifier()
        {
            var path = SmdxClassifierLocator.FindMaterialsSmdxPath();
            if (path == null) return SmdxClassifier.Empty();
            try { return SmdxClassifier.Parse(File.ReadAllText(path)); }
            catch { return SmdxClassifier.Empty(); }
        }

        public IList<SiteRegion> Regions()
        {
            var list = new List<SiteRegion>();
            var all = _site.Collection;
            for (int i = 0; i < all.Count; i++)
            {
                var r = all[i] as SiteRegion;
                if (r != null) list.Add(r);
            }
            return list;
        }

        public string RegionName(SiteRegion region)
        {
            var layer = region.PatchLayer;
            return layer != null && !string.IsNullOrWhiteSpace(layer.Name) ? layer.Name : "Регион";
        }

        public NodeInput ReadNode(SiteRegion region)
        {
            var input = new NodeInput
            {
                RegionName = RegionName(region),
                Kind = NodeKind.Simple
            };

            var raw = FindMaterialsLayersString(region);
            if (!string.IsNullOrEmpty(raw))
            {
                var layers = MaterialsLayers.CreateFromString(raw);
                foreach (var l in layers)
                {
                    input.Layers.Add(new LayerInput
                    {
                        Material = _materials.Resolve(l.Id),
                        ThicknessM = l.Thickness
                    });
                }
            }

            var border = FindBorder(region);
            if (border != null)
            {
                input.Kind = NodeKind.WithBorder;
                input.Border = new BorderInput
                {
                    Name = "борт из модели",
                    WidthM = border.Width,
                    HeightM = border.Rise + Math.Max(border.OutterRise, 0.15),
                    RiseM = border.Rise,
                    BeddingMaterial = "Бетон класса В15",
                    BeddingThicknessM = 0.10,
                    BeddingOverhangM = 0.05,
                    FromModel = true
                };
            }

            return input;
        }

        private string FindMaterialsLayersString(SiteRegion region)
        {
            var pavement = FindPavement(region);
            if (pavement != null)
            {
                for (int i = 0; i < pavement.Layers.Count; i++)
                {
                    var found = FindInSemantic(pavement.Layers[i].Semantic);
                    if (found != null) return found;
                }
            }

            var inRegion = FindInSemantic(region.Semantic);
            if (inRegion != null) return inRegion;

            return FindInSemantic(PatchSemantic(region));
        }

        // Патч поверхности, на котором стоит регион - третий источник состава (см. комментарий к классу).
        private SemanticDataSet PatchSemantic(SiteRegion region)
        {
            if (_site == null || _surface == null || string.IsNullOrEmpty(region.Patch)) return null;
            try
            {
                var patch = SiteTools.GetPatch(_site, _surface, region.Patch);
                return patch == null ? null : patch.Semantic;
            }
            catch { return null; }
        }

        private static string FindInSemantic(SemanticDataSet set)
        {
            if (set == null) return null;

            // Прямой доступ по тегу: значение приходит из схемы семантики, даже если объект его
            // не переопределял (SemanticDataSet.TryGetValue резолвит handle через SemanticRootNode).
            try
            {
                object direct;
                if (set.TryGetValue(MaterialsLayersScan.Tag, out direct) &&
                    MaterialsLayersScan.IsMaterialsLayers(direct as string))
                    return (string)direct;
            }
            catch { }

            // Поиск по содержимому берёт tv.value, а НЕ tv.svalue: узлу «Контроллер КДО» схема
            // назначает конвертер ConstructionControllerConverter, чей ConvertToString ВСЕГДА
            // возвращает string.Empty, поэтому svalue этого поля пуст по построению - именно на
            // этом ломался прежний поиск (регион с заданным контроллером читался как пустой).
            foreach (var tv in set.TaggedValues)
            {
                var asValue = tv.value as string;
                if (MaterialsLayersScan.IsMaterialsLayers(asValue)) return asValue;
                if (MaterialsLayersScan.IsMaterialsLayers(tv.svalue)) return tv.svalue;
            }
            return null;
        }

        private SitePavement FindPavement(SiteRegion region)
        {
            if (string.IsNullOrEmpty(region.Patch)) return null;
            var all = _site.Collection;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i] as SitePavement;
                if (p != null && string.Equals(p.BasePatch, region.Patch, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        // ЖДЁТ ДАМПА: у региона BaseLine пока пустой во всех живых прогонах (юзер не задавал
        // борт), совпадение по BaseLine не проверено. Пустая строка ни с чем не сопоставляется.
        private SiteBorder FindBorder(SiteRegion region)
        {
            if (string.IsNullOrEmpty(region.BaseLine)) return null;
            var all = _site.Collection;
            for (int i = 0; i < all.Count; i++)
            {
                var b = all[i] as SiteBorder;
                if (b != null && string.Equals(b.BaseLine, region.BaseLine, StringComparison.OrdinalIgnoreCase))
                    return b;
            }
            return null;
        }
    }
}
