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
        [cmd("pavedetail_table")]
        private void BuildTable()
        {
            var cadView = CadView;

            Topomatic.ApplicationPlatform.Core.IProjectModel projectModel;
            var site = SiteReader.FindSiteModel(out projectModel);
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

            // Путь к файлу проекта знает IProjectModel, а не SiteModel - см. Task 2.
            var file = ProjectPaths.Open(projectModel);
            var data = file.Read();
            var catalog = CatalogStore.Materials();
            var rows = TableRowFactory.Build(grouped, data, catalog);

            var liveKeys = new List<string>();
            foreach (var g in grouped.Groups) liveKeys.Add(g.Key);

            using (var dlg = new TableDialog(rows, data.Orphans(liveKeys), file.HasPath))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var target = DrawTarget.Resolve(cadView);
                if (target == null)
                {
                    MessageBox.Show(DrawTarget.NoTargetMessage, "Детали покрытий");
                    return;
                }

                string prompt = "Укажите точку вставки таблицы"
                    + (target.IsSheet ? " - лист «Чертёж»" : " - план площадки");
                Vector2D insert;
                if (!CadPick.TryPoint(cadView, prompt, out insert)) return;

                var options = new TableOptions().Scaled(target.UnitScale);
                var shapes = new TableBuilder(new MaterialResolver(catalog), options).Build(dlg.Rows);
                new DrawingWriter(target.Drawing).Write(shapes, insert, target.UnitScale);

                cadView.Unlock();
                cadView.Invalidate();

                Save(file, data, dlg.Rows);
            }
        }

        // Введённое в гриде переносится в файл проекта: существующая запись правится,
        // новая добавляется. Тексты слоёв диалог не трогает - они правятся в каталоге.
        private static void Save(ProjectDataFile file, ProjectData data, IList<TableRow> rows)
        {
            if (!file.HasPath) return;

            foreach (var row in rows)
            {
                var record = data.Find(row.Key);
                if (record == null)
                {
                    record = new ProjectConstruction { key = row.Key };
                    data.Constructions.Add(record);
                }
                record.code = row.Code;
                record.section = row.Section;
                record.name = row.Name;
                record.modulus = row.Modulus;
                record.note = row.Note;
            }

            if (!file.Write(data))
                MessageBox.Show("Таблица построена, но сохранить данные рядом с проектом не удалось:\r\n"
                    + file.Path, "Детали покрытий");
        }
    }
}
