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
            cmd.Crop = ParseCropTypeSafely(jo["Crop"]);
        }

        return cmd;
    }

    private static TileData.CropType ParseCropTypeSafely(JToken cropToken)
    {
        if (cropToken == null || cropToken.Type == JTokenType.Null)
        {
            return TileData.CropType.IsEmpty;
        }

        string rawValue = cropToken.Value<string>()?.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return TileData.CropType.IsEmpty;
        }

        if (TryParseCropType(rawValue, out TileData.CropType parsedCrop))
        {
            return parsedCrop;
        }

        Debug.LogWarning($"[AgentCommandConverter] Unknown Crop value '{rawValue}'. Fallback to IsEmpty.");
        return TileData.CropType.IsEmpty;
    }

    private static bool TryParseCropType(string rawValue, out TileData.CropType cropType)
    {
        if (Enum.TryParse(rawValue, true, out cropType))
        {
            return true;
        }

        string normalizedValue = NormalizeCropValue(rawValue);
        return Enum.TryParse(normalizedValue, true, out cropType);
    }

    private static string NormalizeCropValue(string rawValue)
    {
        string value = rawValue.Trim();

        if (value.EndsWith(" seeds", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^6].Trim();
        }
        else if (value.EndsWith(" seed", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^5].Trim();
        }
        else if (value.EndsWith("씨앗", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^2].Trim();
        }

        return value switch
        {
            "당근" => nameof(TileData.CropType.Carrot),
            "체리" => nameof(TileData.CropType.Cherry),
            "없음" => nameof(TileData.CropType.IsEmpty),
            _ => value
        };
    }
}
