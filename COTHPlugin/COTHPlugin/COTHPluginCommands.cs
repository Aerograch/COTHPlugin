using System;
using System.Linq;
using System.Collections.Generic;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage;
using VRage.Game;
using VRageMath;
using VRage.ObjectBuilder;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.Game.Screens.Helpers;
using Torch.API.Managers;

namespace COTHPlugin.COTHPlugin
{
    [Category("COTHPlugin")]
    public class COTHPluginCommands : CommandModule
    {
        public COTHPlugin Plugin => (COTHPlugin)Context.Plugin;
        public static IChatManagerServer chatManager = COTHPlugin.Instance.Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();

        [Command("GetZoneWithName", "Gets zone with a certain name")]
        [Permission(MyPromoteLevel.None)]
        public void GetZoneWithName(string name = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                if (COTHPlugin.Zones != null && COTHPlugin.Zones.Count > 0)
                {
                    foreach(COTHZone z in COTHPlugin.Zones)
                    {
                        Context.Respond(qGPS(z.Name, z.Sphere.Center));
                    }
                }
                else
                {
                    Context.Respond("There are no zones currently!");
                }
            }
            else
            {
                if (COTHPlugin.Zones != null && COTHPlugin.Zones.Count > 0)
                {
                    if (COTHPlugin.Zones.Any(x => x.Name == name))
                    {
                        COTHZone tempZone = COTHPlugin.Zones.First(x => x.Name == name);
                        Context.Respond(qGPS(tempZone.Name, tempZone.Sphere.Center)); 
                    }
                }
                else
                {
                    Context.Respond("There are no such zone currently!");
                }
            }
        }

        [Command("GetScoreOfTop10", "Gets score of the top 10 factions/players")]
        [Permission(MyPromoteLevel.None)]
        public void GetScoreOfTop10(string name = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                if (COTHPlugin.Zones != null && COTHPlugin.Zones.Count > 0)
                {
                    foreach (COTHZone z in COTHPlugin.Zones)
                    {
                        Context.Respond(z.Top10Score);
                    }
                }
                else
                {
                    Context.Respond("There are no zones currently!");
                }
            }
        }

        [Command("CreateZone", "This is a Test Command.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void CreateZone(string name, string gpsPoint, string radius = "500", string durationInHours = "1")
        {
            qGPS(gpsPoint, out Vector3D pointVector);
            COTHPlugin.Zones.Add(new COTHZone(name, 
                new BoundingSphereD(pointVector, Convert.ToDouble(radius)), 
                new TimeSpan(Convert.ToInt32(durationInHours), Convert.ToInt32(Convert.ToInt32(durationInHours) % 1*60), 0)));
            MyVisualScriptLogicProvider.AddGPSObjectiveForAll(name, $"COTHZone with radius of {radius} meters", pointVector, new Color(255f, 0, 0));
            COTHPlugin.Log.Info($"Createad a COTH zone at {qGPS(name, pointVector)}");
        }

        [Command("CreateZoneSeconds", "This is a Test Command.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void CreateZoneSeconds(string name, string gpsPoint, string radius = "500", string durationInSeconds = "60")
        {
            qGPS(gpsPoint, out Vector3D pointVector);
            COTHPlugin.Zones.Add(new COTHZone(name,
                new BoundingSphereD(pointVector, Convert.ToDouble(radius.Replace(" ", string.Empty))),
                new TimeSpan(0, Convert.ToInt32(durationInSeconds.Replace(" ", string.Empty)) /60, Convert.ToInt32(durationInSeconds.Replace(" ", string.Empty))%60)));
            MyVisualScriptLogicProvider.AddGPSObjectiveForAll(name, $"COTHZone with radius of {radius} meters", pointVector, new Color(255f, 0, 0));
            COTHPlugin.Log.Info($"Createad a COTH zone at {qGPS(name, pointVector)}");
        }

        [Command("DeleteZone", "Deletes test zone")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void DeleteZone(string name)
        {
            if (COTHPlugin.Zones.Any(x => x.Name == name))
            {
                COTHPlugin.Zones.Remove(COTHPlugin.Zones.First(x => x.Name == name));
                Context.Respond("Зона удалена!");
            }
            else
            {
                Context.Respond("Такой зоны нет!");
            }
        }

        public static bool qGPS(string GPS, out Vector3D V)
        {
            string[] gps = GPS.Split(':');
            if (gps.Length > 5 && double.TryParse(gps[2], out V.X) && double.TryParse(gps[3], out V.Y) && double.TryParse(gps[4], out V.Z)) { return true; }
            V = new Vector3D(); return false;
        }
        public static string qGPS(string Name, Vector3D V, Color C = new Color())
        {
            if (C == new Color()) { C = new Color(45, 78, 91); }
            return $"GPS:{Name}:{V.X}:{V.Y}:{V.Z}:#{C.R:X02}{C.G:X02}{C.B:X02}{C.A:X02}";
        }
    }
}
