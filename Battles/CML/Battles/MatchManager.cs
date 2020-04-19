using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CML.Battles
{
    public sealed class MatchManager
    {
        public readonly DataCollection<Application> Applications = new DataCollection<Application>("apps.json");
        public readonly DataCollection<Submission> Submissions = new DataCollection<Submission>("contestants.json");
        public readonly List<PartialSubmission> HallOfFame = new List<PartialSubmission>();
        public readonly List<(Guid, Guid)> Battles = new List<(Guid, Guid)>();
        public bool InBattle;
        public const string ContestantDir = "contestant_data/";
        private const string BattleInfo = "battles.json";
        private const string HoFInfo = "fame.json";

        public MatchManager()
        {
            if (!Directory.Exists(ContestantDir)) Directory.CreateDirectory(ContestantDir);
            if (File.Exists(HoFInfo))
                HallOfFame = JsonConvert.DeserializeObject<List<PartialSubmission>>(File.ReadAllText(HoFInfo));
            else
            {
                using var writer = File.CreateText(HoFInfo);
                writer.WriteLine("[]");
                writer.Close();
            }
            if (File.Exists(BattleInfo))
            {
                foreach (var arr in JsonConvert.DeserializeObject<string[][]>(
                    File.ReadAllText(BattleInfo)))
                {
                    Battles.Add((arr[0] == null ? Guid.Empty : Guid.Parse(arr[0]),
                        arr[1] == null ? Guid.Empty : Guid.Parse(arr[1])));
                }
            }
            else
            {
                using var writer = File.CreateText(BattleInfo);
                writer.WriteLine("[]");
                writer.Close();
            }
        }

        public async Task UpdateApplications()
        {
            await Applications.Update();
        }
        
        public async Task UpdateContestants()
        {
            await Submissions.Update();
        }
        
        public async Task UpdateBattles()
        {
            var obj = new JArray();
            foreach (var (item1, item2) in Battles)
            {
                obj.Add(new JArray
                {
                    item1 == Guid.Empty ? null : item1.ToString(), item2 == Guid.Empty ? null : item2.ToString()
                });
            }

            await File.WriteAllTextAsync(BattleInfo, obj.ToString());
        }
        
        public async Task UpdateHoF()
        {
            await File.WriteAllTextAsync(HoFInfo, JsonConvert.SerializeObject(HallOfFame));
        }
    }

    public class DataCollection<T> : Dictionary<Guid, T>
    {
        private readonly string _file;
        
        public DataCollection(string file)
        {
            if (File.Exists(file))
            {
                foreach (var (id, data) in JsonConvert.DeserializeObject<Dictionary<string, T>>(
                    File.ReadAllText(file)))
                    this[Guid.Parse(id)] = data;
            }
            else
            {
                using var writer = File.CreateText(file);
                writer.WriteLine("{}");
                writer.Close();
            }

            _file = file;
        }

        public async Task Update()
        {
            var obj = new JObject();
            foreach (var (id, data) in this)
            {
                obj[id.ToString()] = JObject.FromObject(data);
            }

            await File.WriteAllTextAsync(_file, obj.ToString());
        }
    }
}