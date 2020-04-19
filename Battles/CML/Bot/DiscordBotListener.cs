using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CML.Bot
{
    public sealed class DiscordBotListener : CmlListener
    {
        private readonly string _token;
        public DiscordSocketClient Client;

        public DiscordBotListener(string token, DiscordSocketClient client)
        {
            _token = token;
            Client = client;
        }
        
        public override async void Listen()
        {
            //logs are annoying
            //_client.Log += LogAsync;
            if (Client != null)
            {
                Client.Ready += ReadyAsync;
                await Client.LoginAsync(TokenType.Bot, _token);
                await Client.StartAsync();
            }

            Console.WriteLine("Discord client started.");
        }
        
        public override async void Close()
        {
            if (Client != null)
            {
                await Client.LogoutAsync();
                await Client.StopAsync();
            }

            Client = null;
            Console.WriteLine("Discord client stopped.");
        }

        /*private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }*/

        private Task ReadyAsync()
        {
            if (Client == null) return Task.FromException(new Exception("Discord client is null."));
            Console.WriteLine($"Ready and logged in as {Client.CurrentUser}");
            return Task.CompletedTask;
        }
    }
}
