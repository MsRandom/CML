using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CML.Bot
{
    public sealed class DiscordBotListener : CmlListener
    {
        private readonly string _token;
        private DiscordSocketClient _client;

        public DiscordBotListener(string token)
        {
            _token = token;
        }
        
        public override async void Listen()
        {
            _client = new DiscordSocketClient();
            //logs are annoying
            //_client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
            Console.WriteLine("Discord client started.");
        }
        
        public override async void Close()
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
            _client = null;
            Console.WriteLine("Discord client stopped.");
        }

        /*private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }*/
        
        private Task ReadyAsync()
        {
            Console.WriteLine($"Ready and logged in as {_client.CurrentUser}");
            return Task.CompletedTask;
        }
        
        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == _client.CurrentUser.Id)
                return;

            if (message.Channel.Id.Equals(654157597389225985) && message.Content.ToLower() == "!ping")
                await message.Channel.SendMessageAsync("Pong!");
        }
    }
}
