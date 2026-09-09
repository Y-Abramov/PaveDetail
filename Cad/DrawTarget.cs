using System;
using Topomatic.ApplicationPlatform.Core;
using Topomatic.ApplicationPlatform.Plugins;
using Topomatic.Cad.View;
using Topomatic.Dtm;
using Topomatic.Dwg;
using Topomatic.Dwg.Layer;

namespace PaveDetail.Cad
{
    // Куда и в каком масштабе рисовать. Команда пишет туда, где стоит пользователь:
    // активен лист «Чертёж» - в лист, активен план площадки - в план.
    //
    // Тип «Чертёж» в Robur - Topomatic.Dtm.DrawingModel; у него есть свойство Drawing
    // того же типа Topomatic.Dwg.Drawing, в который уже пишет DrawingWriter, поэтому
    // отдельный писатель не нужен - нужен только правильный Drawing и множитель единиц.
    //
    // Принадлежность определяется сравнением ССЫЛОК (ReferenceEquals), а не разбором
    // Drawing.Owner: Owner объявлен как object, его семантика не документирована и живьём
    // не проверена, а ссылочное равенство однозначно.
    internal sealed class DrawTarget
    {
        // Высота текста на плане - метры (умолчание TableOptions/NodeOptions),
        // на листе чертежа - миллиметры по ГОСТ 2.304.
        public const double PlanTextHeightM = 0.08;
        public const double SheetTextHeightMm = 2.5;

        public const string NoTargetMessage =
            "Нет активного окна чертежа.\r\n" +
            "Откройте план площадки или лист «Чертёж» и повторите команду.";

        public Drawing Drawing;
        public double UnitScale;
        public bool IsSheet;

        public static DrawTarget Resolve(CadView cadView)
        {
            var layer = DrawingLayer.GetDrawingLayer(cadView) as DrawingLayer;
            var drawing = layer == null ? null : layer.Drawing;
            if (drawing == null) return null;

            bool sheet = BelongsToSheet(drawing);
            return new DrawTarget
            {
                Drawing = drawing,
                IsSheet = sheet,
                UnitScale = sheet ? SheetTextHeightMm / PlanTextHeightM : 1.0
            };
        }

        private static bool BelongsToSheet(Drawing drawing)
        {
            bool found = false;
            try
            {
                PluginCoreOps.FilterOpenedModels((Predicate<IProjectModel>)delegate (IProjectModel pm)
                {
                    if (found) return false;
                    try
                    {
                        var dm = pm == null ? null : pm.LockRead() as DrawingModel;
                        if (dm != null && ReferenceEquals(dm.Drawing, drawing)) found = true;
                    }
                    catch { }
                    return false;
                });
            }
            catch { }
            return found;
        }
    }
}
