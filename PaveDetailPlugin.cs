using System;
using Topomatic.ApplicationPlatform.Plugins;
using PaveDetail.Ui;

namespace PaveDetail
{
    public partial class PaveDetailPlugin : PluginInitializator
    {
        public override void Initialize(PluginFactory factory)
        {
            base.Initialize(factory);
            try { Abr.Bootstrap.AbrBootstrap.Attach("Детали покрытий", null, null); }
            catch { }
        }

        [cmd("about_pavedetail")]
        private void About()
        {
            using (var dlg = new AboutDialog())
            {
                dlg.ShowDialog();
            }
        }
    }
}
