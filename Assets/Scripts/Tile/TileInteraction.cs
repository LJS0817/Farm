using UnityEngine;

[RequireComponent(typeof(TileData))]
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
