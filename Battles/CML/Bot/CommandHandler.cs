using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CML.Battles;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace CML.Bot
{
    public class CommandHandler
    {
        public static readonly Dictionary<ulong, (ExpectationType, string)> Expectations = new Dictionary<ulong, (ExpectationType, string)>();
        public static readonly Dictionary<ulong, (Guid, string)> Confirmed = new Dictionary<ulong, (Guid, string)>();
        public static readonly Dictionary<ulong, object> PendingData = new Dictionary<ulong, object>();
        public static readonly Dictionary<RestUserMessage, Submission> VoteMessages = new Dictionary<RestUserMessage, Submission>();
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IServiceProvider _provider;

        public CommandHandler(
            DiscordSocketClient discord,
            CommandService commands,
            IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

            _discord.MessageReceived += OnMessageReceivedAsync;
            _commands.AddModuleAsync<BattleCommands>(_provider).GetAwaiter().GetResult();
        }
        
        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg)) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;
            var argPos = 0;
            if (msg.HasStringPrefix(Program.Config.DiscordPrefix, ref argPos) ||
                msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                if (msg.Channel.Id == Program.Config.ApplicationsChannel && Expectations.ContainsKey(msg.Author.Id))
                {
                    var (type, password) = Expectations[msg.Author.Id];
                    var command = msg.Content.Substring(1);
                    if (command.Equals("cancel"))
                    {
                        Expectations.Remove(msg.Author.Id);
                        await msg.Channel.SendMessageAsync("The request has been canceled successfully");
                        return;
                    }
                    
                    if (command.Equals(type.ToString().ToLower()))
                    {
                        switch (type)
                        {
                            case ExpectationType.Apply:
                                var id = Guid.NewGuid();
                                Program.Matches.Applications[id] = new Application
                                {
                                    User = msg.Author.Id,
                                    Password = password
                                };
                                Confirmed[msg.Author.Id] = (id, password);
                                await msg.Channel.SendMessageAsync("Application successful.");
                                if (Program.Matches.Applications.Count >= 8)
                                {
                                    var rnd = new Random();
                                    var elements = new Stack<Element>();
                                    var guild = Program.Discord.Client.GetGuild(Program.Config.Guild);
                                    var role = guild.GetRole(Program.Config.Guild);
                                    foreach (var element in Elements.Values().OrderBy(e => rnd.Next())) elements.Push(element);
                                    foreach (var application in Program.Matches.Applications.Values)
                                    {
                                        await guild.GetUser(application.User).AddRoleAsync(role);
                                        application.Element = elements.Pop();
                                    }
                                    await guild.GetTextChannel(Program.Config.AnnouncementsChannel).SendMessageAsync("Applications are now closed!");
                                }
                                await Program.Matches.UpdateApplications();
                                break;
                            case ExpectationType.Submit:
                                var (guid, submission) = ((Guid, Submission)) PendingData[msg.Author.Id];
                                Program.Matches.Submissions[guid] = submission;
                                Program.Matches.Applications.Remove(guid);
                                Program.Matches.HallOfFame.Add(submission);
                                await Program.Matches.UpdateContestants();
                                await Program.Matches.UpdateHoF();
                                await msg.Channel.SendMessageAsync("Submission successful.");
                                if (Program.Matches.Submissions.Count >= 8)
                                {
                                    var guild = Program.Discord.Client.GetGuild(Program.Config.Guild);
                                    var votes = guild.GetTextChannel(Program.Config.VotesChannel);
                                    if (votes != null)
                                    {
                                        await guild.GetTextChannel(Program.Config.AnnouncementsChannel)
                                            .SendMessageAsync("Votes are now open!");
                                        var check = new Emoji("✅");
                                        var attack = await guild.GetEmoteAsync(Program.Config.AttackEmote);
                                        var defense = await guild.GetEmoteAsync(Program.Config.DefenseEmote);
                                        var speed = await guild.GetEmoteAsync(Program.Config.SpeedEmote);
                                        foreach (var (_, entry) in Program.Matches.Submissions)
                                        {
                                            var emote = guild.Emotes.First(r => r.Name.Equals(entry.Element.ToString().ToLower()));
                                            var message = await votes.SendFileAsync(
                                                Program.Config.SiteRoot + "contestants/" + entry.PictureLocation,
                                                $"{emote} {entry.Name} {emote}");
                                            VoteMessages[message] = entry;
                                            await message.AddReactionAsync(check);
                                            await message.AddReactionAsync(attack);
                                            await message.AddReactionAsync(defense);
                                            await message.AddReactionAsync(speed);
                                        }
                                    }
                                }
                                break;
                            case ExpectationType.Update:
                            {
                                Program.Matches.Applications[(Guid) PendingData[msg.Author.Id]].User = msg.Author.Id;
                                await Program.Matches.UpdateApplications();
                                await msg.Channel.SendMessageAsync("Update successful.");
                                break;
                            }
                        }
                    }
                }
                else
                    await _commands.ExecuteAsync(new SocketCommandContext(_discord, msg), argPos, _provider);
            }
        }
    }
}
