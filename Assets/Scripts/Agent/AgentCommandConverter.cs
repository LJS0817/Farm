using System;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // JObject를 사용하기 위해 필요합니다.

public class AgentCommandConverter : JsonConverter<AgentCommand>
{
    public override void WriteJson(JsonWriter writer, AgentCommand value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        writer.WritePropertyName("Action");
        serializer.Serialize(writer, value.Action);

        writer.WritePropertyName("TargetGridPos");
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(value.TargetGridPos.x);
        writer.WritePropertyName("y");
        writer.WriteValue(value.TargetGridPos.y);
        writer.WriteEndObject();

        if (value.Crop != TileData.CropType.IsEmpty)
        {
            writer.WritePropertyName("Crop");
            serializer.Serialize(writer, value.Crop);
        }

        writer.WriteEndObject();
    }

    public override AgentCommand ReadJson(JsonReader reader, Type objectType, AgentCommand existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        JObject jo = JObject.Load(reader);
        AgentCommand cmd = new AgentCommand();

        if (jo["Action"] != null)
        {
            cmd.Action = jo["Action"].ToObject<ACTION_TYPE>(serializer);
        }

        if (jo["TargetGridPos"] != null)
        {
            int x = jo["TargetGridPos"]["x"]?.Value<int>() ?? 0;
            int y = jo["TargetGridPos"]["y"]?.Value<int>() ?? 0;
            cmd.TargetGridPos = new Vector2Int(x, y);
        }

        if (jo["Crop"] != null)
        {
            cmd.Crop = jo["Crop"].ToObject<TileData.CropType>(serializer);
        }

        return cmd;
    }
}