using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
#else
using MediaBrowser.Model.Providers;
#endif

namespace Stash
{
    public class Peoples : IExternalId
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name;
#else
        public string ProviderName => Plugin.Instance.Name;

        public ExternalIdMediaType? Type => ExternalIdMediaType.Person;
#endif

        public string Key => Plugin.Instance.Name;

        public string UrlFormatString => Plugin.Instance.Configuration.StashEndpoint + "/performers/{0}";

        public bool Supports(IHasProviderIds item) => item is Person;
    }
}
