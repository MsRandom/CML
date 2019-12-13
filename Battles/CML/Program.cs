using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CML.Bot;
using CML.Site;

namespace CML
{
    internal static class Program
    {
        private static CmlListener _listener;
        private static CmlListener _discord;
        
        private static async Task Main(string[] args)
        {
            var parsedArgs = new Dictionary<string, string>();
            foreach (var s in args) parsedArgs[s.Substring(0, s.IndexOf('='))] = s.Substring(s.IndexOf('=') + 1);
            var port = "8080";
            if (parsedArgs.ContainsKey("port")) port = parsedArgs["port"];
            if(parsedArgs.ContainsKey("wwwroot")) _listener = new SiteHostListener(new FileInfo(parsedArgs["wwwroot"] + "/index.html"), $"http://localhost:{port}/");
            if(parsedArgs.ContainsKey("token")) _discord = new DiscordBotListener(parsedArgs["token"]);
            _listener?.Listen();
            _discord?.Listen();
            await Task.Delay(-1);
        }
    }
}
