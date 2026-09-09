using System.Collections.Generic;
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
        [cmd("pavedetail_batch")]
        private void BuildBatch()
        {
            var cadView = CadView;

            var site = SiteReader.FindSiteModel();
            if (site == null)
            {
                MessageBox.Show("В проекте не найдена площадка.", "Детали покрытий");
                return;
            }

            var reader = new SiteReader(site, SiteReader.LoadMaterialsClassifier());
            var nodes = new List<NodeInput>();
            foreach (var region in reader.Regions())
                nodes.Add(reader.ReadNode(region));

            var grouped = BatchGrouping.Group(nodes);
            if (grouped.Groups.Count == 0)
            {
                MessageBox.Show("Ни у одного региона не задана конструкция дорожной одежды.", "Детали покрытий");
                return;
            }

            using (var dlg = new BatchDialog(grouped))
            {
                if (dlg.ShowDialog() != DialogResult.OK || dlg.Selected.Count == 0) return;

                var target = DrawTarget.Resolve(cadView);
                if (target == null)
                {
                    MessageBox.Show(DrawTarget.NoTargetMessage, "Детали покрытий");
                    return;
                }

                string prompt = "Укажите точку вставки первого узла"
                    + (target.IsSheet ? " - лист «Чертёж»" : " - план площадки");
                Vector2D insert;
                if (!CadPick.TryPoint(cadView, prompt, out insert)) return;

                var options = new NodeOptions().Scaled(target.UnitScale);
                // Шаг между узлами задан в диалоге в единицах плана - на листе он тоже
                // должен вырасти, иначе узлы наедут друг на друга.
                double step = dlg.StepM * target.UnitScale;
                var writer = new DrawingWriter(target.Drawing);
                int built = 0;

                for (int i = 0; i < dlg.Selected.Count; i++)
                {
                    var shapes = new NodeBuilder(new MaterialResolver(CatalogStore.Materials()), options)
                        .Build(dlg.Selected[i].Sample);
                    writer.Write(shapes, new Vector2D(insert.X + step * i, insert.Y), target.UnitScale);
                    built++;
                }

                cadView.Unlock();
                cadView.Invalidate();

                var message = "Построено узлов: " + built + " из " + grouped.Groups.Count + ".";
                if (grouped.Skipped.Count > 0)
                    message += "\r\nПропущены без конструкции: " + string.Join(", ", grouped.Skipped.ToArray());
                MessageBox.Show(message, "Детали покрытий");
            }
        }
    }
}
