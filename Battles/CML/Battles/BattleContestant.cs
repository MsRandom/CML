using System;
using Discord.WebSocket;

namespace CML.Battles
{
    public class BattleContestant
    {
        public Guid Id { get; }
        public Submission Submission { get; }
        public SocketGuildUser User { get; }
        public int Health { get; set; }
        public int Defense { get; set; }
        public bool IsAuto { get; set; }
        

        public BattleContestant(Guid id, Submission submission, SocketGuildUser user)
        {
            Id = id;
            Submission = submission;
            User = user;
            Health = 150;
            Defense = submission.Defense;
        }
    }
}
