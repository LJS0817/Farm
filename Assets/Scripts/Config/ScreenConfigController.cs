using UnityEngine;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class ScreenData
{
    public int resolutionWidth;
    public int resolutionHeight;
    public FullScreenMode screenMode = FullScreenMode.FullScreenWindow;
    //public int vSyncCount = 1;

    public ScreenData Clone()
    {
        return (ScreenData)this.MemberwiseClone();
    }
}

public class ScreenConfigController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown screenModeDropdown;

    private bool _isChanged = false;

    private ScreenData _savedData;
    private ScreenData _currentData;

    private List<Resolution> _filteredResolutions = new List<Resolution>();
    private List<string> _resolutionOptions = new List<string>();

    public bool IsChanged => _isChanged;
    public ScreenData CurrentData => _currentData;

    private void Awake()
    {
        _currentData = new ScreenData();
        _savedData = new ScreenData();
    }

    private void Start()
    {
        InitResolutionOptions();
        InitScreenModeOptions();

        CommitChanges();
    }

    private void InitResolutionOptions()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();
        _filteredResolutions.Clear();
        _resolutionOptions.Clear();

        Resolution[] rawResolutions = Screen.resolutions;
        int currentResolutionIndex = 0;

        for (int i = 0; i < rawResolutions.Length; i++)
        {
            bool isDuplicate = false;
            for (int j = 0; j < _filteredResolutions.Count; j++)
            {
                if (_filteredResolutions[j].width == rawResolutions[i].width &&
                    _filteredResolutions[j].height == rawResolutions[i].height)
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                Resolution res = rawResolutions[i];
                _filteredResolutions.Add(res);
                _resolutionOptions.Add($"{res.width} x {res.height}");

                if (res.width == Screen.width && res.height == Screen.height)
                {
                    currentResolutionIndex = _filteredResolutions.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(_resolutionOptions);
        resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
        resolutionDropdown.RefreshShownValue();

        _currentData.resolutionWidth = _filteredResolutions[currentResolutionIndex].width;
        _currentData.resolutionHeight = _filteredResolutions[currentResolutionIndex].height;
    }

    private void InitScreenModeOptions()
    {
        if (screenModeDropdown == null) return;

        screenModeDropdown.ClearOptions();
        List<string> optionsTextList = new List<string>(3) { "전체 창모드", "전체화면", "창모드" };
        screenModeDropdown.AddOptions(optionsTextList);

        int currentModeIndex = Screen.fullScreenMode switch
        {
            FullScreenMode.ExclusiveFullScreen => 1,
            FullScreenMode.Windowed => 2,
            _ => 0
        };

        screenModeDropdown.SetValueWithoutNotify(currentModeIndex);
        screenModeDropdown.RefreshShownValue();

        _currentData.screenMode = Screen.fullScreenMode;
        //_currentData.vSyncCount = QualitySettings.vSyncCount;
    }

    private void ApplyScreenSettingsToUnity()
    {
        Screen.SetResolution(_currentData.resolutionWidth, _currentData.resolutionHeight, _currentData.screenMode);
    }

    public void SetResolutionByIndex(int index)
    {
        if (index < 0 || index >= _filteredResolutions.Count) return;

        Resolution targetRes = _filteredResolutions[index];
        if (_currentData.resolutionWidth != targetRes.width || _currentData.resolutionHeight != targetRes.height)
        {
            _currentData.resolutionWidth = targetRes.width;
            _currentData.resolutionHeight = targetRes.height;
            _isChanged = true;

            // 값 변경 시 실제 화면에 즉시 반영
            //ApplyScreenSettingsToUnity();
        }
    }

    public void SetScreenModeByIndex(int index)
    {
        FullScreenMode mode = index switch
        {
            1 => FullScreenMode.ExclusiveFullScreen,
            2 => FullScreenMode.Windowed,
            _ => FullScreenMode.FullScreenWindow
        };

        if (_currentData.screenMode != mode)
        {
            _currentData.screenMode = mode;
            _isChanged = true;

            // 값 변경 시 실제 화면에 즉시 반영
            //ApplyScreenSettingsToUnity();
        }
    }

    //public void SetVSync(bool isOn)
    //{
    //    int syncValue = isOn ? 1 : 0;
    //    if (_currentData.vSyncCount != syncValue)
    //    {
    //        _currentData.vSyncCount = syncValue;
    //        _isChanged = true;

    //        // VSync 즉시 반영
    //        QualitySettings.vSyncCount = _currentData.vSyncCount;
    //    }
    //}

    // --- Load Data ---
    public void ApplyLoadedData(ScreenData loadedData)
    {
        // 1. 불러온 데이터를 현재 데이터로 깊은 복사(Clone)
        _currentData = loadedData.Clone();

        // 2. 불러온 데이터에 맞춰 드롭다운 UI 갱신
        UpdateUIFromData();

        // 3. 실제 화면에 세팅을 적용(Apply)하고, 저장된 상태(_savedData)로 확정(Commit)
        CommitChanges();
    }

    // --- Commit & Revert ---
    public void CommitChanges()
    {
        ApplyScreenSettingsToUnity();
        _savedData = _currentData.Clone();
        _isChanged = false;
    }

    public void RevertChanges()
    {
        if (!_isChanged) return;

        // 1. 데이터를 이전 저장된 상태로 복구
        _currentData = _savedData.Clone();
        _isChanged = false;

        // 2. UI를 복구된 데이터에 맞게 다시 업데이트
        UpdateUIFromData();

        //ApplyScreenSettingsToUnity();
        //QualitySettings.vSyncCount = _currentData.vSyncCount;
    }

    private void UpdateUIFromData()
    {
        int resIndex = _filteredResolutions.FindIndex(r => r.width == _currentData.resolutionWidth && r.height == _currentData.resolutionHeight);
        if (resIndex >= 0)
        {
            resolutionDropdown.SetValueWithoutNotify(resIndex);
            resolutionDropdown.RefreshShownValue();
        }

        int modeIndex = _currentData.screenMode switch
        {
            FullScreenMode.ExclusiveFullScreen => 1,
            FullScreenMode.Windowed => 2,
            _ => 0
        };
        screenModeDropdown.SetValueWithoutNotify(modeIndex);
        screenModeDropdown.RefreshShownValue();
    }
}