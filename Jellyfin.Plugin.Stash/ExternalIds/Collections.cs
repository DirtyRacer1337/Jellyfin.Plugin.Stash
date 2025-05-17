using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
using MediaBrowser.Controller.Entities;
#else
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Providers;
#endif

namespace Stash.ExternalIds
{
    public class Collections : IExternalId
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name;
#else
        public string ProviderName => Plugin.Instance.Name;

        public ExternalIdMediaType? Type => ExternalIdMediaType.BoxSet;
#endif

        public string Key => Plugin.Instance.Name;

        public string UrlFormatString => Plugin.Instance.Configuration.StashEndpoint + "/studios/{0}";

        public bool Supports(IHasProviderIds item) => item is BoxSet;
    }
}
