using UnityEngine;

[RequireComponent(typeof(TileData))]
// 개별 타일 오브젝트에 붙어 있는 클릭 진입점.
// 타일 정보 패널을 열고, 필요하면 클릭 시 타일 타입을 Soil로 변경한다.
public class TileInteraction : MonoBehaviour
{
    [SerializeField] private TileManager tileManager;
    [SerializeField] private TileInfoPanel tileInfoPanel;
    [SerializeField] private bool changeTileToSoilOnClick;

    private TileData tileData;

    private void Awake()
    {
        tileData = GetComponent<TileData>();

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }

        if (tileInfoPanel == null)
        {
            tileInfoPanel = FindFirstObjectByType<TileInfoPanel>();
        }
    }

    // 현재 타일을 Soil로 바꾸는 함수.
    // 경작 가능한 땅으로 전환해야 하는 흐름에서 사용할 수 있다.
    public void ChangeCurrentTileToSoil()
    {
        if (tileData == null)
        {
            Debug.LogError("TileData reference is missing.", this);
            return;
        }

        if (tileManager == null)
        {
            Debug.LogError("TileManager reference is missing.", this);
            return;
        }

        bool success = tileManager.SetTileType(tileData.coord, TileData.TileType.Soil);
        if (!success)
        {
            Debug.LogWarning($"Failed to change tile {tileData.coord} to Soil.", this);
        }
        Debug.Log("타일이 변경되었음");
    }

    // 선택한 타일의 정보를 UI 패널에 표시한다.
    public void OpenTileInfoPanel()
    {
        if (tileData == null)
        {
            Debug.LogError("TileData reference is missing.", this);
            return;
        }

        if (tileInfoPanel == null)
        {
            tileInfoPanel = FindFirstObjectByType<TileInfoPanel>();
        }

        if (tileInfoPanel == null)
        {
            Debug.LogWarning("TileInfoPanel reference is missing.", this);
            return;
        }

        tileInfoPanel.OpenPanel();
        tileInfoPanel.OnClickTileInfoPanel(tileData.id);
    }

    // 타일 클릭 시 호출되는 메인 엔트리 함수.
    // 패널을 열고, 옵션에 따라 타일을 Soil로 변경한다.
    public void HandleClick()
    {
        if (tileInfoPanel != null && tileInfoPanel.IsOpen)
        {
            return;
        }

        OpenTileInfoPanel();

        if (changeTileToSoilOnClick)
        {
            ChangeCurrentTileToSoil();
        }
    }
}
