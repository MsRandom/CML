using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using CML.Battles;
using CML.Bot;
using Newtonsoft.Json.Linq;

namespace CML.Site
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public static class EndpointHandlers
    {
        public static readonly List<Guid> Authorized = new List<Guid>();
        
        public static JContainer GetClientToken(HttpListenerRequest req, string s, dynamic o)
        {
            dynamic res = new JObject();
            res.token = Guid.NewGuid().ToString();
            return res;
        }
        
        public static JContainer ValidateToken(HttpListenerRequest req, string s, dynamic o)
        {
            dynamic res = new JObject();
            var valid = Program.Config.DiscordToken.Equals((string) o.token);
            res.valid = valid;
            if(valid) Authorized.Add(Guid.Parse(req.Headers.Get("Client-Token")));
            return res;
        }
        
        public static JContainer Battles0GetBattles(HttpListenerRequest req, string s, dynamic o)
        {
            dynamic res = new JObject();
            var contestants = new JObject();
            var battles = new JArray();
            foreach (var (id, submission) in Program.Matches.Submissions) contestants.Add(id.ToString(), JObject.FromObject(submission));
            foreach (var (left, right) in Program.Matches.Battles)
            {
                dynamic obj = new JObject();
                obj.left = left == Guid.Empty ? null : left.ToString();
                obj.right = right == Guid.Empty ? null : right.ToString();
                battles.Add(obj);
            }

            res.contestants = contestants;
            res.battles = battles;
            return res;
        }
            
        public static JContainer Battles0EnterSubmission(HttpListenerRequest req, string s, dynamic o)
        {
            var res = new JObject();
            if (!(o is Dictionary<string, object> dict)) return res;
            var application = Program.Matches.Applications[Guid.Parse((string) dict["id"])];
            var guild = Program.Discord.Client.GetGuild(Program.Config.Guild);
            var channel = guild.GetTextChannel(Program.Config.ApplicationsChannel);
            var user = guild.GetUser(application.User);
            if (user == null) return res;
            var element = application.Element.GetValueOrDefault();
            var elemName = element.ToString().ToLower();
            var (modelName, modelData) = ((string, byte[])) dict["model"];
            var (pictureName, pictureData) = ((string, byte[])) dict["picture"];
            var (textureName, textureData) = ((string, byte[])) dict["texture"];
            var submission = new Submission
            {
                Owner = application.User,
                Name = (string) dict["name"],
                Attack = Convert.ToInt32((string) dict["attack"]),
                Defense = Convert.ToInt32((string) dict["defense"]),
                Speed = Convert.ToInt32((string) dict["speed"]),
                Element = element,
                ModelLocation = elemName + "/" + modelName,
                PictureLocation = elemName + "/" + pictureName,
                TextureLocation = elemName + "/" + textureName
            };
            var dir = MatchManager.ContestantDir + elemName;
            var assets = Program.Config.SiteRoot + "contestants/";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!Directory.Exists(assets)) Directory.CreateDirectory(dir);
            if (!Directory.Exists(assets + elemName)) Directory.CreateDirectory(assets + elemName);
            if (!Directory.Exists(MatchManager.ContestantDir + elemName)) Directory.CreateDirectory(MatchManager.ContestantDir + elemName);
            File.WriteAllBytes(MatchManager.ContestantDir + submission.ModelLocation, modelData);
            File.WriteAllBytes(assets + submission.PictureLocation, pictureData);
            File.WriteAllBytes(MatchManager.ContestantDir + submission.TextureLocation, textureData);
            CommandHandler.PendingData[application.User] = submission;
            channel.SendMessageAsync(
                    $"{user.Mention}, please type !submit to confirm your submission or !cancel to cancel the request.")
                .GetAwaiter().GetResult();
            return res;
        }
        
        public static JContainer Battles0GetUsers(HttpListenerRequest req, string s, dynamic o)
        {
            var res = new JArray();
            foreach (var user in Program.Discord.Client.GetGuild(Program.Config.Guild).Users)
            {
                dynamic obj = new JObject();
                obj.user = user.Id.ToString();
                obj.name = user.Username;
                res.Add(obj);
            }
            return res;
        }
        
        public static JContainer Battles0Apply(HttpListenerRequest req, string s, dynamic o)
        {
            dynamic res = new JObject();
            var result = "failed";
            var userId = Convert.ToUInt64((string) o.user);
            if (CommandHandler.Expectations.ContainsKey(userId))
            {
                if (CommandHandler.Confirmed.ContainsKey(userId))
                {
                    CommandHandler.Expectations.Remove(userId);
                    var (id, password) = CommandHandler.Confirmed[userId];
                    if (password.Equals(o.password.ToString()))
                    {
                        result = "success-added";
                        res.id = id.ToString();
                    }
                }
            }
            else
            {
                if (Program.Matches.Applications.Count < 8)
                {
                    if (Program.Matches.Applications.All(app => app.Value.User != userId))
                    {
                        CommandHandler.Expectations[userId] = (ExpectationType.Apply, o.password.ToString());
                        var guild = Program.Discord.Client.GetGuild(Program.Config.Guild);
                        var channel = guild.GetTextChannel(Program.Config.ApplicationsChannel);
                        var user = guild.GetUser(userId);
                        if (user != null)
                        {
                            channel.SendMessageAsync(
                                    $"{user.Mention}, please type !apply to confirm your application or !cancel to cancel the request.")
                                .GetAwaiter().GetResult();
                            result = "success";
                        }
                    }
                }
                else
                {
                    result = "filled";
                }
            }

            res.result = result;
            return res;
        }

        public static JContainer Battles0Login(HttpListenerRequest req, string s, dynamic o)
        {
            dynamic res = new JObject();
            var id = Guid.Parse((string) o.id);
            var result = false;
            if (Program.Matches.Applications.ContainsKey(id))
            {
                var app = Program.Matches.Applications[id];
                if (app.Password.Equals((string) o.password))
                {
                    result = true;
                    res.user = app.User.ToString();
                    res.element = app.Element.HasValue ? app.Element.ToString() : null;
                }
            }

            res.success = result;
            return res;
        }

        public static JContainer Battles0UpdateUser(HttpListenerRequest req, string s, dynamic o)
        {

            var id = Guid.Parse((string) o.id);
            var userId = Convert.ToUInt64((string) o.user);
            CommandHandler.Expectations[userId] = (ExpectationType.Update, o.password.ToString());
            CommandHandler.PendingData[userId] = id;
            var guild = Program.Discord.Client.GetGuild(Program.Config.Guild);
            var channel = guild.GetTextChannel(Program.Config.ApplicationsChannel);
            var user = guild.GetUser(userId);
            if (user != null)
            {
                channel.SendMessageAsync(
                        $"{user.Mention}, please type !update to confirm your application update or !cancel to cancel the request.")
                    .GetAwaiter().GetResult();
            }

            return new JObject();
        }

        public static JContainer Battles0GetHoF(HttpListenerRequest req, string s, dynamic o)
        {
            var res = JArray.FromObject(Program.Matches.HallOfFame);
            foreach (var element in res)
            {
                var parsedElem = (dynamic) element;
                parsedElem.owner = Program.Discord.Client.GetUser((ulong) parsedElem.owner).Username;
            }

            return res;
        }
        
        public static JContainer Battles0UpdateMatches(HttpListenerRequest req, string s, dynamic o, bool auth = true)
        {
            var res = new JObject();
            if (!(o.updated is JArray arr)) return res;
            Program.Matches.Battles.Clear();
            foreach (var match in arr)
                Program.Matches.Battles.Add((match[0] == null ? Guid.Empty : Guid.Parse(match[0].ToString()),
                    match[1] == null ? Guid.Empty : Guid.Parse(match[1].ToString())));
            return res;
        }
    }
}
