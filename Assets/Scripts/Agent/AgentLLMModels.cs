using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

[JsonConverter(typeof(StringEnumConverter))]
public enum AgentIntentType
{
    Unknown,
    GeneralChat,
    Move,
    Plant,
    Harvest,
    Eat,
    QueryPosition,
    QueryInventory,
    QueryMap,
    QueryTile,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AgentInteractionType
{
    Unknown,
    Conversation,
    Command,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AgentValidationStatus
{
    Executable,
    Informational,
    Rejected,
}

[Serializable]
public class AgentIntentDecisionDto
{
    public string Intent;
}

[Serializable]
public class AgentInteractionDecisionDto
{
    public string Mode;
}

[Serializable]
public class GridPositionDto
{
    public int x = -1;
    public int y = -1;

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(x, y);
    }

    public static GridPositionDto FromVector2Int(Vector2Int value)
    {
        return new GridPositionDto
        {
            x = value.x,
            y = value.y,
        };
    }
}

[Serializable]
public class AgentFunctionArgumentsDto
{
    public GridPositionDto TargetGridPos = new GridPositionDto();
    public string Crop = nameof(TileData.CropType.IsEmpty);

    public static AgentFunctionArgumentsDto Default()
    {
        return new AgentFunctionArgumentsDto();
    }
}

[Serializable]
public class AgentReplyDto
{
    public string Reply;
}

[Serializable]
public class AgentInventoryEntryDto
{
    public string name;
    public int count;
}

[Serializable]
public class AgentTileSnapshotDto
{
    public int x;
    public int y;
    public string tileType;
    public string cropType;
    public string cropState;
    public bool isFarmable;
    public bool isWalkable;
    public float growDuration;
    public float maxTime;
}

[Serializable]
public class AgentMapContextDto
{
    public int width;
    public int height;
    public GridPositionDto topLeft;
    public GridPositionDto topRight;
    public GridPositionDto bottomLeft;
    public GridPositionDto bottomRight;
}

[Serializable]
public class AgentPlanningContextDto
{
    public string intent;
    public string userInput;
    public GridPositionDto currentPosition;
    public AgentMapContextDto map;
    public AgentTileSnapshotDto currentTile;
    public GridPositionDto referencedCoordinate;
    public AgentTileSnapshotDto referencedTile;
    public List<AgentTileSnapshotDto> adjacentTiles = new List<AgentTileSnapshotDto>();
    public List<AgentTileSnapshotDto> nonEmptyTiles = new List<AgentTileSnapshotDto>();
    public List<AgentInventoryEntryDto> inventory = new List<AgentInventoryEntryDto>();
    public OntologyData ontology;
}

[Serializable]
public class AgentValidationResultDto
{
    public string intent;
    public string status;
    public string reasonCode;
    public GridPositionDto currentPosition;
    public GridPositionDto targetGridPos;
    public string requestedCrop;
    public string requiredItemName;
    public int currentItemCount;
    public AgentTileSnapshotDto targetTile;
    public List<AgentInventoryEntryDto> inventory = new List<AgentInventoryEntryDto>();
    public List<AgentTileSnapshotDto> visibleCrops = new List<AgentTileSnapshotDto>();
    public string infoMessage;
}

public class AgentValidationResult
{
    public AgentIntentType intent;
    public AgentValidationStatus status;
    public string reasonCode;
    public Vector2Int currentPosition = new Vector2Int(-1, -1);
    public Vector2Int targetGridPos = new Vector2Int(-1, -1);
    public TileData.CropType requestedCrop = TileData.CropType.IsEmpty;
    public string requiredItemName;
    public int currentItemCount;
    public TileData targetTile;
    public string infoMessage;
    public readonly List<AgentCommand> commands = new List<AgentCommand>();

