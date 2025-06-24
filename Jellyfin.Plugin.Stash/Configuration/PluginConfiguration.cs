#if __EMBY__
using Emby.Web.GenericEdit;
#else
using MediaBrowser.Model.Plugins;
#endif

namespace Stash.Configuration
{
    public enum TagStyle
    {
        Genre = 0,
        Tag = 1,
        Disabled = 2,
    }

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

            this.TagStyle = TagStyle.Genre;
        }

#if __EMBY__
        public override string EditorTitle => Plugin.Instance.Name;
#endif

        public string StashEndpoint { get; set; }

        public string StashAPIKey { get; set; }

        public bool UseFilePath { get; set; }

        public bool UseFullPathToSearch { get; set; }

        public bool AddDisambiguation { get; set; }

        public TagStyle TagStyle { get; set; }
    }
}
