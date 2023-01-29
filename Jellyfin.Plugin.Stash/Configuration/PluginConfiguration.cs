using MediaBrowser.Model.Plugins;

namespace Stash.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            this.StashEndpoint = "http://localhost:9999";
            this.StashAPIKey = string.Empty;
        }

        public string StashEndpoint { get; set; }

        public string StashAPIKey { get; set; }
    }
}
