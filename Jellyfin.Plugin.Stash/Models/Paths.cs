using Newtonsoft.Json;

namespace Stash.Models
{
    public struct Paths
    {
        [JsonProperty(PropertyName = "screenshot")]
        public string Screenshot { get; set; }
    }
}
