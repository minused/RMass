using Newtonsoft.Json;

namespace RMass.Models
{
    internal class ProductVersionModel
    {
        [JsonProperty("productVersion")]
        public string ProductVersion { get; set; }
    }
}