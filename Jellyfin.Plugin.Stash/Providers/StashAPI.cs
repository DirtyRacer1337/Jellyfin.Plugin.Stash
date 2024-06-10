using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stash.Helpers;
using Stash.Helpers.Utils;
using Stash.Models;

namespace Stash.Providers
{
    public static class StashAPI
    {
        public static async Task<JObject> GetDataFromAPI(string query, CancellationToken cancellationToken)
        {
            JObject json = null;
            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
            };

            if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.StashAPIKey))
            {
                headers.Add("APIKey", Plugin.Instance.Configuration.StashAPIKey);
            }

            var url = Plugin.Instance.Configuration.StashEndpoint + string.Format("/graphql?query={0}", Uri.EscapeDataString(query));

            var http = await HTTP.Request(url, cancellationToken, headers).ConfigureAwait(false);
            try
            {
                json = JObject.Parse(http.Content);
            }
            catch (Exception e)
            {
                Logger.Error($"Error GetDataFromAPI \"${e}\"");
            }
            finally
            {
                if (json != null && json.ContainsKey("errors"))
                {
                    var e = json["errors"][0]["message"];
                    json = null;
                    Logger.Error($"Error GetDataFromAPI \"${e}\"");
                }
            }

            return json;
        }

        public static async Task<List<RemoteSearchResult>> SceneSearch(ItemLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            var query = searchInfo.Name;
#if __EMBY__
            string path = string.Empty;
#else
            var path = searchInfo.Path;
#endif

            if (!string.IsNullOrEmpty(path))
            {
                query = System.IO.Path.GetFileNameWithoutExtension(path);
            }

            if (string.IsNullOrEmpty(query))
            {
                return result;
            }

            query = HttpUtility.JavaScriptStringEncode(query.Trim());

            string searchData;
            if (!string.IsNullOrEmpty(path))
            {
                searchData = string.Format("scene_filter:{{path:{{value:\"\\\"{0}\\\"\",modifier:INCLUDES}}}}", query);
            }
            else
            {
                searchData = string.Format("filter:{{q:\"{0}\"}}", query);
            }

            var data = string.Format(Consts.SceneSearchQuery, searchData);
            var http = await GetDataFromAPI(data, cancellationToken).ConfigureAwait(false);

            if (http == null)
            {
                return result;
            }

            data = http["data"]["findScenes"]["scenes"].ToString();
            var searchResults = JsonConvert.DeserializeObject<List<Scene>>(data);

            if (!string.IsNullOrEmpty(path) && searchResults.Count > 1)
            {
                return result;
            }

            foreach (var searchResult in searchResults)
            {
                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, searchResult.id } },
                    Name = searchResult.title,
                    PremiereDate = searchResult.date,
                    ImageUrl = await ImageHelper.GetImageSizeAndValidate(searchResult.movies.FirstOrDefault()?.movie.front_image_path, cancellationToken) ?? searchResult.paths.screenshot,
                });
            }

            return result;
        }

        public static async Task<MetadataResult<MediaBrowser.Controller.Entities.Movies.Movie>> SceneUpdate(string sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MediaBrowser.Controller.Entities.Movies.Movie>()
            {
                Item = new MediaBrowser.Controller.Entities.Movies.Movie(),
                People = new List<PersonInfo>(),
            };

            var data = string.Format(Consts.SceneQuery, HttpUtility.JavaScriptStringEncode(sceneID));

            var http = await GetDataFromAPI(data, cancellationToken).ConfigureAwait(false);
            if (http == null)
            {
                return result;
            }

            data = http["data"]["findScene"].ToString();
            var sceneData = JsonConvert.DeserializeObject<Scene>(data);

            result.Item.Name = sceneData.title;
            result.Item.Overview = sceneData.details;
            result.Item.PremiereDate = sceneData.date;

            var studioName = sceneData.studio?.name;
            if (studioName != null)
            {
                var parentStudio = sceneData.studio?.parent_studio?.name;
                if (parentStudio != null)
                {
                    result.Item.AddStudio(parentStudio);
                }

                result.Item.AddStudio(studioName);
            }

            foreach (var genreLink in sceneData.tags)
            {
                var genreName = genreLink.name;

                result.Item.AddGenre(genreName);
            }

            var directorName = sceneData.movies.FirstOrDefault()?.movie.director;
            if (!string.IsNullOrEmpty(directorName))
            {
                var director = new PersonInfo
                {
                    Name = directorName,
                    Type = PersonKind.Director,
                };

                result.AddPerson(director);
            }

            foreach (var actorLink in sceneData.performers)
            {
                var actorName = (Plugin.Instance.Configuration.AddDisambiguation && !string.IsNullOrEmpty(actorLink.disambiguation)) ? $"{actorLink.name} ({actorLink.disambiguation})" : actorLink.name;
                var actor = new PersonInfo
                {
                    ProviderIds = { { Plugin.Instance.Name, actorLink.id } },
                    Name = actorName,
                    ImageUrl = actorLink.image_path,
                };

                result.People.Add(actor);
            }

            result.HasMetadata = true;

            return result;
        }

        public static async Task<IEnumerable<RemoteImageInfo>> SceneImages(string sceneID, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();
            if (sceneID == null)
            {
                return result;
            }

            var data = string.Format(Consts.SceneQuery, HttpUtility.JavaScriptStringEncode(sceneID));

            var http = await GetDataFromAPI(data, cancellationToken).ConfigureAwait(false);
            if (http == null)
            {
                return result;
            }

            data = http["data"]["findScene"].ToString();
            var sceneData = JsonConvert.DeserializeObject<Scene>(data);

            result.Add(new RemoteImageInfo
            {
                Type = ImageType.Primary,
                Url = await ImageHelper.GetImageSizeAndValidate(sceneData.movies.FirstOrDefault()?.movie.front_image_path, cancellationToken) ?? sceneData.paths.screenshot,
            });

            return result;
        }

        public static async Task<List<RemoteSearchResult>> PerformersSearch(ItemLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (string.IsNullOrEmpty(searchInfo.Name))
            {
                return result;
            }

            var query = HttpUtility.JavaScriptStringEncode(searchInfo.Name.Trim());
            string searchData = string.Format("filter:{{q:\"{0}\"}}", query);

            var data = string.Format(Consts.PerformerSearchQuery, searchData);
            var http = await GetDataFromAPI(data, cancellationToken).ConfigureAwait(false);

            if (http == null)
            {
                return result;
            }

            data = http["data"]["findPerformers"]["performers"].ToString();
            var searchResults = JsonConvert.DeserializeObject<List<Performer>>(data);

            foreach (var searchResult in searchResults)
            {
                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, searchResult.id } },
                    Name = searchResult.name,
                    PremiereDate = searchResult.birthdate,
                    ImageUrl = searchResult.image_path,
                });
            }

            return result;
        }

        public static async Task<MetadataResult<Person>> PerformerUpdate(string sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Person>()
            {
                Item = new Person(),
            };

            var data = string.Format(Consts.PerformerQuery, HttpUtility.JavaScriptStringEncode(sceneID));

            var http = await GetDataFromAPI(data, cancellationToken).ConfigureAwait(false);
            if (http == null)
            {
                return result;
            }

            data = http["data"]["findPerformer"].ToString();
            var sceneData = JsonConvert.DeserializeObject<Performer>(data);

            result.Item.OriginalTitle = string.Join(", ", sceneData.alias_list);

            result.Item.PremiereDate = sceneData.birthdate;
            result.Item.EndDate = sceneData.death_date;
            if (sceneData.country.Any())
                {
                    var bornPlaceList = new List<string>();
                    var location = new RegionInfo(sceneData.country.Trim()).EnglishName;
                    if (!string.IsNullOrEmpty(location))
                        {
                            bornPlaceList.Add(location);
                            result.Item.ProductionLocations = new string[] { string.Join(", ", bornPlaceList) };
                        }
                }

            result.HasMetadata = true;

            return result;
        }

        public static async Task<IEnumerable<RemoteImageInfo>> PerformerImages(string sceneID, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            var data = string.Format(Consts.PerformerQuery, HttpUtility.JavaScriptStringEncode(sceneID));

            var http = await GetDataFromAPI(data, cancellationToken).ConfigureAwait(false);
            if (http == null)
            {
                return result;
            }

            data = http["data"]["findPerformer"].ToString();
            var sceneData = JsonConvert.DeserializeObject<Performer>(data);

            result.Add(new RemoteImageInfo
            {
                Type = ImageType.Primary,
                Url = sceneData.image_path,
            });

            return result;
        }

        public static async Task<List<RemoteSearchResult>> StudiosSearch(ItemLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (string.IsNullOrEmpty(searchInfo.Name))
            {
                return result;
            }

            var query = HttpUtility.JavaScriptStringEncode(searchInfo.Name.Trim());
            string searchData = string.Format("filter:{{q:\"{0}\"}}", query);

            var data = string.Format(Consts.StudiosSearchQuery, searchData);
            var http = await GetDataFromAPI(data, cancellationToken).ConfigureAwait(false);

            if (http == null)
            {
                return result;
            }

            data = http["data"]["findStudios"]["studios"].ToString();
            var searchResults = JsonConvert.DeserializeObject<List<Models.Studio>>(data);

            foreach (var searchResult in searchResults)
            {
                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, searchResult.id } },
                    Name = searchResult.name,
                    ImageUrl = searchResult.image_path,
                });
            }

            return result;
        }

        public static async Task<MetadataResult<BoxSet>> StudioUpdate(string sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BoxSet>()
            {
                Item = new BoxSet(),
            };

            var data = string.Format(Consts.StudioQuery, HttpUtility.JavaScriptStringEncode(sceneID));

            var http = await GetDataFromAPI(data, cancellationToken).ConfigureAwait(false);
            if (http == null)
            {
                return result;
            }

            /*
            data = http["data"]["findStudio"].ToString();
            var sceneData = JsonConvert.DeserializeObject<Models.Studio>(data);
            */

            result.HasMetadata = true;

            return result;
        }

        public static async Task<IEnumerable<RemoteImageInfo>> StudioImages(string sceneID, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            var data = string.Format(Consts.StudioQuery, HttpUtility.JavaScriptStringEncode(sceneID));

            var http = await GetDataFromAPI(data, cancellationToken).ConfigureAwait(false);
            if (http == null)
            {
                return result;
            }

            data = http["data"]["findStudio"].ToString();
            var sceneData = JsonConvert.DeserializeObject<Models.Studio>(data);

            result.Add(new RemoteImageInfo
            {
                Type = ImageType.Logo,
                Url = sceneData.image_path,
            });

            return result;
        }
    }
}
