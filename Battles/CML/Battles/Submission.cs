using Newtonsoft.Json;

namespace CML.Battles
{
    public class Submission : PartialSubmission
    {
        [JsonProperty("attack")]
        public int Attack { get; set; }
        
        [JsonProperty("defense")]
        public int Defense { get; set; }
        
        [JsonProperty("speed")]
        public int Speed { get; set; }
        
        [JsonProperty("model")] public string ModelLocation { get; set; }

        [JsonProperty("texture")] public string TextureLocation { get; set; }
    }
}
