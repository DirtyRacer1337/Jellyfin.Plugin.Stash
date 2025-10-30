using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Stash.Configuration;
using Stash.Helpers;

#if __EMBY__
using MediaBrowser.Common.Net;
#else
using System.Net.Http;
using Jellyfin.Data.Enums;
#endif

namespace Stash.Providers
{
#if __EMBY__
    public class Movies : IRemoteMetadataProvider<Movie, MovieInfo>, IHasSupportedExternalIdentifiers
#else
    public class Movies : IRemoteMetadataProvider<Movie, MovieInfo>
#endif
    {
        public string Name => Plugin.Instance.Name;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            if (searchInfo == null)
            {
                return result;
            }

            searchInfo.ProviderIds.TryGetValue(Plugin.Instance.Name, out var curID);
            if (!string.IsNullOrEmpty(curID))
            {
                var sceneData = new MetadataResult<Movie>()
                {
                    HasMetadata = false,
                    Item = new Movie(),
                    People = new List<PersonInfo>(),
                };

                var sceneImages = new List<RemoteImageInfo>();

                try
                {
                    sceneData = await StashAPI.SceneUpdate(curID, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error($"Update error: \"{e}\"");
                }

                try
                {
                    sceneImages = (List<RemoteImageInfo>)await StashAPI.SceneImages(curID, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error($"GetImages error: \"{e}\"");
                }

                if (sceneData.HasMetadata)
                {
                    result.Add(new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneData.Item.Name,
                        ImageUrl = sceneImages?.Where(o => o.Type == ImageType.Primary).FirstOrDefault()?.Url,
                        PremiereDate = sceneData.Item.PremiereDate,
                    });

                    return result;
                }
            }

            try
            {
                result = await StashAPI.SceneSearch(searchInfo, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Search error: \"{e}\"");
            }

            if (result.Count != 0)
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

                var tags = result.Item.Genres;
                switch (Plugin.Instance.Configuration.TagStyle)
                {
                    case TagStyle.Disabled:
                        result.Item.Genres = Array.Empty<string>();
                        result.Item.Tags = Array.Empty<string>();
                        break;
                    case TagStyle.Genre:
                        result.Item.Genres = tags.ToArray();
                        result.Item.Tags = Array.Empty<string>();
                        break;
                    case TagStyle.Tag:
                        result.Item.Genres = Array.Empty<string>();
                        result.Item.Tags = tags.ToArray();
                        break;
                }

                if (result.People.Count != 0)
                {
                    foreach (var actorLink in result.People)
                    {
#if __EMBY__
                        actorLink.Type = PersonType.Actor;
#else
                        actorLink.Type = PersonKind.Actor;
#endif
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

#if __EMBY__
        public string[] GetSupportedExternalIdentifiers()
        {
            return new[] { Plugin.Instance.Name };
        }
#endif
    }
}
