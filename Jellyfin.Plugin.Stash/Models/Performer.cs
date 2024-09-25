using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Stash.Models
{
    public struct Performer
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "disambiguation")]
        public string Disambiguation { get; set; }

        [JsonProperty(PropertyName = "image_path")]
        public string ImagePath { get; set; }

        [JsonProperty(PropertyName = "alias_list")]
        public List<string> AliasList { get; set; }

        [JsonProperty(PropertyName = "birthdate")]
        public DateTime? BirthDate { get; set; }

        [JsonProperty(PropertyName = "death_date")]
        public DateTime? DeathDate { get; set; }
    }
}
