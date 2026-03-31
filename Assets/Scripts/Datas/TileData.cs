using UnityEngine;

public class TileData : MonoBehaviour
{
    public enum TileType
    {
        Empty,
        Soil
    }

    public enum CropType
    {
        None,
        Carrot
    }

    public int id;//타일 ID
    public Vector2Int coord;
    public TileType tileType;
    public bool isFarmable;
    public CropType cropType;

    public void ApplyState(MiddleDB.TileState state)
    {
        id = state.id;
        coord = state.coord;
        tileType = state.tileType;
        isFarmable = state.isFarmable;
        cropType = state.cropType;
    }
}
