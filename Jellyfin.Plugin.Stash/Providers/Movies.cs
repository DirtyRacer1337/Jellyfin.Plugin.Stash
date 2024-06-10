using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Stash.Helpers;

#if __EMBY__
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Entities;
#else
using System.Net.Http;
using Jellyfin.Data.Enums;
#endif

namespace Stash.Providers
{
    public class Movies : IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public string Name => Plugin.Instance.Name;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            if (searchInfo == null)
            {
                return result;
            }

            try
            {
                result = await StashAPI.SceneSearch(searchInfo, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Search error: \"{e}\"");
            }

            if (result.Any())
            {
                foreach (var scene in result)
                {
                    if (scene.PremiereDate.HasValue)
                    {
                        scene.ProductionYear = scene.PremiereDate.Value.Year;
                    }
                }
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                HasMetadata = true,
                Item = new Movie(),
                People = new List<PersonInfo>(),
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

            result.HasMetadata = false;
            try
            {
                result = await StashAPI.SceneUpdate(curID, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Update error: \"{e}\"");
            }

            if (result.HasMetadata)
            {
                result.Item.ProviderIds.Add(Plugin.Instance.Name, curID);
                result.Item.OfficialRating = "XXX";

                if (result.Item.PremiereDate.HasValue)
                {
                    result.Item.ProductionYear = result.Item.PremiereDate.Value.Year;
                }

                if (result.People.Any())
                {
                    foreach (var actorLink in result.People)
                    {
                        if (actorLink.Type == null)
                        {
                            actorLink.Type = PersonKind.Actor;
                        }
                    }
                }
            }
            else
            {
                result.HasMetadata = true;
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
