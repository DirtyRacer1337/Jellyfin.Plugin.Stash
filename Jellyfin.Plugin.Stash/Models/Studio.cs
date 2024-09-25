using Newtonsoft.Json;

namespace Stash.Models
{
    public struct Studio
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "parent_studio")]
        public ParentStudio? ParentStudio { get; set; }

        [JsonProperty(PropertyName = "image_path")]
        public string ImagePath { get; set; }
    }
}
