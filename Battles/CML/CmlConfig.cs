using Newtonsoft.Json;

namespace CML
{
    public class CmlConfig
    {
        [JsonProperty("port")] public int Port { get; set; } = 8080;
        [JsonProperty("token")] public string DiscordToken { get; set; }
        [JsonProperty("wwwroot")] public string SiteRoot { get; set; } = "./wwwroot/";
        [JsonProperty("prefix")] public string DiscordPrefix { get; set; } = "!";
        [JsonProperty("server")] public ulong Guild { get; set; }
        [JsonProperty("battles")] public ulong BattlesChannel { get; set; }
        [JsonProperty("applications")] public ulong ApplicationsChannel { get; set; }
        [JsonProperty("announcements")] public ulong AnnouncementsChannel { get; set; }
        [JsonProperty("votes")] public ulong VotesChannel { get; set; }
        [JsonProperty("competitor")] public ulong CompetitorRole { get; set; }
        [JsonProperty("attack")] public ulong AttackEmote { get; set; }
        [JsonProperty("defense")] public ulong DefenseEmote { get; set; }
        [JsonProperty("speed")] public ulong SpeedEmote { get; set; }
    }
}
