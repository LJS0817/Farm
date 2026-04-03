using UnityEngine;

[CreateAssetMenu(menuName = "Game/Crop Data")]
public class CropsData : ScriptableObject
{
    public int cropId;
    public string cropName;
    public float growTime;
    public Sprite finalCropSprite;
}
