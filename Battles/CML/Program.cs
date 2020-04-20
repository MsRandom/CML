using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CML.Battles;
using CML.Bot;
using CML.Site;
using Newtonsoft.Json;

namespace CML
{
    internal static class Program
    {
        public static readonly List<CmlListener> ActiveListeners = new List<CmlListener>();
        public static readonly MatchManager Matches = new MatchManager();
        public static CmlConfig Config = new CmlConfig();
        public static SiteHostListener Listener;
        public static DiscordBotListener Discord;
        
        private static async Task Main(string[] args)
        {
            string config;
            if (args.Length > 0) 
                config = args[0];
            else
                config = nameof(config) + ".json";
            Config = JsonConvert.DeserializeObject<CmlConfig>(new FileInfo(config).OpenText().ReadToEnd());
            
            Startup.Run();
            ActiveListeners.ForEach(l => l.Listen());
            await Task.Delay(-1);
        }
    }
}
