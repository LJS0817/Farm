using System;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // JObject를 사용하기 위해 필요합니다.

public class AgentCommandConverter : JsonConverter<AgentCommand>
{
    // 1. C# -> JSON 변환 (작성하신 ToString() 형식으로 출력)
    public override void WriteJson(JsonWriter writer, AgentCommand value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        writer.WriteValue(value.ToString());
    }

    // 2. JSON -> C# 변환 (서버에서 넘어온 {} 객체 형식을 해석)
    public override AgentCommand ReadJson(JsonReader reader, Type objectType, AgentCommand existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // 서버에서 온 데이터가 null이거나 빈 값이면 null 반환
        if (reader.TokenType == JsonToken.Null) return null;

        // JSON을 JObject(트리 구조)로 읽어들임
        JObject jo = JObject.Load(reader);
        AgentCommand cmd = new AgentCommand();

        // 1. Action 파싱 (문자열 Enum 변환)
        if (jo["Action"] != null)
        {
            cmd.Action = jo["Action"].ToObject<ACTION_TYPE>(serializer);
        }

        // 2. TargetGridPos 파싱 (불필요한 magnitude 등은 무시하고 x, y만 추출)
        if (jo["TargetGridPos"] != null)
        {
            int x = jo["TargetGridPos"]["x"]?.Value<int>() ?? 0;
            int y = jo["TargetGridPos"]["y"]?.Value<int>() ?? 0;
            cmd.TargetGridPos = new Vector2Int(x, y);
        }

        // 3. Crop 파싱 (문자열 Enum 변환)
        if (jo["Crop"] != null)
        {
            cmd.Crop = jo["Crop"].ToObject<TileData.CropType>(serializer);
        }

        return cmd;
    }
}