using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Stash.Configuration;
using Stash.Helpers;

#if __EMBY__
using MediaBrowser.Common.Net;
#else
using System.Net.Http;
#endif

namespace Stash.Providers
{
    public class Peoples : IRemoteMetadataProvider<Person, PersonLookupInfo>
    {
        public string Name => Plugin.Instance.Name;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            if (searchInfo == null)
            {
                return result;
            }

            try
            {
                result = await StashAPI.PerformersSearch(searchInfo, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Actor Search error: \"{e}\"");
            }

            return result;
        }

        public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Person>()
            {
                HasMetadata = false,
                Item = new Person(),
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
                result = await StashAPI.PerformerUpdate(curID, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Actor Update error: \"{e}\"");
            }

            if (result.HasMetadata)
            {
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
