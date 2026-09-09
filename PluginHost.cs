using System;
using Topomatic.ApplicationPlatform.Plugins;

namespace PaveDetail
{
    public class PluginHost : PluginHostInitializator
    {
        protected override Type[] GetTypes()
        {
            return new[] { typeof(PaveDetailPlugin) };
        }

        public override void Initialize(PluginFactory factory)
        {
            // ТОЛЬКО base.Initialize - вызов ((PluginHostInitializator)this).Initialize
            // даёт StackOverflow и краш Robur.
            base.Initialize(factory);
        }
    }
}
