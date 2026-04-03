using UnityEngine;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

[DisallowMultipleComponent]
public class MiddleDB : MonoBehaviour
{
    [System.Serializable]
    public class TileState
    {
        public int id;
        public Vector2Int coord;
        public TileData.TileType tileType;
        public bool isFarmable;
    }

    [System.Serializable]
    private class TileStateJson
    {
        public int id;
        public int x;
        public int y;
        public string tileType;
        public bool isFarmable;
        public string cropType;
    }

    [System.Serializable]
    private class MiddleDbMockJson
    {
        public int width;
        public int height;
        public TileStateJson[] tileStates;
    }

    private const string MockJson = @"{
  ""width"": 15,
  ""height"": 9,
  ""tileStates"": [
    { ""x"": 0, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 1, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 2, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 3, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 4, ""y"": 0, ""tileType"": ""Water"", ""isFarmable"": false },
    { ""x"": 5, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 6, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 7, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 8, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 9, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 10, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 11, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 12, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 13, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 14, ""y"": 0, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 0, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 1, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 2, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 3, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 4, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 5, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 6, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 7, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 8, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 9, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 10, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 11, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 12, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 13, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 14, ""y"": 1, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 0, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 1, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 2, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 3, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 4, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 5, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 6, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 7, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 8, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 9, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 10, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 11, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 12, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 13, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 14, ""y"": 2, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 0, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 1, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 2, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 3, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 4, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 5, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 6, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 7, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 8, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 9, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 10, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 11, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 12, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 13, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 14, ""y"": 3, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 0, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 1, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 2, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 3, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 4, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 5, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 6, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 7, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 8, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 9, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 10, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 11, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 12, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 13, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 14, ""y"": 4, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 0, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 1, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 2, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 3, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 4, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 5, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 6, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 7, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 8, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 9, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 10, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 11, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 12, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 13, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 14, ""y"": 5, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 0, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 1, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 2, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 3, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 4, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 5, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 6, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 7, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 8, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 9, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 10, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 11, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 12, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 13, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 14, ""y"": 6, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 0, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 1, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 2, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 3, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 4, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 5, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 6, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 7, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 8, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 9, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 10, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 11, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 12, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 13, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 14, ""y"": 7, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 0, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 1, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 2, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 3, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 4, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 5, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 6, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 7, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 8, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 9, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 10, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 11, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 12, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 13, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true },
    { ""x"": 14, ""y"": 8, ""tileType"": ""Empty"", ""isFarmable"": true }
  ]
}";
    private int width = 15;
    private int height = 9;
    private TileState[] tileStates = new TileState[0];

    public int Width => width;
    public int Height => height;
    public int TileCount => width * height;

    private void Awake()
    {
        ApplyMockJson();
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        tileStates = BuildInitializedStates();
    }

    public bool IsInBounds(Vector2Int coord)
    {
        return coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;
    }

    public bool TryGetTileState(Vector2Int coord, out TileState state)
    {
        EnsureInitialized();

        if (!IsInBounds(coord))
        {
            state = null;
            return false;
        }

        state = tileStates[ToIndex(coord.x, coord.y)];
        return state != null;
    }

    public TileState GetTileState(Vector2Int coord)
    {
        return TryGetTileState(coord, out TileState state) ? state : null;
    }

    public bool TryGetTileStateById(int tileId, out TileState state)
    {
        EnsureInitialized();

        if (tileId < 0 || tileId >= TileCount)
        {
            state = null;
            return false;
        }

        state = tileStates[tileId];
        return state != null;
    }

    public void CacheTile(TileData tileData)
    {
        if (tileData == null)
        {
            return;
        }

        EnsureInitialized();

        if (!IsInBounds(tileData.coord))
        {
            Debug.LogWarning($"Tile coord {tileData.coord} is out of MiddleDB bounds.", this);
            return;
        }

        int index = ToIndex(tileData.coord.x, tileData.coord.y);
        TileState state = tileStates[index] ?? CreateDefaultState(tileData.coord.x, tileData.coord.y, index);

        state.id = tileData.id;
        state.coord = tileData.coord;
        state.tileType = tileData.tileType;
        state.isFarmable = tileData.isFarmable;
        tileStates[index] = state;
    }

    public bool ApplyStateToTile(TileData tileData)
    {
        if (tileData == null)
        {
            return false;
        }

        if (!TryGetTileState(tileData.coord, out TileState state))
        {
            return false;
        }

        tileData.ApplyState(state);
        return true;
    }

    public bool UpdateTileType(Vector2Int coord, TileData.TileType tileType)
    {
        if (!TryGetTileState(coord, out TileState state))
        {
            return false;
        }

        state.tileType = tileType;

        if (tileType == TileData.TileType.Weed)
        {
            state.isFarmable = true;
            return true;
        }

        state.isFarmable = false;

        return true;
    }

    [ContextMenu("Log All Tile States")]
    public void LogAllTileStates()
    {
        EnsureInitialized();

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"[MiddleDB] Tile States ({width}x{height}, total: {TileCount})");

        for (int y = 0; y < height; y++)
        {
            builder.AppendLine($"Row {y}");

            for (int x = 0; x < width; x++)
            {
                TileState state = tileStates[ToIndex(x, y)];
                if (state == null)
                {
                    builder.AppendLine($"  ({x},{y}) <null>");
                    continue;
                }

                builder.AppendLine(
                    $"  ({state.coord.x},{state.coord.y}) | id:{state.id} | tile:{state.tileType} | farmable:{state.isFarmable}");
            }
        }

        Debug.Log(builder.ToString(), this);
    }

    private void ApplyMockJson()
    {
        MiddleDbMockJson parsed;

        try
        {
            parsed = JsonConvert.DeserializeObject<MiddleDbMockJson>(MockJson);
        }
        catch (JsonException exception)
        {
            Debug.LogError($"Failed to parse MiddleDB MockJson.\n{exception.Message}", this);
            return;
        }

        if (parsed == null)
        {
            Debug.LogWarning("MiddleDB MockJson did not produce any data.", this);
            return;
        }

        width = Mathf.Max(1, parsed.width);
        height = Mathf.Max(1, parsed.height);
        tileStates = new TileState[TileCount];

        if (parsed.tileStates == null)
        {
            EnsureInitialized();
            return;
        }

        foreach (TileStateJson tileJson in parsed.tileStates)
        {
            if (tileJson == null)
            {
                continue;
            }

            Vector2Int coord = new Vector2Int(tileJson.x, tileJson.y);
            if (!IsInBounds(coord))
            {
                Debug.LogWarning($"Mock tile coord {coord} is out of bounds.", this);
                continue;
            }

            int index = ToIndex(coord.x, coord.y);
            TileState state = CreateDefaultState(coord.x, coord.y, index);

            state.id = index;
            state.coord = coord;
            string resolvedTileType = !string.IsNullOrWhiteSpace(tileJson.tileType) ? tileJson.tileType : tileJson.cropType;
            state.tileType = ParseEnum(resolvedTileType, TileData.TileType.Weed);
            state.isFarmable = tileJson.isFarmable;

            tileStates[index] = state;
        }

        EnsureInitialized();
    }

    private int ToIndex(int x, int y)
    {
        return y * width + x;
    }

    private TileState[] BuildInitializedStates()
    {
        TileState[] initializedStates = new TileState[TileCount];
        Dictionary<Vector2Int, TileState> existingStatesByCoord = new Dictionary<Vector2Int, TileState>();

        if (tileStates != null)
        {
            foreach (TileState existingState in tileStates)
            {
                if (existingState == null || !IsInBounds(existingState.coord))
                {
                    continue;
                }

                existingStatesByCoord[existingState.coord] = existingState;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                int index = ToIndex(x, y);

                if (existingStatesByCoord.TryGetValue(coord, out TileState existingState))
                {
                    existingState.id = index;
                    existingState.coord = coord;
                    initializedStates[index] = existingState;
                    continue;
                }

                initializedStates[index] = CreateDefaultState(x, y, index);
            }
        }

        return initializedStates;
    }
    private TileState CreateDefaultState(int x, int y, int index)
    {
        return new TileState
        {
            id = index,
            coord = new Vector2Int(x, y),
            tileType = TileData.TileType.Weed,
            isFarmable = false
        };
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
    {
        return !string.IsNullOrWhiteSpace(value) && System.Enum.TryParse(value, true, out TEnum parsedValue)
            ? parsedValue
            : fallback;
    }
}
