using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace COTHPlugin.COTHPlugin.JsonSerializers
{
    class IMyFactionJsonConverter : JsonConverter<IMyFaction>
    {
        public override IMyFaction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string jsonReturn = reader.GetString();
            return MySession.Static.Factions.TryGetFactionById(Convert.ToInt64(jsonReturn));
        }

        public override void Write(Utf8JsonWriter writer, IMyFaction value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.FactionId}");
        }
    }
}
