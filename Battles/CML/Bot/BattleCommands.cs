using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CML.Battles;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace CML.Bot
{
    [Name("Battles")]
    [Summary("Battle Commands")]
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class BattleCommands : ModuleBase<SocketCommandContext>
    {
        private static readonly HashSet<Guid> AutoContestants = new HashSet<Guid>();
        private static bool BattleInProgress => _next != null;
        private static BattleContestant _leftContestant;
        private static BattleContestant _rightContestant;
        private static BattleContestant _next;
        private static BattleContestant Other => _next == _leftContestant ? _rightContestant : _leftContestant;
        private static int _bonusTurns;
        private static (Guid, Guid) _currentWinners;

        [Command("setauto")]
        [Summary("Sets the provided ID to attack automatically")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetAuto(string id)
        {
            var guid = Guid.Parse(id);
            if (Program.Matches.Submissions.ContainsKey(guid))
            {
                AutoContestants.Add(guid);
                await Context.Channel.SendMessageAsync(
                    $"{Program.Matches.Submissions[guid].Name} will now attack automatically.");
            }
            else
            {
                await Context.Channel.SendMessageAsync("No one found with a matching ID.");
            }
        }

        [Command("battle")]
        [Summary("Start the next battle")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Battle()
        {
            var guild = Context.Guild;
            var channel = Context.Channel;

            if (BattleInProgress)
            {
                await channel.SendMessageAsync("A battle is already in progress.");
                return;
            }

            if (!Program.Matches.InBattle)
            {
                var votes = guild.GetTextChannel(Program.Config.VotesChannel);
                if (votes != null)
                {
                    var check = new Emoji("✅");
                    var attack = await guild.GetEmoteAsync(Program.Config.AttackEmote);
                    var defense = await guild.GetEmoteAsync(Program.Config.DefenseEmote);
                    var speed = await guild.GetEmoteAsync(Program.Config.SpeedEmote);
                    var stats = new[] {attack, defense, speed};
                    var dict = new Dictionary<RestUserMessage, (int, IEmote)>();
                    var users = new Dictionary<IUser, RestUserMessage>();
                    var emoteToProperty = new Dictionary<IEmote, PropertyInfo>
                    {
                        {attack, typeof(Submission).GetProperty(nameof(Submission.Attack))},
                        {defense, typeof(Submission).GetProperty(nameof(Submission.Defense))},
                        {speed, typeof(Submission).GetProperty(nameof(Submission.Speed))}
                    };

                    foreach (var msg in CommandHandler.VoteMessages.Keys)
                    {
                        var v = new List<(IEmote, int)>();
                        foreach (var emote in stats)
                        {
                            var s = await msg.GetReactionUsersAsync(emote, 100).FlattenAsync();
                            v.Add((emote, s.Count()));
                        }

                        var checkVotes = await msg.GetReactionUsersAsync(check, 100).FlattenAsync();
                        var count = 0;
                        foreach (var checkVote in checkVotes)
                        {
                            if (users.ContainsKey(checkVote))
                            {
                                var m = users[checkVote];
                                var (size, emote) = dict[m];
                                dict[m] = (size - 1, emote);
                            }
                            else
                            {
                                users[checkVote] = msg;
                                ++count;
                            }
                        }

                        var result = v.Aggregate((a, b) =>
                        {
                            if (a.Item2 > b.Item2) return a;
                            if (b.Item2 > a.Item2) return b;
                            var aInt = (int) (emoteToProperty[a.Item1].GetValue(CommandHandler.VoteMessages[msg]) ?? 0);
                            var bInt = (int) (emoteToProperty[b.Item1].GetValue(CommandHandler.VoteMessages[msg]) ?? 0);
                            if (aInt < bInt) return a;
                            return aInt > bInt ? b : a;
                        });
                        dict[msg] = (count, result.Item1);
                    }

                    var additions = 16;
                    foreach (var (message, (_, type)) in dict.OrderByDescending(pair => pair.Value.Item1))
                    {
                        var property = emoteToProperty[type];
                        var entry = CommandHandler.VoteMessages[message];
                        property.SetValue(entry, (int) (property.GetValue(entry) ?? 0) + additions);
                        additions -= 2;
                    }
                }

                Program.Matches.InBattle = true;
            }

            var (battleLeft, battleRight) = Program.Matches.Battles.First();
            Program.Matches.Battles.RemoveAt(0);

            channel = guild.GetTextChannel(Program.Config.BattlesChannel);

            var submissionLeft = Program.Matches.Submissions[battleLeft];
            var submissionRight = Program.Matches.Submissions[battleRight];
            var userLeft = guild.GetUser(submissionLeft.Owner);
            var userRight = guild.GetUser(submissionRight.Owner);
            _leftContestant = new BattleContestant(battleLeft, submissionLeft, userLeft);
            _rightContestant = new BattleContestant(battleRight, submissionRight, userRight);
            if (AutoContestants.Contains(battleLeft)) _leftContestant.IsAuto = true;
            if (AutoContestants.Contains(battleRight)) _rightContestant.IsAuto = true;
            AutoContestants.Clear();
            
            await channel.SendMessageAsync(
                $"A battle between {userLeft.Mention} with **{submissionLeft.Name}** and {userRight.Mention} with **{submissionRight.Name}** has begun!");
            _next = Test(nameof(submissionLeft.Speed),
                () => Test(nameof(submissionRight.Attack),
                    () => Test(nameof(submissionLeft.Defense), () => _leftContestant)), true);

            BattleContestant Test(string name, Func<BattleContestant> fallback, bool flipped = false)
            {
                var property = typeof(Submission).GetProperty(name);
                if (property == null) return fallback();
                var left = (int) (property.GetValue(submissionLeft) ?? 0);
                var right = (int) (property.GetValue(submissionRight) ?? 0);
                var r = flipped ? _leftContestant : _rightContestant;
                var l = flipped ? _rightContestant : _leftContestant;
                return left > right ? r :
                    left < right ? l : fallback();

            }

            var speedDiff = _next == _leftContestant
                ? submissionLeft.Speed - submissionRight.Speed
                : submissionRight.Speed - submissionLeft.Speed;
            var bonusTurns = 0;
            var i = 0;
            while (true)
            {
                if (speedDiff - (10 + 5 * i) < 0) break;
                speedDiff -= 10 + 5 * i;
                if (++bonusTurns >= 2)
                {
                    bonusTurns = 2;
                    break;
                }

                ++i;
            }

            _bonusTurns = bonusTurns;

            var starter = _next.IsAuto ? _next.Submission.Name : _next.User.Mention;
            await channel.SendMessageAsync(
                $"{starter} has the first turn{(_bonusTurns > 0 ? $" and {_bonusTurns} bonus turns" : "")}.");
            await CheckNext(channel);
        }

        [Command("cancelbattle")]
        [Summary("Cancels current battle")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CancelBattle()
        {
            if (BattleInProgress)
            {
                var channel = Context.Channel.Id == Program.Config.BattlesChannel ? Context.Channel : Context.Guild.GetTextChannel(Program.Config.BattlesChannel);
                EndBattle();
                await channel.SendMessageAsync("Battle is canceled, everyone go home");
            }
            else
            {
                await Context.Channel.SendMessageAsync("What battle?");
            }
        }

        [Command("attack")]
        [Summary("Standard Attack")]
        public async Task Attack()
        {
            var channel = Context.Channel;

            if (channel.Id != Program.Config.BattlesChannel) return;

            if (!BattleInProgress)
            {
                await channel.SendMessageAsync("There isn't a battle in progress.");
                return;
            }

            if (!Context.User.Id.Equals(_next.User.Id))
            {
                var next = _next.IsAuto ? _next.Submission.Name : _next.User.Mention;
                await channel.SendMessageAsync($"It's {next}'s turn!");
            }

            await PerformAttack(Context.Channel);
        }
        
        //Checks if the current turn is auto, to prevent it getting stuck
        private async Task CheckNext(ISocketMessageChannel channel, Func<Task> callback = null)
        {
            if (_next.IsAuto) await PerformAttack(channel);
            else if (callback != null) await callback();
        }

        private async Task PerformAttack(ISocketMessageChannel channel)
        {
            var other = Other;
            var attacker = _next.Submission;
            var defender = other.Submission;
            var bonus = attacker.Element.HasAdvantage(defender.Element) ? attacker.Attack / 6 : 0;
            var damage = attacker.Attack * (defender.Defense / 100) + bonus;
            if (other.Defense - damage <= 0) other.Defense = 0;
            else other.Defense -= damage;
            other.Health -= damage;
            await channel.SendMessageAsync(other.Health > 0
                ? $"{attacker.Name} attacked{(bonus != 0 ? " with an elemental advantage " : " ")}and dealt {damage} damage, {defender.Name} is now at {other.Health}HP!"
                : $"{attacker.Name} dealt the final blow, {defender.Name} is now at 0HP!");
            if (other.Health <= 0)
            {
                await channel.SendMessageAsync($"{attacker.Name} defeated {defender.Name}, {attacker.Name} wins!");
                if (Program.Matches.Battles.Count == 0 && _currentWinners.Item1 == Guid.Empty)
                {
                    await channel.SendMessageAsync(
                        $"{_next.User.Mention} wins this week! {attacker.Name} will be added to the mod.");
                    await Context.Guild.GetTextChannel(Program.Config.AnnouncementsChannel)
                        .SendMessageAsync("Applications are now open!");
                    attacker.IsWinner = true;
                    var role = Context.Guild.GetRole(Program.Config.Guild);
                    foreach (var submission in Program.Matches.Submissions.Values)
                        await Context.Guild.GetUser(submission.Owner).RemoveRoleAsync(role);
                    //Program.Matches.Submissions.Clear();
                    Program.Matches.InBattle = false;
                    var votes = Context.Guild.GetTextChannel(Program.Config.VotesChannel);
                    if (votes != null) await votes.DeleteMessagesAsync(CommandHandler.VoteMessages.Keys);
                    CommandHandler.VoteMessages.Clear();
                    await Program.Matches.UpdateContestants();
                    await Program.Matches.UpdateHoF();
                    Program.Listener.Refresh();
                }
                else if (_currentWinners.Item1 == Guid.Empty) _currentWinners.Item1 = _next.Id;
                else if (_currentWinners.Item2 == Guid.Empty)
                {
                    _currentWinners.Item2 = _next.Id;
                    Program.Matches.Battles.Add(_currentWinners);
                    _currentWinners = (Guid.Empty, Guid.Empty);
                }

                EndBattle();
                await Program.Matches.UpdateBattles();
                return;
            }

            if (_bonusTurns-- <= 0) _next = other;
            await CheckNext(channel,
                async () => await channel.SendMessageAsync($"It's {_next.User.Mention}'s turn!"));
        }

        private static void EndBattle()
        {
            _leftContestant = null;
            _rightContestant = null;
            _next = null;
            _bonusTurns = 0;
        }
    }
}
