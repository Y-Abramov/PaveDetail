using System.Windows.Forms;
using PaveDetail.Cad;
using PaveDetail.Core;
using PaveDetail.Ui;
using Topomatic.ApplicationPlatform.Plugins;
using Topomatic.Cad.Foundation;

namespace PaveDetail
{
    public partial class PaveDetailPlugin
    {
        [cmd("pavedetail_node")]
        private void BuildNode()
        {
            var cadView = CadView;

            // Регион ищется по клику в координатах Site-модели площадки: если сейчас
            // активен лист «Чертёж», клик придёт в его собственных координатах и
            // NearestRegion молча вернёт первый попавшийся регион без всякой связи
            // с местом клика. Вставка на лист по-прежнему возможна - см. ниже, тут
            // блокируется только бессмысленный первый пик.
            var initialTarget = DrawTarget.Resolve(cadView);
            if (initialTarget != null && initialTarget.IsSheet)
            {
                MessageBox.Show(
                    "Регион выбирается на плане площадки, а не на листе «Чертёж».\r\n" +
                    "Откройте план, выберите регион; для вставки узла на лист переключитесь " +
                    "на него на шаге «Укажите точку вставки узла».",
                    "Детали покрытий");
                return;
            }

            var site = SiteReader.FindSiteModel();
            if (site == null)
            {
                MessageBox.Show("В проекте не найдена площадка.", "Детали покрытий");
                return;
            }

            var reader = new SiteReader(site, SiteReader.LoadMaterialsClassifier());

            Vector2D pick;
            if (!CadPick.TryPoint(cadView, "Укажите точку внутри региона", out pick)) return;

            var region = CadPick.NearestRegion(reader, pick);
            if (region == null)
            {
                MessageBox.Show("Регион по указанной точке не найден.", "Детали покрытий");
                return;
            }

            var input = reader.ReadNode(region);
            if (input.Layers.Count == 0)
            {
                MessageBox.Show(
                    "У региона «" + input.RegionName + "» не задана конструкция дорожной одежды.\r\n" +
                    "Задайте слои модификатором «Слои ДО» или полем «Контроллер КДО» и повторите.",
                    "Детали покрытий");
                return;
            }

            var resolver = new MaterialResolver(CatalogStore.Materials());
            using (var dlg = new NodeDialog(input, resolver, CatalogStore.Borders()))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var target = DrawTarget.Resolve(cadView);
                if (target == null)
                {
                    MessageBox.Show(DrawTarget.NoTargetMessage, "Детали покрытий");
                    return;
                }

                string prompt = "Укажите точку вставки узла"
                    + (target.IsSheet ? " - лист «Чертёж»" : " - план площадки");
                Vector2D insert;
                if (!CadPick.TryPoint(cadView, prompt, out insert)) return;

                var shapes = new NodeBuilder(
                        new MaterialResolver(CatalogStore.Materials()),
                        dlg.Options.Scaled(target.UnitScale))
                    .Build(dlg.Input);

                new DrawingWriter(target.Drawing).Write(shapes, insert, target.UnitScale);

                cadView.Unlock();
                cadView.Invalidate();
            }
        }
    }
}
