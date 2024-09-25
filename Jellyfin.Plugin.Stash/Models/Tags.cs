using Newtonsoft.Json;

namespace Stash.Models
{
    public struct Tags
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
