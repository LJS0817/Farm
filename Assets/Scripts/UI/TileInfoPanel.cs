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
    public bool IsOpen { get; private set; }

    [SerializeField] private MiddleDB middleDB;

    private void Awake()
    {
        if (middleDB == null)
        {
            middleDB = FindFirstObjectByType<MiddleDB>();
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
            SetSliderValue(0f);
            return;
        }

        if (!middleDB.TryGetTileStateById(tileID, out MiddleDB.TileState state))
        {
            Debug.LogWarning($"Tile info not found. tileID: {tileID}", this);
            SetTexts($"Tile #{tileID}", "Unknown", "존재하지 않는 타일입니다.", "-");
            SetSliderValue(0f);
            return;
        }

        string tileName = GetTileDisplayName(state);
        string location = $"({state.coord.x}, {state.coord.y})";
        string tileState = $"타일:{state.tileType} / 작물:{state.cropType} / 경작 가능:{(state.isFarmable ? "예" : "아니오")}";
        string timeText = state.cropType == TileData.CropType.None ? "심어진 작물이 없습니다." : "성장 시간 시스템 연결 필요";

        SetTexts(tileName, location, tileState, timeText);
        SetSliderValue(state.cropType == TileData.CropType.None ? 0f : 0.1f);
    }

    public void ClearPanel()
    {
        SetTexts("Tile Info", "-", "선택된 타일이 없습니다.", "-");
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
        if (state.cropType != TileData.CropType.None)
        {
            return state.cropType.ToString();
        }

        return state.tileType switch
        {
            TileData.TileType.Empty => $"Empty Tile #{state.id}",
            TileData.TileType.Soil => $"Soil Tile #{state.id}",
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
