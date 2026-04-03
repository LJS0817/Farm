using UnityEngine;

public class TileManager : MonoBehaviour
{
    [SerializeField] private MiddleDB middleDB;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform tileRoot;
    [SerializeField] private Sprite soilSprite;
    [SerializeField] private Sprite carrotSprite;
    [SerializeField] private Sprite weedSprite;
    [SerializeField] private Sprite waterSprite;
    [SerializeField] private Vector2 tileSpacing = Vector2.one;
    [SerializeField] private Vector2 topLeftOrigin = new Vector2(-7f, 4f);
    [SerializeField] private bool generateOnStartIfNoSceneTiles = true;

    public TileData[,] tiles;
    public TileView[,] tileViews;


    private void Awake()
    {
        if (middleDB == null)
        {
            middleDB = FindFirstObjectByType<MiddleDB>();
        }

        if (middleDB == null)
        {
            Debug.LogError("MiddleDB reference is missing.", this);
            return;
        }

        middleDB.EnsureInitialized();
        tiles = new TileData[middleDB.Width, middleDB.Height];
        tileViews = new TileView[middleDB.Width, middleDB.Height];
    }

    private void Start()
    {
        if (HasSceneTiles())
        {
            RegisterSceneTiles();
        }
        else if (generateOnStartIfNoSceneTiles && tilePrefab != null)
        {
            GenerateGrid();
        }

        RefreshAllTiles();
    }

    [ContextMenu("Regenerate Grid")]
    public void RegenerateGrid()
    {
        ClearGeneratedTiles();
        GenerateGrid();
        RefreshAllTiles();
    }

    public void GenerateGrid()
    {
        if (middleDB == null)
        {
            Debug.LogError("Cannot generate grid without MiddleDB.", this);
            return;
        }

        if (tilePrefab == null)
        {
            Debug.LogError("Tile prefab reference is missing.", this);
            return;
        }

        ClearTileArrays();

        Transform parent = tileRoot != null ? tileRoot : transform;
        for (int y = 0; y < middleDB.Height; y++)
        {
            Transform lineParent = CreateLineParent(parent, y);
            for (int x = 0; x < middleDB.Width; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                GameObject tileObject = Instantiate(tilePrefab, GetTilePosition(x, y), Quaternion.identity, lineParent);
                tileObject.name = $"({x},{y})";

                TileData tileData = tileObject.GetComponent<TileData>();
                if (tileData == null)
                {
                    Debug.LogError($"Tile prefab {tilePrefab.name} is missing TileData.", tileObject);
                    continue;
                }

                tileData.id = y * middleDB.Width + x;
                tileData.coord = coord;

                if (middleDB.TryGetTileState(coord, out MiddleDB.TileState state))
                {
                    tileData.ApplyState(state);
                }
                else
                {
                    middleDB.CacheTile(tileData);
                }

                tiles[x, y] = tileData;
                tileViews[x, y] = tileObject.GetComponent<TileView>();
            }
        }
    }

    public void RegisterSceneTiles()
    {
        if (middleDB == null)
        {
            return;
        }

        TileData[] foundTiles = FindObjectsByType<TileData>(FindObjectsSortMode.None);
        foreach (TileData tile in foundTiles)
        {
            if (!middleDB.IsInBounds(tile.coord))
            {
                Debug.LogWarning($"Tile {tile.name} has out of bounds coord {tile.coord}.", tile);
                continue;
            }

            tiles[tile.coord.x, tile.coord.y] = tile;
            tileViews[tile.coord.x, tile.coord.y] = tile.GetComponent<TileView>();

            if (middleDB.TryGetTileState(tile.coord, out MiddleDB.TileState state))
            {
                tile.ApplyState(state);
            }
            else
            {
                middleDB.CacheTile(tile);
            }
        }
    }

    public bool TryGetTile(Vector2Int coord, out TileData tile)
    {
        tile = null;

        if (middleDB == null || !middleDB.IsInBounds(coord))
        {
            return false;
        }

        tile = tiles[coord.x, coord.y];
        return tile != null;
    }

    public bool SetTileType(Vector2Int coord, TileData.TileType tileType)
    {
        if (middleDB == null || !middleDB.UpdateTileType(coord, tileType))
        {
            return false;
        }

        if (!TryGetTile(coord, out TileData tile))
        {
            return false;
        }

        middleDB.ApplyStateToTile(tile);

        TileView tileView = tileViews[coord.x, coord.y];
        if (tileView != null)
        {
            tileView.Refresh();
        }

        return true;
    }

    public void RefreshAllTiles()
    {
        if (middleDB == null)
        {
            return;
        }

        for (int y = 0; y < middleDB.Height; y++)
        {
            for (int x = 0; x < middleDB.Width; x++)
            {
                TileData tile = tiles[x, y];
                if (tile == null)
                {
                    continue;
                }

                middleDB.ApplyStateToTile(tile);

                TileView tileView = tileViews[x, y];
                if (tileView != null)
                {
                    tileView.Refresh();
                }
            }
        }
    }

    public Sprite GetTileSprite(TileData.TileType tileType)
    {
        return tileType switch
        {
            TileData.TileType.Weed => weedSprite,
            TileData.TileType.Water => waterSprite,
            _ => null
        };
    }

    private bool HasSceneTiles()
    {
        TileData[] foundTiles = FindObjectsByType<TileData>(FindObjectsSortMode.None);
        return foundTiles.Length > 0;
    }

    private void ClearTileArrays()
    {
        for (int y = 0; y < middleDB.Height; y++)
        {
            for (int x = 0; x < middleDB.Width; x++)
            {
                tiles[x, y] = null;
                tileViews[x, y] = null;
            }
        }
    }

    private void ClearGeneratedTiles()
    {
        Transform parent = tileRoot != null ? tileRoot : transform;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        ClearTileArrays();
    }

    private Vector3 GetTilePosition(int x, int y)
    {
        return new Vector3(
            topLeftOrigin.x + (x * tileSpacing.x),
            topLeftOrigin.y - (y * tileSpacing.y),
            0f);
    }

    private Transform CreateLineParent(Transform root, int lineIndex)
    {
        GameObject lineObject = new GameObject($"{lineIndex}line");
        lineObject.transform.SetParent(root);
        lineObject.transform.position = Vector3.zero;
        lineObject.transform.localRotation = Quaternion.identity;
        lineObject.transform.localScale = Vector3.one;
        return lineObject.transform;
    }
}
