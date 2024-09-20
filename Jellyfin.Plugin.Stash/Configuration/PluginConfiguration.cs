#if __EMBY__
using Emby.Web.GenericEdit;
#else
using MediaBrowser.Model.Plugins;
#endif

namespace Stash.Configuration
{
#if __EMBY__
    public class PluginConfiguration : EditableOptionsBase
    {
        public override string EditorTitle => Plugin.Instance.Name;

#else
    public class PluginConfiguration : BasePluginConfiguration
    {
#endif
        public PluginConfiguration()
        {
            this.StashEndpoint = "http://localhost:9999";
            this.StashAPIKey = string.Empty;

            this.AddDisambiguation = false;
        }

        public string StashEndpoint { get; set; }

        public string StashAPIKey { get; set; }

        public bool AddDisambiguation { get; set; }
    }
}
