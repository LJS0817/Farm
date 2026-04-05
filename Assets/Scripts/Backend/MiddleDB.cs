using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

[DisallowMultipleComponent]
// 타일 상태를 중앙에서 관리하는 중간 데이터 계층.
// 게임 로직은 여기의 상태를 수정하고, TileManager가 이 상태를 실제 씬 타일에 반영한다.
public class MiddleDB : MonoBehaviour
{
    [System.Serializable]
    // 타일 하나의 직렬화 가능한 상태 데이터.
    public class TileState
    {
        public int id;
        public Vector2Int coord;
        public TileData.TileType tileType;
        public TileData.CropType cropType;
        public TileData.CropState cropState;
        public bool isFarmable;
        public float growDuration;
        public float maxTime;
    }

    [System.Serializable]
    private class TileStateJson
    {
        public int? id;
        public int? x;
        public int? y;
        public string tileType;
        public string cropType;
        public string cropState;
        public float? growDuration;
    }

    [System.Serializable]
    private class MiddleDbMockJson
    {
        public TileStateJson[] tileStates;
    }
    [SerializeField] private string mockJsonFileName = "MiddleDBMock.json";
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

    // 좌표가 현재 맵 범위 안에 있는지 검사한다.
    public bool IsInBounds(Vector2Int coord)
    {
        return coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;
    }

    // 좌표 기준으로 타일 상태를 조회한다.
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

    // 좌표 기준 타일 상태를 바로 가져오는 편의 함수.
    public TileState GetTileState(Vector2Int coord)
    {
        return TryGetTileState(coord, out TileState state) ? state : null;
    }

    // 타일 ID 기준으로 타일 상태를 조회한다.
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

    // public void CacheTile(TileData tileData)
    // {
    //     if (tileData == null)
    //     {
    //         return;
    //     }

    //     EnsureInitialized();

    //     if (!IsInBounds(tileData.coord))
    //     {
    //         Debug.LogWarning($"Tile coord {tileData.coord} is out of MiddleDB bounds.", this);
    //         return;
    //     }

    //     int index = ToIndex(tileData.coord.x, tileData.coord.y);
    //     TileState state = tileStates[index] ?? CreateDefaultState(tileData.coord.x, tileData.coord.y, index);

    //     state.id = tileData.id;
    //     state.coord = tileData.coord;
    //     state.tileType = tileData.tileType;
    //     state.cropType = tileData.cropType;
    //     state.cropState = tileData.cropState;
    //     state.isFarmable = tileData.isFarmable;
    //     state.growDuration = tileData.GrowDuration;
    //     tileStates[index] = state;
    // }

    // MiddleDB 상태를 실제 TileData 컴포넌트에 복사한다.
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

    // 타일 타입을 변경하면서 해당 타입에 맞는 기본 상태를 함께 정리한다.
    public bool UpdateTileType(Vector2Int coord, TileData.TileType tileType)
    {
        if (!TryGetTileState(coord, out TileState state))
        {
            return false;
        }

        state.tileType = tileType;

        if (tileType == TileData.TileType.Soil)
        {
            state.cropType = TileData.CropType.IsEmpty;
            state.cropState = TileData.CropState.IsEmpty;
            state.growDuration = 0f;
            state.isFarmable = true;
            return true;
        }

        if (tileType == TileData.TileType.Weed || tileType == TileData.TileType.Water)
        {
            state.cropType = TileData.CropType.IsEmpty;
            state.cropState = TileData.CropState.IsEmpty;
            state.growDuration = 0f;
        }

        state.isFarmable = false;

        return true;
    }

    public bool PlantCrop(Vector2Int coord, CropsData cropsData)
    {
        //작물을 심는 함수
        if (!TryGetTileState(coord, out TileState state))
        {
            return false;
        }

        if (!state.isFarmable)
        {
            return false;
        }


        state.cropType = cropsData.crop;
        state.cropState = TileData.CropState.IsGrowing;
        state.isFarmable = false;
        state.maxTime = cropsData.growTime;
        state.growDuration = cropsData.growTime;
        
       // Debug.Log($"coord:{coord}, tileType:{state.tileType}, isFarmable:{state.isFarmable}, cropType:{state.cropType}, cropState:{state.cropState}");


        return true;
    }
    


    //작물 생성 완료
    // 성장 중인 작물을 수확 가능 상태로 바꾼다.
    public bool CompleteCropGrowth(Vector2Int coord)
    {
        if (!TryGetTileState(coord, out TileState state))
        {
            return false;
        }

        if (state.cropState != TileData.CropState.IsGrowing)
        {
            return false;
        }

        state.growDuration = 0f;
        state.cropState = TileData.CropState.IsHarvastable;
        state.isFarmable = false;

        return true;
    }

