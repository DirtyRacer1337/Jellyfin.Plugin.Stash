using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Stash.Helpers;

#if __EMBY__
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Configuration;
#else
using System.Net.Http;
using MediaBrowser.Controller.Entities.Movies;
#endif

namespace Stash.Providers
{
    public class CollectionsImages : IRemoteImageProvider
    {
        public string Name => Plugin.Instance.Name;

        public bool Supports(BaseItem item) => item is BoxSet;

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
            => new List<ImageType>
            {
                ImageType.Logo,
            };

#if __EMBY__
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
#else
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
#endif
        {
            IEnumerable<RemoteImageInfo> images = new List<RemoteImageInfo>();

            if (item == null || !item.ProviderIds.TryGetValue(Plugin.Instance.Name, out var curID))
            {
                return images;
            }

            try
            {
                images = await StashAPI.StudioImages(curID, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"GetImages error: \"{e}\"");
            }

            if (images.Any())
            {
                foreach (var image in images)
                {
                    image.ProviderName = Plugin.Instance.Name;
                }
            }

            return images;
        }

#if __EMBY__
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
#else
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
#endif
        {
            return UGetImageResponse.SendAsync(url, cancellationToken);
        }
    }
}
