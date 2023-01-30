using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using Sandbox.Game.GameSystems.BankingAndCurrency;

namespace COTHPlugin.COTHPlugin
{
    internal interface IRewardable 
    {
        long Score { get; }
        string Name { get; }

        void PayReward(long reward);
    }

    internal class COTHZone
    {
        private string name;
        private BoundingSphereD COTHSphere;
        private long durationInTicks;
        private bool finished = false;

        private List<COTHFaction> myFactions = new List<COTHFaction>();
        private List<COTHPlayer> players = new List<COTHPlayer>();

        public COTHZone(string name, BoundingSphereD COTHSphere, TimeSpan duration)
        {
            this.name = name;
            this.COTHSphere = COTHSphere;
            durationInTicks = (long)duration.TotalSeconds * 60;
            
            foreach (MyFaction f in MySession.Static.Factions.Factions.Values)
            {
                myFactions.Add(new COTHFaction(f as IMyFaction));
            }
            SubscribeToEvents();
        }

        [JsonConstructor]
        public COTHZone(string Name, BoundingSphereD Sphere, long DurationInTicks, bool Finished, List<COTHFaction> MyFactions, List<COTHPlayer> Players)
        {
            this.Name = Name;
            this.Sphere = Sphere;
            this.DurationInTicks = DurationInTicks;
            this.Finished = Finished;
            this.MyFactions = MyFactions;
            this.Players = Players;
            SubscribeToEvents();
        }

        public string Name { get => name; set => name = value; }
        public BoundingSphereD Sphere { get { return COTHSphere; } set => COTHSphere = value; }
        public long DurationInTicks { get { return durationInTicks; } set => durationInTicks = value; }
        public bool Finished { get { return finished; } set => finished = value; }

        public List<COTHFaction> MyFactions { get => myFactions; set => myFactions = value; }
        public List<COTHPlayer> Players { get => players; set => players = value; }
        [JsonIgnore]
        public string Top10Score 
        { 
            get
            {
                List<IRewardable> candidates = SortedCandidates.GetRange(0, Math.Min(10, SortedCandidates.Count));
                string output = "";
                foreach (IRewardable r in candidates)
                {
                    output += $"{r.Name}: {r.Score}\n";
                }
                return output;
            }
        }
        private List<IRewardable> SortedCandidates
        {
            get
            {
                List<IRewardable> candidates = new List<IRewardable>();

                if (players == null || players.Count == 0) { return candidates; }
                foreach (COTHPlayer p in players)
                {
                    if (!p.IsInFaction)
                    {
                        candidates.Add(p);
                    }
                }
                if (myFactions != null && myFactions.Count > 0)
                {
                    foreach (COTHFaction f in myFactions)
                    {
                        candidates.Add(f);
                    }
                }
                candidates.Sort(new RewardableComparer());
                candidates.Reverse();
                return candidates;
            }
        }

        private void SubscribeToEvents()
        {
            MySession.Static.Factions.OnPlayerJoined += OnPlayerJoinedToFaction;
            MySession.Static.Factions.OnPlayerLeft += OnPlayerLeftFromFaction;
            MySession.Static.Factions.FactionCreated += OnNewFactionCreated;
        }

        private void OnNewFactionCreated(long id)
        {
            IMyFaction createdFaction = MySession.Static.Factions.TryGetFactionById(id) as IMyFaction;
            COTHFaction createdFactionAsCOTHFaction = new COTHFaction(createdFaction);
            ulong steamIdOfCreator = MySession.Static.Players.GetOnlinePlayers().First(x => x.Identity.IdentityId == createdFaction.Members.First().Value.PlayerId).Id.SteamId;
            if (players.Any(x => x.SteamId == steamIdOfCreator))
            {
                createdFactionAsCOTHFaction.FactionMembers.Add(players.First(x => x.SteamId == steamIdOfCreator));
            }
            myFactions.Add(createdFactionAsCOTHFaction);
        }

        private void OnPlayerLeftFromFaction(MyFaction faction, long playerIdentityId)
        {
            ulong playerSteamId = MySession.Static.Players.GetOnlinePlayers().First(x => x.Identity.IdentityId == playerIdentityId).Id.SteamId;
            if (players.Any(x => x.SteamId == playerSteamId))
            {
                myFactions.First(x => x.Faction.FactionId == faction.FactionId).OnPlayerLeft(players.First(x => x.SteamId == playerSteamId));
            }
        }

        private void OnPlayerJoinedToFaction(MyFaction faction, long playerIdentityId)
        {
            ulong playerSteamId = MySession.Static.Players.GetOnlinePlayers().First(x => x.Identity.IdentityId == playerIdentityId).Id.SteamId;
            if (players.Any(x => x.SteamId == playerSteamId))
            {
                myFactions.First(x => x.Faction.FactionId == faction.FactionId).FactionMembers.Add(players.First(x => x.SteamId == playerSteamId));
            }
        }