    // 수확 완료 후 타일을 다시 빈 경작 가능 상태로 되돌린다.
    public bool HarvestCrop(Vector2Int coord)
    {
        if (!TryGetTileState(coord, out TileState state))
        {
            return false;
        }

        if (state.cropState != TileData.CropState.IsHarvastable)
        {
            return false;
        }

        state.cropType = TileData.CropType.IsEmpty;
        state.cropState = TileData.CropState.IsEmpty;
        state.growDuration = 0f;
        state.maxTime = 0f;
        state.isFarmable = true;

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
                    $"  ({state.coord.x},{state.coord.y}) | id:{state.id} | tile:{state.tileType} | crop:{state.cropType} | cropState:{state.cropState} | farmable:{state.isFarmable} | grow:{state.growDuration:0.##}/{state.maxTime:0.##}");
            }
        }

        Debug.Log(builder.ToString(), this);
    }

    // StreamingAssets의 Mock JSON을 읽어 초기 타일 상태를 구성한다.
    private void ApplyMockJson()
    {
        string mockJson = LoadMockJsonText();
        if (string.IsNullOrWhiteSpace(mockJson))
        {
            EnsureInitialized();
            return;
        }

        MiddleDbMockJson parsed;

        try
        {
            parsed = JsonConvert.DeserializeObject<MiddleDbMockJson>(mockJson);
        }
        catch (JsonException exception)
        {
            Debug.LogError($"Failed to parse MiddleDB mock JSON file '{mockJsonFileName}'.\n{exception.Message}", this);
            return;
        }

        if (parsed == null)
        {
            Debug.LogWarning($"MiddleDB mock JSON file '{mockJsonFileName}' did not produce any data.", this);
            return;
        }

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

            if (!TryResolveCoord(tileJson, out Vector2Int coord, out int index))
            {
                Debug.LogWarning("Mock tile entry is missing a valid id/coord.", this);
                continue;
            }

            if (!IsInBounds(coord))
            {
                Debug.LogWarning($"Mock tile coord {coord} is out of bounds.", this);
                continue;
            }

            TileState state = CreateDefaultState(coord.x, coord.y, index);

            state.id = index;
            state.coord = coord;
            string resolvedTileType = ResolveLegacyTileType(tileJson.tileType, tileJson.cropType);
            state.tileType = ParseEnum(resolvedTileType, TileData.TileType.Weed);
            state.cropType = ParseEnum(tileJson.cropType, TileData.CropType.IsEmpty);
            state.cropState = ParseEnum(tileJson.cropState, TileData.CropState.IsEmpty);
            state.growDuration = Mathf.Max(0f, tileJson.growDuration ?? 0f);
            state.isFarmable = ComputeIsFarmable(state.tileType, state.cropType, state.cropState);

            tileStates[index] = state;
        }

        EnsureInitialized();
    }

    private string LoadMockJsonText()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, mockJsonFileName);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"MiddleDB mock JSON file not found at '{filePath}'.", this);
            return null;
        }

        try
        {
            return File.ReadAllText(filePath);
        }
        catch (IOException exception)
        {
            Debug.LogError($"Failed to read MiddleDB mock JSON file '{filePath}'.\n{exception.Message}", this);
            return null;
        }
    }

    private bool TryResolveCoord(TileStateJson tileJson, out Vector2Int coord, out int index)
    {
        if (tileJson.id.HasValue)
        {
            index = tileJson.id.Value;
            if (index < 0 || index >= TileCount)
            {
                coord = default;
                return false;
            }

            coord = ToCoord(index);
            return true;
        }

        if (tileJson.x.HasValue && tileJson.y.HasValue)
        {
            coord = new Vector2Int(tileJson.x.Value, tileJson.y.Value);
            index = ToIndex(coord.x, coord.y);
            return true;
        }

        coord = default;
        index = -1;
        return false;
    }

    private int ToIndex(int x, int y)
    {
        return y * width + x;
    }

    private Vector2Int ToCoord(int index)
    {
        int x = index % width;
        int y = index / width;
        return new Vector2Int(x, y);
    }

    // 빠진 타일 없이 전체 맵 상태 배열을 초기화한다.
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
    // 지정한 좌표의 기본 타일 상태를 생성한다.
    private TileState CreateDefaultState(int x, int y, int index)
    {
        return new TileState
        {
            id = index,
            coord = new Vector2Int(x, y),
            tileType = TileData.TileType.Weed,
            cropType = TileData.CropType.IsEmpty,
            cropState = TileData.CropState.IsEmpty,
            isFarmable = true,
            growDuration = 0f
        };
    }

    // 타일 타입과 작물 상태를 기준으로 현재 심기 가능한지 계산한다.
    private static bool ComputeIsFarmable(TileData.TileType tileType, TileData.CropType cropType, TileData.CropState cropState)
    {
        if (tileType == TileData.TileType.Water)
        {
            return false;
        }

        return cropType == TileData.CropType.IsEmpty
            && cropState == TileData.CropState.IsEmpty;
    }

    private static string ResolveLegacyTileType(string tileType, string cropType)
    {
        string resolved = !string.IsNullOrWhiteSpace(tileType) ? tileType : cropType;
        if (string.Equals(resolved, "Empty", System.StringComparison.OrdinalIgnoreCase))
        {
            return nameof(TileData.TileType.Soil);
        }

        return resolved;
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
    {
        return !string.IsNullOrWhiteSpace(value) && System.Enum.TryParse(value, true, out TEnum parsedValue)
            ? parsedValue
            : fallback;
    }
}
