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
#else
    public class PluginConfiguration : BasePluginConfiguration
    {
#endif
        public PluginConfiguration()
        {
            this.StashEndpoint = "http://localhost:9999";
            this.StashAPIKey = string.Empty;

            this.UseFilePath = true;
            this.UseFullPathToSearch = true;

            this.AddDisambiguation = false;
            this.UseTags = false;
        }

#if __EMBY__
        public override string EditorTitle => Plugin.Instance.Name;
#endif

        public string StashEndpoint { get; set; }

        public string StashAPIKey { get; set; }

        public bool UseFilePath { get; set; }

        public bool UseFullPathToSearch { get; set; }

        public bool AddDisambiguation { get; set; }

        public bool UseTags { get; set; }
    }
}
