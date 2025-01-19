using Newtonsoft.Json;

namespace Stash.Models
{
    public struct ParentStudio
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "image_path")]
        public string ImagePath { get; set; }
    }
}
