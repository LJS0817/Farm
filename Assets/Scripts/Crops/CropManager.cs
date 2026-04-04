using UnityEngine;

public class CropManager : MonoBehaviour
{
    public static CropManager instance;

    [Header("Crop Data List")]
    public CropsData[] cropDatas;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("[CropManager] Duplicate instance detected. Destroying...");
            Destroy(gameObject);
        }
    }

    // cropId로 CropData 가져오기
    public CropsData GetCropData(int cropId)
    {
        foreach (var data in cropDatas)
        {
            if (data.cropId == cropId)
                return data;
        }

        Debug.LogError($"[CropManager] CropData not found: {cropId}");
        return null;
    }

    public CropsData GetCropData(TileData.CropType cropType)
    {
        foreach (var data in cropDatas)
        {
            if (data != null && data.crop == cropType)
            {
                return data;
            }
        }

        Debug.LogError($"[CropManager] CropData not found for crop type: {cropType}");
        return null;
    }
}
