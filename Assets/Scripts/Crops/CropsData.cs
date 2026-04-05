using UnityEngine;

[CreateAssetMenu(menuName = "Game/Crop Data")]
// 작물 1종의 메타데이터를 담는 ScriptableObject.
// 성장 시간, 최종 스프라이트, 수확 아이템 같은 정보를 정의한다.
public class CropsData : ScriptableObject
{
    public int cropId;
    public TileData.CropType crop;
    public string cropName;
    public float growTime;
    public Sprite finalCropSprite;
    public ItemSO harvestItem;
    public int harvestAmount = 1;
}
