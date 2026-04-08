using Unity.VisualScripting;
using UnityEngine;

// 타일 그리드의 생성, 상태 반영, 성장 진행, 심기/수확을 총괄하는 매니저.
// MiddleDB를 기준 데이터로 사용하고, TileData/TileView를 그 상태에 맞춰 동기화한다.
public class TileManager : MonoBehaviour
{
    [SerializeField] private MiddleDB middleDB;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform tileRoot;
    [SerializeField] private Sprite soilSprite;
    [SerializeField] public Sprite weedSprite;
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

    private void Update()
    {
        if (tiles == null)
        {
            return;
        }

        for (int y = 0; y < tiles.GetLength(1); y++)
        {
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                TileData tile = tiles[x, y];
                if (tile == null || tile.cropState != TileData.CropState.IsGrowing)
                {
                    continue;
                }

                tile.GrowDuration -= Time.deltaTime;

                if (tile.GrowDuration > 0f)
                {
                    continue;
                }

                tile.GrowDuration = 0f;
                CompleteCropGrowth(tile);
            }
        }
    }

    [ContextMenu("Regenerate Grid")]
    public void RegenerateGrid()
    {
        ClearGeneratedTiles();
        GenerateGrid();
        RefreshAllTiles();
    }

    // 씬에 타일이 없을 때 프리팹으로 전체 타일 그리드를 생성한다.
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

                middleDB.ApplyStateToTile(tileData);

                tiles[x, y] = tileData;
                tileViews[x, y] = tileObject.GetComponent<TileView>();
            }
        }
    }

    // 이미 씬에 배치된 TileData들을 배열에 등록하고 MiddleDB 상태를 반영한다.
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

            middleDB.ApplyStateToTile(tile);
        }
    }

    // 좌표로 타일을 찾아 TileData 인스턴스를 반환한다.
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

    // 타일 타입을 변경하고, 변경된 상태를 화면까지 갱신한다.
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

    //작물을 심는 함수 *핵심
    // 지정 좌표에 작물을 심고 MiddleDB, TileData, TileView를 함께 갱신한다.
    public bool PlantCrop(Vector2Int coord, CropsData cropsData)
    {
        if (middleDB == null || !middleDB.PlantCrop(coord, cropsData))
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
    }//

    // 수확 가능한 작물을 수확하고 보상 아이템을 인벤토리에 넣는다.
    public bool HarvestCrop(Vector2Int coord, InventoryManager inventoryManager)
    {
        if (inventoryManager == null)
        {
            Debug.LogWarning("[TileManager] Harvest failed: InventoryManager reference is missing.", this);
            return false;
        }

        if (!TryGetTile(coord, out TileData tile) || tile == null)
        {
            Debug.LogWarning($"[TileManager] Harvest failed: tile not found at {coord}.", this);
            return false;
        }

        if (tile.cropState != TileData.CropState.IsHarvastable || tile.cropType == TileData.CropType.IsEmpty)
        {
            Debug.LogWarning($"[TileManager] Harvest failed: tile {coord} is not harvestable.", this);
            return false;
        }

        if (CropManager.instance == null)
        {
            Debug.LogWarning("[TileManager] Harvest failed: CropManager reference is missing.", this);
            return false;
        }

        CropsData cropData = CropManager.instance.GetCropData(tile.cropType);
        if (cropData == null || cropData.harvestItem == null)
        {
            Debug.LogWarning($"[TileManager] Harvest failed: harvest item is not configured for {tile.cropType}.", this);
            return false;
        }

        int harvestAmount = Mathf.Max(1, cropData.harvestAmount);
        bool added = inventoryManager.AddItem(cropData.harvestItem, harvestAmount);
        if (!added)
        {
            Debug.LogWarning($"[TileManager] Harvest failed: could not add {cropData.harvestItem.itemName} to inventory.", this);
            return false;
        }

        if (middleDB == null || !middleDB.HarvestCrop(coord))
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


    // 전체 타일을 MiddleDB 상태 기준으로 다시 동기화한다.
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

    // 성장 완료 시 타일 상태를 수확 가능 상태로 전환하고 화면을 갱신한다.
    private void CompleteCropGrowth(TileData tile)
    {
        if (tile == null)
        {
            return;
        }

        tile.GrowDuration = 0f;
        tile.cropState = TileData.CropState.IsHarvastable;

        if (middleDB != null)
        {
            // 여기서 middleDB 쪽 완료 상태 갱신 함수 호출
            middleDB.CompleteCropGrowth(tile.coord);

            // middleDB 반영 후 필요하면 다시 tile에도 적용
            middleDB.ApplyStateToTile(tile);
        }

        TileView tileView = tileViews[tile.coord.x, tile.coord.y];
        if (tileView != null)
        {
            tileView.Refresh();
        }

        Debug.Log($"작물 생성 완료: {tile.coord}, crop:{tile.cropType}", tile);
    }

    // 타일 타입에 맞는 바닥 스프라이트를 반환한다.
    public Sprite GetTileSprite(TileData.TileType tileType)
    {
        return tileType switch
        {
            TileData.TileType.Weed => null,
            TileData.TileType.Soil => soilSprite,
            TileData.TileType.Water => waterSprite,
            _ => null
        };
    }
    // 수확 가능 상태의 작물에 표시할 최종 작물 스프라이트를 반환한다.
    public Sprite GetCropSpirte(TileData.CropType crop)
    {
        return crop switch
        {
            TileData.CropType.Carrot => CropManager.instance.cropDatas[0].finalCropSprite,
            TileData.CropType.Cherry => CropManager.instance.cropDatas[1].finalCropSprite,
            TileData.CropType.IsEmpty => null,
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
