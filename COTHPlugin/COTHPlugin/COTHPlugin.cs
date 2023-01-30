using NLog;
using System.Text;
using System.Text.Json;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;
using COTHPlugin.COTHPlugin.JsonSerializers;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using System.Threading.Tasks;

namespace COTHPlugin.COTHPlugin
{
    public class COTHPlugin : TorchPluginBase
    {
        private int counter = 0;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        internal static Dictionary<ulong, long> rewardDue;
        private Persistent<COTHConfig> config;
        private static List<COTHZone> zones;

        internal static COTHPlugin Instance { get; private set; }
        internal static List<COTHZone> Zones { get => zones; set => zones = value; }
        public COTHConfig Config => config?.Data;


        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");
            Log.Info("Plugin is working");

            Instance = this;

            MyVisualScriptLogicProvider.PlayerConnected += OnPlayerJoined;

            SetupConfig();
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {

                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    LoadZones();
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");
                    SaveZones();
                    break;
            }
        }

        private void SetupConfig()
        {
           
            var configFile = Path.Combine(StoragePath, "COTHConfig.cfg");

            try
            {
                config = Persistent<COTHConfig>.Load(configFile);
            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (config?.Data == null)
            {

                Log.Info("Create Default Config, because none was found!");

                config = new Persistent<COTHConfig>(configFile, new COTHConfig());
                config.Save();
            }
        }

        internal void SaveZones()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Converters =
                {
                    new BoundingSphereDJsonConverter(),
                    new IMyFactionJsonConverter()
                }
            };
            string serializedData;
            serializedData = JsonSerializer.Serialize(zones, typeof(List<COTHZone>), options);
            File.WriteAllText(Path.Combine(StoragePath, "COTHStorage.json"), serializedData);
            Log.Info("Zones saved!");
            for (int i = 0; i < zones.Count; i++)
            {
                if (!Directory.Exists(Path.Combine(StoragePath, $"COTHZones"))) { Directory.CreateDirectory(Path.Combine(StoragePath, $"COTHZones")); }
                File.WriteAllText(Path.Combine(StoragePath, $"COTHZones/{zones[i].Name}.txt"), zones[i].ExportToVano());
            }
            serializedData = JsonSerializer.Serialize(rewardDue, typeof(Dictionary<ulong, long>));
            File.WriteAllText(Path.Combine(StoragePath, "COTHReward.json"), serializedData);
            Log.Info("Rewards saved!");
        }

        internal void LoadZones()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Converters =
                {
                    new BoundingSphereDJsonConverter(),
                    new IMyFactionJsonConverter()
                }
            };
            string serializedData;
            try
            {
                serializedData = File.ReadAllText(Path.Combine(StoragePath, "COTHStorage.json"));
                zones = JsonSerializer.Deserialize<List<COTHZone>>(serializedData, options);
                Log.Info($"Zones loaded! {serializedData}");
            }
            catch (Exception ex) 
            { 
                Log.Info(ex);
                zones = new List<COTHZone>();
            }
            try
            {
                serializedData = File.ReadAllText(Path.Combine(StoragePath, "COTHReward.json"));
                rewardDue = JsonSerializer.Deserialize<Dictionary<ulong, long>>(serializedData);
                Log.Info("Rewards loaded");
                if (rewardDue != null && rewardDue.Count > 0) { Log.Info($"{rewardDue.Keys.ToList()[0]} {rewardDue.Values.ToList()[0]}"); }
            }
            catch (Exception ex) 
            { 
                Log.Info(ex);
                rewardDue = new Dictionary<ulong, long>();
            }
            
        }

        private void OnPlayerJoined(long id)
        {
            OnPlayerJoinedAsync(id); // No, you can't just subscribe this method to event, i tried
        }

        private async Task OnPlayerJoinedAsync(long id)
        {
            for(int i = 0; i < 100; i++)
            {
                if(MySession.Static.Players.GetOnlinePlayers().Any(x => x.Identity.IdentityId == id))
                {
                    break;
                }
                await Task.Delay(5000);
            }
            try
            {
                MyPlayer player = MySession.Static.Players.GetOnlinePlayers().First(x => x.Identity.IdentityId == id);
                ulong steamId = player.Id.SteamId;
                if (rewardDue != null && rewardDue.ContainsKey(steamId))
                {
                    (player as IMyPlayer).RequestChangeBalance(rewardDue[steamId]);
                    Log.Info($"Rewarded {steamId} with {rewardDue[steamId]}");
                    rewardDue.Remove(steamId);
                }
                foreach(COTHZone z in zones)
                {
                    z.OnPlayerJoinedToServer(player);
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex);
            }
        }

        internal void AddZone(COTHZone z)
        {
            zones.Add(z);
        }

        public override void Update()
        {
            if (counter >= config.Data.UPS * 60)
            {
                counter = 0;
                if (zones != null && zones.Count > 0)
                {
                    foreach (COTHZone z in zones)
                    {
                        z.Update(config.Data.UPS * 60);
                    }
                }
                if (zones.Any(x => x.Finished))
                {
                    zones.RemoveAll(x => x.Finished);
                }
            }
            counter++;
        }
    }
}
