using UnityEngine;

public class TileData : MonoBehaviour
{
    public enum TileType
    {
        Weed,
        Water,
    }
    public enum CropType
    {
        IsEmpty,//작물이 없을 경우
        Carrot,
        Onion
    }
    public enum CropState
    {
       IsEmpty,//작물이 없을 경우
       IsGrowing,//작물이 자라는 중
       IsHarvastable//작물 성장 완료
    }

    public int id;//타일 ID
    public Vector2Int coord;
    public TileType tileType;
    public CropType cropType;
    public CropState cropState;
    public bool isFarmable;//이 타일에 새로운 작물을 심을 수 있는지
    private float growDuration;//성장 완료까지 남은 시간

    public void ApplyState(MiddleDB.TileState state)
    {
        id = state.id;
        coord = state.coord;
        tileType = state.tileType;
        isFarmable = state.isFarmable;
       
    }
}
