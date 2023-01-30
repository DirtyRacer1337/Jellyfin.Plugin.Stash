using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Stash.Helpers;

#if __EMBY__
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
#else
using System.Net.Http;
using MediaBrowser.Controller.Entities.Movies;
#endif

namespace Stash.Providers
{
#if __EMBY__
    public class Collections : IRemoteSearchProvider<BoxSetInfo>
#else
    public class Collections : IRemoteMetadataProvider<BoxSet, BoxSetInfo>
#endif
    {
        public string Name => Plugin.Instance.Name;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BoxSetInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            if (searchInfo == null)
            {
                return result;
            }

            try
            {
                result = await StashAPI.StudiosSearch(searchInfo, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Studios Search error: \"{e}\"");
            }

            return result;
        }

        public async Task<MetadataResult<BoxSet>> GetMetadata(BoxSetInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BoxSet>()
            {
                HasMetadata = false,
                Item = new BoxSet(),
            };

            if (info == null)
            {
                return result;
            }

            info.ProviderIds.TryGetValue(Plugin.Instance.Name, out var curID);

            if (string.IsNullOrEmpty(curID))
            {
                var searchResults = await this.GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                if (searchResults.Any())
                {
                    searchResults.First().ProviderIds.TryGetValue(Plugin.Instance.Name, out curID);
                }
            }

            if (string.IsNullOrEmpty(curID))
            {
                return result;
            }

            try
            {
                result = await StashAPI.StudioUpdate(curID, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Studio Update error: \"{e}\"");
            }

            return result;
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