        public void OnPlayerJoinedToServer(MyPlayer player)
        {
            MyVisualScriptLogicProvider.AddGPSObjective(name, $"COTHZone with radius of {COTHSphere.Radius} meters", COTHSphere.Center, new Color(255f, 0, 0), playerId: player.Identity.IdentityId);
            COTHPlayer customPlayer = null;
            if (players.Any(x => x.SteamId == player.Id.SteamId))
            {
                customPlayer = players.First(x => x.SteamId == player.Id.SteamId);
                if (!customPlayer.IsInFaction)
                {
                    IMyFaction playerFaction = MySession.Static.Factions.TryGetPlayerFaction(player.Identity.IdentityId);
                    if (playerFaction != null)
                    {
                        myFactions.First(x => x.Faction.FactionId == playerFaction.FactionId).FactionMembers.Add(customPlayer);
                    }
                }
            } 
        }

        public void Update(int TPU)
        {
            List<MyEntity> tempEntities = MyEntities.GetEntitiesInSphere(ref COTHSphere);
            List<MyEntity> tempCharacters = tempEntities.FindAll(x => x.DefinitionId.HasValue && x.DefinitionId.Value.TypeId == new MyObjectBuilderType(typeof(MyObjectBuilder_Character)));
            tempEntities.Clear();

            if (tempCharacters != null && tempCharacters.Count > 0)
            {
                foreach (MyEntity e in tempCharacters)
                {
                    ulong steamId = (e as MyCharacter).ControlSteamId;
                    if (players.Any(x => x.SteamId == steamId))
                    {
                        players.Single(x => x.SteamId == steamId).Score += 1;
                    }
                    else
                    {
                        COTHPlayer player = new COTHPlayer(steamId, 1, (e as MyCharacter).ModelName);

                        if (!MySession.Static.Players.TryGetPlayerBySteamId(steamId, out MyPlayer keenPlayer)) continue;
                        IMyFaction playerFaction = MySession.Static.Factions.TryGetPlayerFaction(keenPlayer.Identity.IdentityId);
                        if (playerFaction != null)
                        {
                            player.IsInFaction = true;
                            myFactions.Single(x => x.Faction.FactionId == playerFaction.FactionId).FactionMembers.Add(player);
                        }

                        players.Add(player);
                    }
                }
            }
            tempCharacters.Clear();

            durationInTicks -= TPU;
            if (durationInTicks <= 0)
            {
                double percent = COTHPlugin.Instance.Config.PercentOfWinners;
                int multiplier = COTHPlugin.Instance.Config.Multiplier;

                finished = true;
                List<IRewardable> candidates = SortedCandidates;

                int count = 1;
                ulong totalScore = 0;
                foreach (IRewardable r in candidates)
                {
                    totalScore += Convert.ToUInt64(r.Score);
                }
                for (int i = 0; i < candidates.Count * percent; i++)
                {
                    candidates[i].PayReward((long)((totalScore * (ulong)multiplier) / Math.Pow(2, i+1)));
                }
                COTHPluginCommands.chatManager.SendMessageAsSelf($"Ивент {name} закончился");
            }
        }



        public string ExportToVano()
        {
            COTHFaction[] factionsArray = new COTHFaction[myFactions.Count];
            myFactions.CopyTo(factionsArray);
            List<COTHFaction> factions = factionsArray.ToList();
            factions.Sort(new RewardableComparer());
            factions.Reverse();

            string output = "";

            for(int i = 0; i < Math.Min(10, factions.Count); i++)
            {
                output += $"{factions[i].Faction.Name} {factions[i].Score}\n";
            }

            output += "я разделяю топ\n";

            COTHPlayer[] peoplesArray = new COTHPlayer[players.Count];
            players.CopyTo(peoplesArray);
            List<COTHPlayer> peoples = peoplesArray.ToList();
            factions.Sort(new RewardableComparer());
            factions.Reverse();

            for(int i = 0; i < Math.Min(10, peoples.Count); i++)
            {
                output += $"{peoples[i].Name}{peoples[i].Score}";
                if (i < 9)
                {
                    output += '\n';
                }
            }

            return output;
        }
    }

    internal class RewardableComparer : IComparer<IRewardable>
    {
        public int Compare(IRewardable x, IRewardable y)
        {
            return (int)(x.Score - y.Score);
        }
    }


    internal class COTHFaction : IRewardable
    {
        private IMyFaction faction;
        private List<COTHPlayer> factionMembers;
        private long scoreOfMembersThatLeft;

