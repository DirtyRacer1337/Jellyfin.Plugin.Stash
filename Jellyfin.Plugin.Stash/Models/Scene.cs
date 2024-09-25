using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Stash.Models
{
    public struct Scene
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "details")]
        public string Details { get; set; }

        [JsonProperty(PropertyName = "date")]
        public DateTime? Date { get; set; }

        [JsonProperty(PropertyName = "paths")]
        public Paths Paths { get; set; }

        [JsonProperty(PropertyName = "studio")]
        public Studio? Studio { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public List<Tags> Tags { get; set; }

        [JsonProperty(PropertyName = "performers")]
        public List<Performer> Performers { get; set; }
    }
}
