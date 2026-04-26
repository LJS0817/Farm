using UnityEngine;
using System.IO;

[System.Serializable]
public class PlayerConfigData
{
    // 두 개의 데이터를 하나로 묶는 최상위 클래스
    public SoundData soundData;
    public ScreenData screenData;
}

public class ConfigManager : MonoBehaviour
{
    [Header("Controllers")]
    public SoundConfigController soundController;
    public ScreenConfigController screenController;

    private const string CONFIG_FILE_NAME = "playerConfig.json";

    private void Start()
    {
        // 게임 시작 시 자동으로 설정 불러오기
        LoadConfig();
    }

    public void LoadConfig()
    {
        string loadPath = Path.Combine(Application.persistentDataPath, CONFIG_FILE_NAME);

        if (!File.Exists(loadPath))
        {
            Debug.Log("저장된 설정 파일이 없습니다. 기본 설정을 유지합니다.");
            return;
        }

        try
        {
            string json = File.ReadAllText(loadPath);

            PlayerConfigData loadedData = JsonUtility.FromJson<PlayerConfigData>(json);

            if (loadedData.soundData != null) soundController.ApplyLoadedData(loadedData.soundData);
            if (loadedData.screenData != null) screenController.ApplyLoadedData(loadedData.screenData);

            Debug.Log("[PC Load] 설정을 성공적으로 불러와 적용했습니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PC Load] 설정 로드 중 오류 발생: {e.Message}");
        }
    }

    public void OnApplyButtonClicked()
    {
        // 1. 소리나 화면 중 하나라도 변경된 사항이 있는지 체크
        if (!soundController.IsChanged && !screenController.IsChanged)
        {
            Debug.Log("변경된 설정이 없어 저장을 건너뜁니다.");
            return;
        }

        // 2. 최상위 데이터 객체 생성 및 각 컨트롤러의 데이터 할당
        PlayerConfigData mergedConfig = new PlayerConfigData();
        mergedConfig.soundData = soundController.CurrentData;
        mergedConfig.screenData = screenController.CurrentData;

        // 3. 통합된 데이터를 단일 JSON 문자열로 변환 (true: 보기 좋게 줄바꿈 적용)
        string mergedJson = JsonUtility.ToJson(mergedConfig, true);

        // 4. PC 로컬 저장소에 파일 기록
        string savePath = Path.Combine(Application.persistentDataPath, CONFIG_FILE_NAME);
        File.WriteAllText(savePath, mergedJson);

        soundController.CommitChanges();
        screenController.CommitChanges();

        Debug.Log($"[PC Save] 파일로 저장되었습니다.\n경로: {mergedJson}");
    }

    public void OnRevertButtonClicked()
    {
        // 각 컨트롤러가 가지고 있는 수정 전 상태로 스스로를 되돌림
        soundController.RevertChanges();
        screenController.RevertChanges();
    }
}