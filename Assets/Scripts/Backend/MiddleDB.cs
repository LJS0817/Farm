using UnityEngine;
using System.Text;

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
        public TileData.CropType cropType;
    }

    [SerializeField] private int width = 14;
    [SerializeField] private int height = 8;
    [SerializeField] private TileState[] tileStates = new TileState[0];

    public int Width => width;
    public int Height => height;
    public int TileCount => width * height;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        int expectedCount = TileCount;
        if (tileStates == null || tileStates.Length != expectedCount)
        {
            tileStates = new TileState[expectedCount];
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = ToIndex(x, y);
                tileStates[index] ??= CreateDefaultState(x, y, index);
            }
        }
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
        state.cropType = tileData.cropType;

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

    public bool UpdateTileCrop(Vector2Int coord, TileData.CropType cropType)
    {
        if (!TryGetTileState(coord, out TileState state))
        {
            return false;
        }

        state.cropType = cropType;
        return true;
    }

    public bool UpdateTileType(Vector2Int coord, TileData.TileType tileType)
    {
        if (!TryGetTileState(coord, out TileState state))
        {
            return false;
        }

        state.tileType = tileType;

        if (tileType == TileData.TileType.Soil)
        {
            state.isFarmable = true;
        }

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
                    $"  ({state.coord.x},{state.coord.y}) | id:{state.id} | tile:{state.tileType} | farmable:{state.isFarmable} | crop:{state.cropType}");
            }
        }

        Debug.Log(builder.ToString(), this);
    }

    private int ToIndex(int x, int y)
    {
        return y * width + x;
    }

    private TileState CreateDefaultState(int x, int y, int index)
    {
        return new TileState
        {
            id = index,
            coord = new Vector2Int(x, y),
            tileType = TileData.TileType.Empty,
            isFarmable = false,
            cropType = TileData.CropType.None
        };
    }
}
