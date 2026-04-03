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
    public bool IsOpen { get; private set; }

    [SerializeField] private MiddleDB middleDB;
    [SerializeField] private TileManager tileManager;

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
    }

    private void Start()
    {
        ClearPanel();
        ClosePanel();
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

        string tileName = GetTileDisplayName(state);
        string location = $" 위치 : ({state.coord.x}, {state.coord.y})";
        string tileState = $"경작 가능:{(state.isFarmable ? "예" : "아니오")}";
       // string timeText = state.cr == TileData.CropType.Carrot ? "성장 시간 시스템 연결 필요" : "심어진 작물이 없습니다.";

      //  SetTexts(tileName, location, tileState, timeText);
        SetTileImage(GetTileSprite(state.tileType));
     //   SetSliderValue(state.tileType == TileData.TileType.Carrot ? 0.1f : 0f);
    }

    public void ClearPanel()
    {
        SetTexts("Tile Info", "-", "선택된 타일이 없습니다.", "-");
        SetTileImage(null);
        SetSliderValue(0f);
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
            TileData.TileType.Water => $"타일명 : 물",
            _ => $"Tile #{state.id}"
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
