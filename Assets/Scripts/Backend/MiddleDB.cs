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
        public int variantIndex;
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
    [SerializeField] private bool generateRandomMapOnStart = true;
    [SerializeField] private int worldSeed = 12345;
    [SerializeField] private Vector2Int guaranteedWeedStartCoord = new Vector2Int(7, 4);
    [SerializeField] [Min(0f)] private float weedWeight = 0.7f;
    [SerializeField] [Min(0f)] private float waterWeight = 0.1f;
    [SerializeField] [Min(0f)] private float treeWeight = 0.1f;
    [SerializeField] [Min(0f)] private float rockWeight = 0.1f;
    private int width = 15;
    private int height = 9;
    private TileState[] tileStates = new TileState[0];
    private bool isInitialized;

    public int Width => width;
    public int Height => height;
    public int TileCount => width * height;
    public int WorldSeed => worldSeed;

    private void Awake()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        if (!generateRandomMapOnStart)
        {
            ApplyMockJson();
        }

        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (isInitialized && tileStates != null && tileStates.Length == TileCount)
        {
            return;
        }

        tileStates = BuildInitializedStates();
        isInitialized = true;
    }

    public void ResetToDefaultState()
    {
        isInitialized = false;
        tileStates = new TileState[0];
        EnsureInitialized();
    }

    public void SetWorldSeed(int newWorldSeed)
    {
        worldSeed = newWorldSeed;
    }

    public void SetGuaranteedStartCoord(Vector2Int startCoord)
    {
        guaranteedWeedStartCoord = startCoord;
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

    public void LoadTileStates(TileStateDto[] snapshotTiles)
    {
        EnsureInitialized();

        if (snapshotTiles == null)
        {
            return;
        }

        foreach (TileStateDto tileDto in snapshotTiles)
        {
            if (tileDto == null || !TryGetTileStateById(tileDto.id, out TileState state) || state == null)
            {
                continue;
            }

            state.id = tileDto.id;
            state.coord = new Vector2Int(tileDto.id % width, tileDto.id / width);
           // state.tileType = ParseEnum(tileDto.tileType, TileData.TileType.Soil);
            state.cropType = ParseEnum(tileDto.cropType, TileData.CropType.IsEmpty);
            state.cropState = ParseEnum(tileDto.cropState, TileData.CropState.IsEmpty);
            state.variantIndex = tileDto.variantIndex;
            state.isFarmable = ComputeIsFarmable(state.tileType, state.cropType, state.cropState);

            if (state.cropState != TileData.CropState.IsGrowing)
            {
                state.growDuration = 0f;
                state.maxTime = 0f;
            }
        }
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
        state.variantIndex = CreateVariantIndex(coord, state.id, tileType);

        if (tileType == TileData.TileType.Weed
            || tileType == TileData.TileType.Water
            || tileType == TileData.TileType.Tree
            || tileType == TileData.TileType.Rock)
        {
            state.cropType = TileData.CropType.IsEmpty;
            state.cropState = TileData.CropState.IsEmpty;
            state.growDuration = 0f;
            state.maxTime = 0f;
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

        isInitialized = false;
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
            state.variantIndex = CreateVariantIndex(coord, index, state.tileType);
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
        Vector2Int coord = new Vector2Int(x, y);
        TileData.TileType tileType = GetInitialTileType(coord, index);

        return new TileState
        {
            id = index,
            coord = coord,
            tileType = tileType,
            cropType = TileData.CropType.IsEmpty,
            cropState = TileData.CropState.IsEmpty,
            variantIndex = CreateVariantIndex(coord, index, tileType),
            isFarmable = ComputeIsFarmable(tileType, TileData.CropType.IsEmpty, TileData.CropState.IsEmpty),
            growDuration = 0f
        };
    }

    private TileData.TileType GetInitialTileType(Vector2Int coord, int id)
    {
        if (coord == guaranteedWeedStartCoord)
        {
            return TileData.TileType.Weed;
        }

        float totalWeight = weedWeight + waterWeight + treeWeight + rockWeight;
        if (totalWeight <= 0f)
        {
            return TileData.TileType.Weed;
        }

        float randomValue = CreateTileDistributionValue(coord, id);
        float normalizedWeed = weedWeight / totalWeight;
        float normalizedWater = waterWeight / totalWeight;
        float normalizedTree = treeWeight / totalWeight;

        if (randomValue < normalizedWeed)
        {
            return TileData.TileType.Weed;
        }

        randomValue -= normalizedWeed;
        if (randomValue < normalizedWater)
        {
            return TileData.TileType.Water;
        }

        randomValue -= normalizedWater;
        if (randomValue < normalizedTree)
        {
            return TileData.TileType.Tree;
        }

        return TileData.TileType.Rock;
    }

    private float CreateTileDistributionValue(Vector2Int coord, int id)
    {
        unchecked
        {
            uint hash = (uint)worldSeed;
            hash ^= 0xA511E9B3u;
            hash += (uint)coord.x * 0x9E3779B9u;
            hash ^= (uint)coord.y * 0x85EBCA6Bu;
            hash += (uint)id * 0xC2B2AE35u;
            hash ^= hash >> 15;
            hash *= 0x27D4EB2Du;
            hash ^= hash >> 16;

            return (hash & 0x00FFFFFFu) / 16777216f;
        }
    }

    private int CreateVariantIndex(Vector2Int coord, int id, TileData.TileType tileType)
    {
        unchecked
        {
            uint hash = (uint)worldSeed;
            hash ^= 0x9E3779B9u;
            hash += (uint)coord.x * 0x85EBCA6Bu;
            hash ^= (uint)coord.y * 0xC2B2AE35u;
            hash += (uint)id * 0x27D4EB2Du;
            hash ^= (uint)((int)tileType + 1) * 0x165667B1u;

            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;

            return (int)(hash & 0x7FFFFFFF);
        }
    }

    // 타일 타입과 작물 상태를 기준으로 현재 심기 가능한지 계산한다.
    private static bool ComputeIsFarmable(TileData.TileType tileType, TileData.CropType cropType, TileData.CropState cropState)
    {
        if (tileType == TileData.TileType.Water
            || tileType == TileData.TileType.Tree
            || tileType == TileData.TileType.Rock)
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
            return nameof(TileData.TileType.Water);//원래 soil 수정필요
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
