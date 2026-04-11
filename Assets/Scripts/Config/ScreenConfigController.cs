using UnityEngine;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class ScreenData
{
    public int resolutionWidth;
    public int resolutionHeight;
    public FullScreenMode screenMode = FullScreenMode.FullScreenWindow;
    public int vSyncCount = 1;
}

public class ScreenConfigController : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    private bool _isChanged = false;
    private ScreenData _currentData;
    private List<Resolution> _filteredResolutions = new List<Resolution>();

    // 매니저에서 접근할 수 있도록 프로퍼티 노출
    public bool IsChanged => _isChanged;
    public ScreenData CurrentData => _currentData;

    private void Awake()
    {
        _currentData = new ScreenData();
    }

    private void Start()
    {
        InitResolutionOptions();
    }

    private void InitResolutionOptions()
    {
        // 1. 기존 드롭다운 옵션 비우기
        resolutionDropdown.ClearOptions();
        _filteredResolutions.Clear();

        // 2. 모니터가 지원하는 모든 해상도 가져오기
        Resolution[] rawResolutions = Screen.resolutions;
        List<string> optionsTextList = new List<string>();
        int currentResolutionIndex = 0;

        // 3. 주사율(Hz) 차이로 인한 중복 해상도 제거 및 리스트 구성
        for (int i = 0; i < rawResolutions.Length; i++)
        {
            bool isDuplicate = false;
            foreach (var res in _filteredResolutions)
            {
                if (res.width == rawResolutions[i].width && res.height == rawResolutions[i].height)
                {
                    isDuplicate = true;
                    break;
                }
            }

            // 중복이 아닐 때만 리스트에 추가
            if (!isDuplicate)
            {
                _filteredResolutions.Add(rawResolutions[i]);
                optionsTextList.Add($"{rawResolutions[i].width} x {rawResolutions[i].height}");

                // 현재 화면 해상도와 일치하는 항목의 인덱스 찾기
                if (rawResolutions[i].width == Screen.width && rawResolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = _filteredResolutions.Count - 1;
                }
            }
        }

        // 4. 드롭다운에 텍스트 옵션 추가
        resolutionDropdown.AddOptions(optionsTextList);

        // 5. 드롭다운의 초기값을 현재 해상도로 설정
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // 데이터 초기화
        _currentData.resolutionWidth = _filteredResolutions[currentResolutionIndex].width;
        _currentData.resolutionHeight = _filteredResolutions[currentResolutionIndex].height;
    }

    public void SetResolutionByIndex(int index)
    {
        if (index < 0 || index >= _filteredResolutions.Count) return;
        int newWidth = _filteredResolutions[index].width;
        int newHeight = _filteredResolutions[index].height;

        if (_currentData.resolutionWidth != newWidth || _currentData.resolutionHeight != newHeight)
        {
            _currentData.resolutionWidth = newWidth;
            _currentData.resolutionHeight = newHeight;
            _isChanged = true;
        }
    }

    public void SetScreenModeByIndex(int index)
    {
        FullScreenMode mode = FullScreenMode.FullScreenWindow;
        switch (index)
        {
            case 0: mode = FullScreenMode.FullScreenWindow; break;
            case 1: mode = FullScreenMode.ExclusiveFullScreen; break;
            case 2: mode = FullScreenMode.Windowed; break;
        }

        if (_currentData.screenMode != mode) { _currentData.screenMode = mode; _isChanged = true; }
    }

    public void SetVSync(bool isOn)
    {
        int syncValue = isOn ? 1 : 0;
        if (_currentData.vSyncCount != syncValue) { _currentData.vSyncCount = syncValue; _isChanged = true; }
    }

    public void ResetChangeFlag()
    {
        _isChanged = false;
    }
}