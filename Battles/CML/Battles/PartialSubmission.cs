using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CML.Battles
{
    public class PartialSubmission
    {
        [JsonProperty("owner")]
        public ulong Owner { get; set; }

        [JsonProperty("name")] public string Name { get; set; }
        
        [JsonProperty("element")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Element Element { get; set; }
        
        [JsonProperty("pic")] public string PictureLocation { get; set; }
        
        [JsonProperty("winner")] public bool IsWinner { get; set; }
    }
}
