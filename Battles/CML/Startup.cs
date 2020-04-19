using System;
using System.Threading.Tasks;
using CML.Bot;
using CML.Site;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace CML
{
    public class Startup
    {
        private static DiscordSocketClient _client;
        
        public static void Run()
        {
            var startup = new Startup();
            Task.Run(() =>
            {
                while (true)
                {
                    var line = Console.ReadLine();
                    if (string.IsNullOrEmpty(line) || line != "stop") continue;
                    Program.ActiveListeners.ForEach(l => l.Close());
                    break;
                }
            });
            startup.Setup();
        }

        private void Setup()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            services.BuildServiceProvider().GetRequiredService<CommandHandler>();
            Program.Listener = new SiteHostListener(Program.Config.Port);
            Program.Discord = new DiscordBotListener(Program.Config.DiscordToken, _client);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_client = new DiscordSocketClient())
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                DefaultRunMode = RunMode.Async,
            }))
            .AddSingleton<CommandHandler>()
            .AddSingleton<Random>();
        }
    }
}