        public COTHFaction(IMyFaction faction)
        {
            this.faction = faction;
            factionMembers = new List<COTHPlayer>();
            scoreOfMembersThatLeft = 0;
        }

        public COTHFaction(IMyFaction faction, long scoreOfMembersThatLeft)
        {
            this.faction = faction;
            factionMembers = new List<COTHPlayer>();
            this.scoreOfMembersThatLeft = scoreOfMembersThatLeft;
        }

        [JsonConstructor]
        public COTHFaction(IMyFaction Faction, List<COTHPlayer> FactionMembers, long ScoreOfMembersThatLeft)
        {
            this.Faction = Faction;
            this.FactionMembers = FactionMembers;
            this.ScoreOfMembersThatLeft = ScoreOfMembersThatLeft;
        }

        public IMyFaction Faction { get { return faction; } set => faction = value; }
        public List<COTHPlayer> FactionMembers { get { return factionMembers; } set { factionMembers = value; } }
        public long ScoreOfMembersThatLeft { get { return scoreOfMembersThatLeft; } set => scoreOfMembersThatLeft = value; }
        [JsonIgnore()]
        public long Score 
        { 
            get 
            {
                long score = scoreOfMembersThatLeft;
                foreach (COTHPlayer p in factionMembers)
                {
                    score += p.Score;
                }
                return score;
            }
        }
        public string Name { get => faction.Name; }

        public void OnPlayerLeft(COTHPlayer player)
        {
            scoreOfMembersThatLeft += player.Score;
            player.Score = 0;
            player.IsInFaction = false;
            factionMembers.Remove(player);
        }

        public void OnPlayerAdded(COTHPlayer player)
        {
            factionMembers.Add(player);
            player.IsInFaction = true;
        }
        
        public void PayReward(long reward)
        {
            faction.RequestChangeBalance(reward);
        }

        public static bool operator ==(COTHFaction faction1, COTHFaction faction2)
        {
            return faction1.Faction.FactionId == faction2.Faction.FactionId;
        }

        public static bool operator !=(COTHFaction faction1, COTHFaction faction2)
        {
            return faction1.Faction.FactionId != faction2.Faction.FactionId;
        }

        public static bool operator <(COTHFaction rewardable1, IRewardable rewardable2)
        {
            return rewardable1.Score < rewardable2.Score;
        }

        public static bool operator >(COTHFaction rewardable1, IRewardable rewardable2)
        {
            return rewardable1.Score > rewardable2.Score;
        }

        public override bool Equals(object obj)
        {
            return obj is COTHFaction faction &&
                   this.faction.FactionId == faction.faction.FactionId;
        }
    }

    internal class COTHPlayer : IRewardable
    {
        private ulong steamId;
        private string name;
        private long score;
        private bool isInFaction = false;

        public COTHPlayer(ulong steamId)
        {
            this.steamId = steamId;
            score = 0;
        }

        public COTHPlayer(ulong steamId, long score, string name)
        {
            this.steamId = steamId;
            this.score = score;
        }

        [JsonConstructor]
        public COTHPlayer(ulong SteamId, string Name, long Score, bool IsInFaction)
        {
            this.SteamId = SteamId;
            this.Name = Name;
            this.Score = Score;
            this.IsInFaction = IsInFaction;
        }

        public void PayReward(long reward)
        {
            if(MySession.Static.Players.TryGetPlayerBySteamId(steamId, out MyPlayer tempPlayer))
            {
                (tempPlayer as IMyPlayer).RequestChangeBalance(reward);
                return;
            }
            else
            {
                if (COTHPlugin.rewardDue.ContainsKey(steamId))
                {
                    COTHPlugin.rewardDue[steamId] += reward;
                }
                else
                {
                    COTHPlugin.rewardDue[steamId] = reward;
                }
            }
        }

        public ulong SteamId { get { return steamId; } set => steamId = value; }
        public string Name { get => name; set => name = value; }
        public long Score { get { return score; } set { score = value; } }
        public bool IsInFaction { get { return isInFaction; } set { isInFaction = value; } }

        public static bool operator ==(COTHPlayer player1, COTHPlayer player2)
        {
            return player1.steamId == player2.steamId;
        }

        public static bool operator !=(COTHPlayer player1, COTHPlayer player2)
        {
            return player1.steamId != player2.steamId;
        }

        public static bool operator <(COTHPlayer rewardable1, IRewardable rewardable2)
        {
            return rewardable1.Score < rewardable2.Score;
        }

        public static bool operator >(COTHPlayer rewardable1, IRewardable rewardable2)
        {
            return rewardable1.Score > rewardable2.Score;
        }

        public override bool Equals(object obj)
        {
            return obj is COTHPlayer player &&
                steamId == player.steamId;
        }
    }
}