    public AgentValidationResultDto ToDto(List<AgentInventoryEntryDto> inventory, List<AgentTileSnapshotDto> visibleCrops)
    {
        return new AgentValidationResultDto
        {
            intent = intent.ToString(),
            status = status.ToString(),
            reasonCode = reasonCode,
            currentPosition = currentPosition.x >= 0 && currentPosition.y >= 0 ? GridPositionDto.FromVector2Int(currentPosition) : null,
            targetGridPos = targetGridPos.x >= 0 && targetGridPos.y >= 0 ? GridPositionDto.FromVector2Int(targetGridPos) : null,
            requestedCrop = requestedCrop.ToString(),
            requiredItemName = requiredItemName,
            currentItemCount = currentItemCount,
            targetTile = targetTile != null ? AgentLLMModelUtils.CreateTileSnapshot(targetTile, AgentLLMModelUtils.ComputeIsWalkable(targetTile)) : null,
            inventory = inventory ?? new List<AgentInventoryEntryDto>(),
            visibleCrops = visibleCrops ?? new List<AgentTileSnapshotDto>(),
            infoMessage = infoMessage,
        };
    }
}

public static class AgentLLMModelUtils
{
    public static string StripCodeFence(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        return rawText.Replace("```json", string.Empty).Replace("```", string.Empty).Trim();
    }

    public static string ExtractFirstJsonObject(string rawText)
    {
        string clean = StripCodeFence(rawText);
        if (string.IsNullOrWhiteSpace(clean))
        {
            return string.Empty;
        }

        int startIndex = clean.IndexOf('{');
        if (startIndex < 0)
        {
            return clean;
        }

        int depth = 0;
        bool inString = false;
        bool isEscaped = false;
        StringBuilder builder = new StringBuilder();

        for (int i = startIndex; i < clean.Length; i++)
        {
            char ch = clean[i];
            builder.Append(ch);

            if (isEscaped)
            {
                isEscaped = false;
                continue;
            }

            if (ch == '\\')
            {
                isEscaped = true;
                continue;
            }

            if (ch == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (ch == '{')
            {
                depth++;
            }
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return builder.ToString();
                }
            }
        }

        return clean;
    }

    public static T DeserializeJsonObject<T>(string rawText) where T : class
    {
        string json = ExtractFirstJsonObject(rawText);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch
        {
            return null;
        }
    }

    public static AgentTileSnapshotDto CreateTileSnapshot(TileData tile, bool isWalkable)
    {
        if (tile == null)
        {
            return null;
        }

        return new AgentTileSnapshotDto
        {
            x = tile.coord.x,
            y = tile.coord.y,
            tileType = tile.tileType.ToString(),
            cropType = tile.cropType.ToString(),
            cropState = tile.cropState.ToString(),
            isFarmable = tile.isFarmable,
            isWalkable = isWalkable,
            growDuration = tile.GrowDuration,
            maxTime = tile.maxTime,
        };
    }

    public static bool ComputeIsWalkable(TileData tile)
    {
        return tile != null
            && tile.tileType != TileData.TileType.Water
            && tile.tileType != TileData.TileType.Rock;
    }

    public static List<AgentInventoryEntryDto> BuildInventoryEntries(InventoryManager inventoryManager)
    {
        Dictionary<string, int> itemCounts = new Dictionary<string, int>();
        List<AgentInventoryEntryDto> entries = new List<AgentInventoryEntryDto>();

        if (inventoryManager == null)
        {
            return entries;
        }

        foreach (InventorySlot slot in inventoryManager.slots)
        {
            if (slot == null || slot.IsEmpty || slot.item == null)
            {
                continue;
            }

            if (!itemCounts.ContainsKey(slot.item.itemName))
            {
                itemCounts[slot.item.itemName] = 0;
            }

            itemCounts[slot.item.itemName] += slot.count;
        }

        foreach (KeyValuePair<string, int> item in itemCounts)
        {
            entries.Add(new AgentInventoryEntryDto
            {
                name = item.Key,
                count = item.Value,
            });
        }

        return entries;
    }

    public static bool TryParseCropType(string rawValue, out TileData.CropType cropType)
    {
        cropType = TileData.CropType.IsEmpty;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        string normalized = rawValue.Trim();

        if (normalized.EndsWith(" seeds", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^6].Trim();
        }
        else if (normalized.EndsWith(" seed", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^5].Trim();
        }
        else if (normalized.EndsWith("씨앗", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^2].Trim();
        }

        normalized = normalized switch
        {
            "당근" => nameof(TileData.CropType.Carrot),
            "체리" => nameof(TileData.CropType.Cherry),
            "없음" => nameof(TileData.CropType.IsEmpty),
            _ => normalized,
        };

        return Enum.TryParse(normalized, true, out cropType);
    }
}
