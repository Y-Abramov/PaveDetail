using PaveDetail.Ui;
using Topomatic.ApplicationPlatform.Plugins;

namespace PaveDetail
{
    public partial class PaveDetailPlugin
    {
        [cmd("pavedetail_catalog")]
        private void OpenCatalog()
        {
            using (var dlg = new CatalogDialog())
            {
                dlg.ShowDialog();
            }
        }
    }
}
