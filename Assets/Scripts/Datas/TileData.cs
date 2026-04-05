using UnityEngine;

// 씬의 개별 타일 오브젝트가 들고 있는 런타임 상태 데이터.
// 실제 값은 MiddleDB에서 받아와 ApplyState로 동기화된다.
public class TileData : MonoBehaviour
{
    public enum TileType
    {
        Weed,
        Soil,
        Water,
    }
    public enum CropType
    {
        IsEmpty,//작물이 없을 경우
        Carrot,
        Cherry
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
    public float maxTime;
    private float growDuration;//성장 완료까지 남은 시간

    public float GrowDuration
    {
        get => growDuration;
        set => growDuration = Mathf.Max(0f, value);
    }

    // MiddleDB의 TileState를 현재 타일 컴포넌트에 복사한다.
    public void ApplyState(MiddleDB.TileState state)
    {
        id = state.id;
        coord = state.coord;
        tileType = state.tileType;
        cropType = state.cropType;
        cropState = state.cropState;
        isFarmable = state.isFarmable;
        GrowDuration = state.growDuration;
        maxTime = state.maxTime;
    }
}
