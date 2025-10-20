using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            var path = Plugin.Instance.Configuration.UseFilePath ? searchInfo.Path : string.Empty;

            if (string.IsNullOrEmpty(query))
            {
                return result;
            }

            string searchData;
            if (!string.IsNullOrEmpty(path))
            {
                if (Plugin.Instance.Configuration.UseFullPathToSearch)
                {
                    searchData = string.Format("path:{{value:\"{0}\",modifier:EQUALS}}", HttpUtility.JavaScriptStringEncode(path));
                }
                else
                {
                    searchData = string.Format("path:{{value:\"\\\"{0}\\\"\",modifier:INCLUDES}}", Path.GetFileNameWithoutExtension(path));
                }

                searchData = string.Format("scene_filter:{{{0}}}", searchData);
            }
            else
            {
                searchData = string.Format("filter:{{q:\"{0}\"}}", HttpUtility.JavaScriptStringEncode(query));
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
                    ProviderIds = { { Plugin.Instance.Name, searchResult.Id } },
                    Name = searchResult.Title,
                    PremiereDate = searchResult.Date,
                    ImageUrl = searchResult.Paths.Screenshot,
                });
            }

            return result;
        }

        public static async Task<MetadataResult<Movie>> SceneUpdate(string sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                Item = new Movie(),
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

            result.Item.Name = sceneData.Title;
            result.Item.Overview = sceneData.Details;
            result.Item.PremiereDate = sceneData.Date;

            var studioName = sceneData.Studio?.Name;
            if (!string.IsNullOrEmpty(studioName))
            {
                var parentStudio = sceneData.Studio?.ParentStudio?.Name;
                if (!string.IsNullOrEmpty(parentStudio))
                {
                    result.Item.AddStudio(parentStudio);
                }

                result.Item.AddStudio(studioName);
            }

            foreach (var genreLink in sceneData.Tags)
            {
                var genreName = genreLink.Name;

                result.Item.AddGenre(genreName);
            }

            foreach (var actorLink in sceneData.Performers)
            {
                var actorName = (Plugin.Instance.Configuration.AddDisambiguation && !string.IsNullOrEmpty(actorLink.Disambiguation)) ? $"{actorLink.Name} ({actorLink.Disambiguation})" : actorLink.Name;
                var actor = new PersonInfo
                {
                    ProviderIds = { { Plugin.Instance.Name, actorLink.Id } },
                    Name = actorName,
                    ImageUrl = actorLink.ImagePath,
                };

                result.AddPerson(actor);
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
                Url = sceneData.Paths.Screenshot,
            });

            result.Add(new RemoteImageInfo
            {
                Type = ImageType.Backdrop,
                Url = sceneData.Paths.Screenshot,
            });

            if (sceneData.Studio.HasValue)
            {
                result.Add(new RemoteImageInfo
                {
                    Type = ImageType.Logo,
                    Url = sceneData.Studio.Value.ImagePath,
                });

                if (sceneData.Studio.Value.ParentStudio.HasValue)
                {
                    result.Add(new RemoteImageInfo
                    {
                        Type = ImageType.Logo,
                        Url = sceneData.Studio.Value.ParentStudio.Value.ImagePath,
                    });
                }
            }

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
                    ProviderIds = { { Plugin.Instance.Name, searchResult.Id } },
                    Name = searchResult.Name,
                    PremiereDate = searchResult.BirthDate,
                    ImageUrl = searchResult.ImagePath,
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

            result.Item.OriginalTitle = string.Join(", ", sceneData.AliasList);
            result.Item.Overview = sceneData.Details;
            result.Item.PremiereDate = sceneData.BirthDate;
            result.Item.EndDate = sceneData.DeathDate;

            if (!string.IsNullOrEmpty(sceneData.Country))
            {
                result.Item.ProductionLocations = new string[] { new RegionInfo(sceneData.Country).EnglishName };
            }

            foreach (var genreLink in sceneData.Tags)
            {
                var genreName = genreLink.Name;

                result.Item.AddGenre(genreName);
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
                Url = sceneData.ImagePath,
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
                    ProviderIds = { { Plugin.Instance.Name, searchResult.Id } },
                    Name = searchResult.Name,
                    ImageUrl = searchResult.ImagePath,
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
                Url = sceneData.ImagePath,
            });

            return result;
        }
    }
}
