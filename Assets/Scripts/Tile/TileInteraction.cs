using UnityEngine;

[RequireComponent(typeof(TileData))]
public class TileInteraction : MonoBehaviour
{
    [SerializeField] private TileManager tileManager;

    private TileData tileData;

    private void Awake()
    {
        tileData = GetComponent<TileData>();

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }
    }
    private void OnMouseDown()
    {
        ChangeCurrentTileToSoil();
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
}
