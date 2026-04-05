using UnityEngine;

// 작물 ScriptableObject 목록을 보관하고, cropId 또는 cropType으로 조회해 주는 매니저.
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
    // UI나 AI 명령에서 받은 cropId를 실제 작물 데이터로 변환한다.
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

    // 현재 타일에 심겨 있는 작물 타입으로 작물 데이터를 다시 찾는다.
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
