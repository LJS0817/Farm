using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Pathfinding;

// 타일 그리드의 생성, 상태 반영, 성장 진행, 심기/수확을 총괄하는 매니저.
// MiddleDB를 기준 데이터로 사용하고, TileData/TileView를 그 상태에 맞춰 동기화한다.
public class TileManager : MonoBehaviour
{
    [SerializeField] private MiddleDB middleDB;
    [SerializeField] private FarmLevelManager farmLevelManager;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform tileRoot;
    [SerializeField] private Sprite soilSprite;
    [SerializeField] public Sprite[] weedSprites;
    [SerializeField] private Sprite waterSprite;
    [SerializeField] private Sprite[] treeSprites;//나무 이미지들
    [SerializeField] private Sprite[] rocksSprites;//돌 이미지들
    
    [SerializeField] private Vector2 tileSpacing = Vector2.one;
    [SerializeField] private Vector2 topLeftOrigin = new Vector2(-7f, 4f);
    [SerializeField] private bool generateOnStartIfNoSceneTiles = true;
    [SerializeField] private string tileSortingLayerName = "Default";
    [SerializeField] private int lineSortingBaseOrder = 10;

    public TileData[,] tiles;
    public TileView[,] tileViews;

    CustomTileController _customTileContoller;


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

        if (farmLevelManager == null)
        {
            farmLevelManager = FindFirstObjectByType<FarmLevelManager>();
        }

        middleDB.EnsureInitialized();
        tiles = new TileData[middleDB.Width, middleDB.Height];
        tileViews = new TileView[middleDB.Width, middleDB.Height];

        _customTileContoller = GetComponent<CustomTileController>();
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
                ApplyLineSorting(tileObject, y);

                TileData tileData = tileObject.GetComponent<TileData>();
                if (tileData == null)
                {
                    Debug.LogError($"Tile prefab {tilePrefab.name} is missing TileData.", tileObject);
                    continue;
                }

                tileData.id = y * middleDB.Width + x;
                tileData.coord = coord;

                middleDB.ApplyStateToTile(tileData);

                // SpriteRenderer spriteRenderer = tileObject.GetComponent<SpriteRenderer>();
                // if (spriteRenderer != null)
                // {
                //     //spriteRenderer.sortingOrder += y;
                // }

                ApplyTileLayer(tileObject, tileData.tileType);

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
            ApplyLineSorting(tile.gameObject, tile.coord.y);

