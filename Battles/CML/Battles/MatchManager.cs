using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CML.Battles
{
    public sealed class MatchManager
    {
        public readonly List<dynamic> Submissions = new List<dynamic>();
        public readonly List<Tuple<long, long>> Battles = new List<Tuple<long, long>>();
        private readonly FileInfo _contestantInfo;


        public MatchManager()
        {
            var contestants = new FileInfo("contestants.json");
            if (contestants.Exists) Submissions = JsonConvert.DeserializeObject<List<dynamic>>(contestants.OpenText().ReadToEnd());
            else contestants.Create();
            _contestantInfo = contestants;
        }

        public async Task UpdateStorage()
        {
            await using var writer = new StreamWriter(_contestantInfo.OpenWrite());
            await writer.WriteLineAsync(JsonConvert.SerializeObject(Submissions));
        }
    }
}
