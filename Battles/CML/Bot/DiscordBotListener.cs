using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CML.Bot
{
    public sealed class DiscordBotListener : CmlListener
    {
        private readonly DiscordSocketClient _client;
        private readonly string _token;

        public DiscordBotListener(string token)
        {
            _client = new DiscordSocketClient();
            //logs are annoying
            //_client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _token = token;
        }
        
        public override async void Listen()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
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
