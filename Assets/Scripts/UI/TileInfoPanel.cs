using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TileInfoPanel : MonoBehaviour
{
    private const float OpenY = 0f;
    private const float ClosedY = -2000f;

    public TMP_Text Text_TileName;
    public TMP_Text Text_Location;
    public TMP_Text Text_TileState;
    public TMP_Text Text_Time;
    public Slider slider;
    public Image tileImage;
    public Button harvestButton;
    public int cacheID;
    public bool IsOpen { get; private set; }

    [SerializeField] private MiddleDB middleDB;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Sprite weedPanelSprite;

    private void Awake()
    {
        if (middleDB == null)
        {
            middleDB = FindFirstObjectByType<MiddleDB>();
        }

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }
    }

    private void Start()
    {
        ClearPanel();
        ClosePanel();
        SetHarvestButton(false);
    }
    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        RefreshGrowthUI();
    }


    public void OnClickTileInfoPanel(int tileID)
    {
        if (middleDB == null)
        {
            middleDB = FindFirstObjectByType<MiddleDB>();
        }

        if (middleDB == null)
        {
            Debug.LogError("MiddleDB reference is missing.", this);
            SetTexts("Tile Info", "Unknown", "데이터를 찾을 수 없습니다.", "-");
            SetTileImage(null);
            SetSliderValue(0f);
            return;
        }

        if (!middleDB.TryGetTileStateById(tileID, out MiddleDB.TileState state))
        {
            Debug.LogWarning($"Tile info not found. tileID: {tileID}", this);
            SetTexts($"Tile #{tileID}", "Unknown", "존재하지 않는 타일입니다.", "-");
            SetTileImage(null);
            SetSliderValue(0f);
            return;
        }
        cacheID = tileID;

        string tileName = GetTileDisplayName(state);
        string location = $" 위치 : ({state.coord.x}, {state.coord.y})";
        string tileState = $"경작 가능:{(state.isFarmable ? "예" : "아니오")} | 작물:{GetCropDisplayName(state.cropType)} | 상태:{GetCropStateDisplayName(state.cropState)}";
        string timeText = state.cropState == TileData.CropState.IsGrowing
            ? $"성장 남은 시간 : {state.growDuration:0.0}"
            : state.cropState == TileData.CropState.IsHarvastable
                ? "수확 가능합니다."
                : "성장 중인 작물이 없습니다.";

        SetTexts(tileName, location, tileState, timeText);
        SetTileImage(GetTileSprite(state.tileType));
        SetSliderValue(state.cropState == TileData.CropState.IsGrowing ? 0.1f : state.cropState == TileData.CropState.IsHarvastable ? 1f : 0f);
        SetHarvestButton(state.cropState == TileData.CropState.IsHarvastable);
    }
    public void PlantSelectedCrop(int cropId)
    {
        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }

        if (middleDB == null)
        {
            middleDB = FindFirstObjectByType<MiddleDB>();
        }

        if (tileManager == null || middleDB == null)
        {
            Debug.LogError("Required reference is missing.", this);
            return;
        }

        if (!middleDB.TryGetTileStateById(cacheID, out MiddleDB.TileState state))
        {
            Debug.LogWarning($"Selected tile not found. tileID: {cacheID}", this);
            return;
        }
        CropsData cropsData = CropManager.instance.GetCropData(cropId);
        
        bool success = tileManager.PlantCrop(state.coord, cropsData);

        if (!success)
        {
            Debug.LogWarning($"Failed to plant crop on tile {state.coord}.", this);
            return;
        }

        OnClickTileInfoPanel(cacheID);
    }
    private void RefreshGrowthUI()
    {
        if (!IsOpen)
        {
            return;
        }

        if (middleDB == null)
        {
            middleDB = FindFirstObjectByType<MiddleDB>();
        }

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (middleDB == null || tileManager == null)
        {
            return;
        }

        if (!middleDB.TryGetTileStateById(cacheID, out MiddleDB.TileState state))
        {
            SetSliderValue(0f);
            SetHarvestButton(false);

            if (Text_Time != null)
            {
                Text_Time.text = "-";
            }

            return;
        }

        if (!tileManager.TryGetTile(state.coord, out TileData tile) || tile == null)
        {
            SetSliderValue(0f);
            SetHarvestButton(false);

            if (Text_Time != null)
            {
                Text_Time.text = "-";
            }

            return;
        }

        if (tile.cropState == TileData.CropState.IsHarvastable)
        {
            SetSliderValue(1f);
            SetHarvestButton(true);

            if (Text_TileState != null)
            {
                Text_TileState.text = $"경작 가능:{(tile.isFarmable ? "예" : "아니오")} | 작물:{GetCropDisplayName(tile.cropType)} | 상태:{GetCropStateDisplayName(tile.cropState)}";
            }

            if (Text_Time != null)
            {
                Text_Time.text = "수확 가능합니다.";
            }

            return;
        }

        if (tile.cropState != TileData.CropState.IsGrowing || tile.cropType == TileData.CropType.IsEmpty)
        {
            SetSliderValue(0f);
            SetHarvestButton(false);

            if (Text_Time != null)
            {
                Text_Time.text = "성장 중인 작물이 없습니다.";
            }

            return;
        }

        float progress = 1f - (tile.GrowDuration / tile.maxTime);
        SetSliderValue(progress);
        SetHarvestButton(false);

        if (Text_TileState != null)
        {
            Text_TileState.text = $"경작 가능:{(tile.isFarmable ? "예" : "아니오")} | 작물:{GetCropDisplayName(tile.cropType)} | 상태:{GetCropStateDisplayName(tile.cropState)}";
        }

        if (Text_Time != null)
        {
            Text_Time.text = $"성장 남은 시간 : {tile.GrowDuration:0.0}";
        }
    }






    public void ClearPanel()
    {
        SetTexts("Tile Info", "-", "선택된 타일이 없습니다.", "-");
        SetTileImage(null);
        SetSliderValue(0f);
        SetHarvestButton(false);
    }

    public void OnClickHarvestButton()
    {
        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }

        if (middleDB == null)
        {
            middleDB = FindFirstObjectByType<MiddleDB>();
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (tileManager == null || middleDB == null || inventoryManager == null)
        {
            Debug.LogWarning("[TileInfoPanel] Harvest failed: required reference is missing.", this);
            return;
        }

        if (!middleDB.TryGetTileStateById(cacheID, out MiddleDB.TileState state))
        {
            Debug.LogWarning($"[TileInfoPanel] Harvest failed: tile not found. tileID: {cacheID}", this);
            return;
        }

        bool success = tileManager.HarvestCrop(state.coord, inventoryManager);
        if (!success)
        {
            Debug.LogWarning($"[TileInfoPanel] Harvest failed on tile {state.coord}.", this);
            return;
        }

        OnClickTileInfoPanel(cacheID);
        inventoryManager.LogInventory();
    }

    public void OpenPanel()
    {
        IsOpen = true;
        SetPanelY(OpenY);
    }

    public void ClosePanel()
    {
        IsOpen = false;
        SetPanelY(ClosedY);
    }

    private string GetTileDisplayName(MiddleDB.TileState state)
    {
        return state.tileType switch
        {
            TileData.TileType.Weed => $"타일명 : 잔디",
            TileData.TileType.Soil => $"타일명 : 흙",
            TileData.TileType.Water => $"타일명 : 물",
            _ => $"Tile #{state.id}"
        };
    }

    private string GetCropDisplayName(TileData.CropType cropType)
    {
        return cropType switch
        {
            TileData.CropType.IsEmpty => "없음",
            TileData.CropType.Carrot => "당근",
            TileData.CropType.Cherry => "체리",
            _ => cropType.ToString()
        };
    }

    private string GetCropStateDisplayName(TileData.CropState cropState)
    {
        return cropState switch
        {
            TileData.CropState.IsEmpty => "비어 있음",
            TileData.CropState.IsGrowing => "성장 중",
            TileData.CropState.IsHarvastable => "수확 가능",
            _ => cropState.ToString()
        };
    }

    private void SetTexts(string tileName, string location, string tileState, string timeText)
    {
        if (Text_TileName != null)
        {
            Text_TileName.text = tileName;
        }

        if (Text_Location != null)
        {
            Text_Location.text = location;
        }

        if (Text_TileState != null)
        {
            Text_TileState.text = tileState;
        }

        if (Text_Time != null)
        {
            Text_Time.text = timeText;
        }
    }
        private Sprite GetTileSprite(TileData.TileType tileType)
    {
        if (tileType == TileData.TileType.Weed)
        {
            return weedPanelSprite;
        }

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }

        return tileManager != null ? tileManager.GetTileSprite(tileType) : null;
    }

    private void SetTileImage(Sprite sprite)
    {
        if (tileImage == null)
        {
            return;
        }

        tileImage.sprite = sprite;
        tileImage.enabled = sprite != null;
    }

    private void SetSliderValue(float value)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(value);
    }

    private void SetHarvestButton(bool canHarvest)
    {
        if (harvestButton == null)
        {
            return;
        }

        harvestButton.gameObject.SetActive(canHarvest);
        harvestButton.interactable = canHarvest;
    }

    private void SetPanelY(float y)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        Vector2 anchoredPosition = rectTransform.anchoredPosition;
        anchoredPosition.y = y;
        rectTransform.anchoredPosition = anchoredPosition;
    }
}
