using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CML.Battles;
using CML.Bot;
using CML.Site;

namespace CML
{
    internal static class Program
    {
        public static readonly Dictionary<string, string> ParsedArgs = new Dictionary<string, string>();
        public static readonly MatchManager Matches = new MatchManager();
        private static CmlListener _listener;
        private static CmlListener _discord;
        
        private static async Task Main(string[] args)
        {
            foreach (var s in args) ParsedArgs[s.Substring(0, s.IndexOf('='))] = s.Substring(s.IndexOf('=') + 1);
            var port = "8080";
            if (ParsedArgs.ContainsKey("port")) port = ParsedArgs["port"];
            if(ParsedArgs.ContainsKey("wwwroot")) _listener = new SiteHostListener($"http://localhost:{port}/");
            if(ParsedArgs.ContainsKey("token")) _discord = new DiscordBotListener(ParsedArgs["token"]);
            _listener?.Listen();
            _discord?.Listen();
            await Task.Delay(-1);
        }
    }
}
