using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace COTHPlugin.COTHPlugin.JsonSerializers
{
    class BoundingSphereDJsonConverter : JsonConverter<BoundingSphereD>
    {
        public override BoundingSphereD Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string input = reader.GetString();
            List<string> inputList = input.Split(':').ToList();
            return new BoundingSphereD(new Vector3D(Convert.ToDouble(inputList[0]), Convert.ToDouble(inputList[1]), Convert.ToDouble(inputList[2])), Convert.ToDouble(inputList[3]));
        }

        public override void Write(Utf8JsonWriter writer, BoundingSphereD value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.Center.X}:{value.Center.Y}:{value.Center.Z}:{value.Radius}");
        }
    }
}