            middleDB.ApplyStateToTile(tile);
        }
    }

    // 월드 좌표를 그리드 좌표로 변환하고, 그 좌표가 맵 범위 안인지 검사하여 타일 데이터를 반환한다.
    // ======== 추가
    public bool TryGetTileFromWorldPosition(Vector3 worldPosition, out TileData tile)
    {
        tile = null;

        Vector2Int coord = WorldToCellPosition(worldPosition);

        return TryGetTile(coord, out tile);
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

    public bool IsWalkable(Vector2Int coord)
    {
        if (!TryGetTile(coord, out TileData tile) || tile == null)
        {
            return false;
        }

        return tile.tileType != TileData.TileType.Water
            && tile.tileType != TileData.TileType.Tree
            && tile.tileType != TileData.TileType.Rock;
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
        ApplyTileLayer(tile.gameObject, tile.tileType);

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

    // 주변 8칸 중 하나라도 물 타일이면 true를 반환한다.
    public bool HasAdjacentWater(Vector2Int coord)
    {
        if (middleDB == null)
        {
            return false;
        }

        for (int yOffset = -1; yOffset <= 1; yOffset++)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                if (xOffset == 0 && yOffset == 0)
                {
                    continue;
                }

                Vector2Int adjacentCoord = new Vector2Int(coord.x + xOffset, coord.y + yOffset);

                if (!middleDB.IsInBounds(adjacentCoord))
                {
                    continue;
                }

                if (!TryGetTile(adjacentCoord, out TileData adjacentTile) || adjacentTile == null)
                {
                    continue;
                }

                if (adjacentTile.tileType == TileData.TileType.Water)
                {
                    return true;
                }
            }
        }

        return false;
    }

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
        if (HasAdjacentWater(coord))
        {
            harvestAmount *= 2;
        }

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

        if (farmLevelManager != null)
        {
            int gainedExp = Mathf.Max(0, cropData.harvestItem.sellPrice * harvestAmount / 10);
            farmLevelManager.GainFarmExp(gainedExp);
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
                ApplyTileLayer(tile.gameObject, tile.tileType);

                TileView tileView = tileViews[x, y];
                if (tileView != null)
                {
                    tileView.Refresh(_customTileContoller.GetTilePrefab);
                }
            }
        }
    }

    public void RefreshNavigationGraph()
    {
        if (AstarPath.active == null)
        {
            return;
        }

        AstarPath.active.Scan();
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

    // 타일 하나의 상태를 기준으로 화면에 표시할 스프라이트를 반환한다.
    public Sprite GetTileSprite(TileData tileData)
    {
        if (tileData == null)
        {
            return null;
        }

        return tileData.tileType switch
        {
            TileData.TileType.Weed => GetWeedSprite(tileData.variantIndex),
            TileData.TileType.Tree => GetTreeSprite(tileData.variantIndex),
            TileData.TileType.Rock => GetRockSprite(tileData.variantIndex),
            _ => GetTileSprite(tileData.tileType)
        };
    }

    // 타일 타입에 맞는 기본 스프라이트를 반환한다.
    public Sprite GetTileSprite(TileData.TileType tileType)
    {
        return tileType switch
        {
            TileData.TileType.Water => waterSprite,
            _ => null
        };
    }

    public Sprite GetWeedSprite(int variantIndex)
    {
        return GetVariantSprite(weedSprites, variantIndex);
    }

    public Sprite GetTreeSprite(int variantIndex)
    {
        return GetVariantSprite(treeSprites, variantIndex);
    }

    public Sprite GetRockSprite(int variantIndex)
    {
        return GetVariantSprite(rocksSprites, variantIndex);
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

    private Sprite GetVariantSprite(Sprite[] sprites, int variantIndex)
    {
        if (sprites == null || sprites.Length == 0)
        {
            return null;
        }

        int safeIndex = Mathf.Abs(variantIndex);
        int spriteIndex = safeIndex % sprites.Length;
        return sprites[spriteIndex];
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

    // ======== 추가
    private Vector2Int WorldToCellPosition(Vector3 worldPosition)
    {
        return new Vector2Int(Mathf.RoundToInt((worldPosition.x - topLeftOrigin.x) / tileSpacing.x), 
            Mathf.RoundToInt((topLeftOrigin.y - worldPosition.y) / tileSpacing.y));
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

    private void ApplyLineSorting(GameObject tileObject, int lineIndex)
    {
        if (tileObject == null)
        {
            return;
        }

        SpriteRenderer[] renderers = tileObject.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        int targetBaseOrder = lineSortingBaseOrder + lineIndex;
        int minOriginalOrder = renderers[0].sortingOrder;

        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i].sortingOrder < minOriginalOrder)
            {
                minOriginalOrder = renderers[i].sortingOrder;
            }
        }

        foreach (SpriteRenderer renderer in renderers)
        {
            int relativeOrder = renderer.sortingOrder - minOriginalOrder;
            renderer.sortingLayerName = tileSortingLayerName;
            renderer.sortingOrder = targetBaseOrder + relativeOrder;
        }
    }

    private void ApplyTileLayer(GameObject tileObject, TileData.TileType tileType)
    {
        if (tileObject == null)
        {
            return;
        }

        bool isBlockedTile = tileType == TileData.TileType.Water
            || tileType == TileData.TileType.Tree
            || tileType == TileData.TileType.Rock;

        tileObject.layer = isBlockedTile ? 6 : 0;
    }

    // ======== 추가
    /// <summary>
    /// 비어있지 않은(작물이 있는) 모든 타일 데이터를 리스트로 반환합니다.
    /// </summary>
    public List<TileData> GetNonEmptyTiles()
    {
        List<TileData> nonEmptyTiles = new List<TileData>();

        if (tiles == null) return nonEmptyTiles;

        int width = tiles.GetLength(0);
        int height = tiles.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TileData tile = tiles[x, y];
                if (tile != null && tile.cropType != TileData.CropType.IsEmpty)
                {
                    nonEmptyTiles.Add(tile);
                }
            }
        }

        return nonEmptyTiles;
    }
}
