using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CML.Battles
{
    public class Application
    {
        [JsonProperty("user")]
        public ulong User { get; set; }

        [JsonProperty("password")] public string Password { get; set; }
        
        [JsonProperty("element")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Element? Element { get; set; }
    }
}
