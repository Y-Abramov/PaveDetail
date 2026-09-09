using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using PaveDetail.Cad;
using PaveDetail.Core;
using Topomatic.ApplicationPlatform.Plugins;

namespace PaveDetail
{
    public partial class PaveDetailPlugin
    {
        // Выгрузка в файл. Чертежа не касается: рисование живёт в pavedetail_table
        // и pavedetail_report, здесь только чтение площадки и запись файла.
        [cmd("pavedetail_export")]
        private void ExportToFile()
        {
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

            using (var save = new SaveFileDialog())
            {
                save.Title = "Выгрузить в файл";
                save.FileName = "Детали покрытий";
                save.Filter =
                    "Детали покрытий - CSV (разделитель ;)|*.csv" +
                    "|Детали покрытий - HTML-страница|*.html" +
                    "|Ведомость конструкций - CSV (разделитель ;)|*.csv" +
                    "|Ведомость конструкций - HTML-страница|*.html";
                save.FilterIndex = 1;
                if (save.ShowDialog() != DialogResult.OK) return;

                var text = Compose(save.FilterIndex, grouped, projectModel);

                try
                {
                    // BOM обязателен: Excel без него читает кириллицу в CSV как мусор.
                    File.WriteAllText(save.FileName, text, new UTF8Encoding(true));
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Не удалось записать файл:\r\n" + ex.Message, "Детали покрытий");
                    return;
                }

                MessageBox.Show("Файл сохранён:\r\n" + save.FileName, "Детали покрытий");
            }
        }

        // FilterIndex у SaveFileDialog считается с единицы и совпадает с порядком
        // фильтров в строке Filter выше.
        private static string Compose(int filterIndex, BatchResult grouped,
            Topomatic.ApplicationPlatform.Core.IProjectModel projectModel)
        {
            if (filterIndex == 3) return ReportExport.ToCsv(ReportBuilder.Build(grouped));
            if (filterIndex == 4) return ReportExport.ToHtml(ReportBuilder.Build(grouped));

            // Таблица берёт те же данные, что и лист: группы площадки + файл рядом с проектом.
            var data = ProjectPaths.Open(projectModel).Read();
            var rows = TableRowFactory.Build(grouped, data, CatalogStore.Materials());
            return filterIndex == 2 ? ReportExport.TableToHtml(rows) : ReportExport.TableToCsv(rows);
        }
    }
}
