using UnityEngine;

[System.Serializable]
public class SoundData
{
    public float masterVolume = 1.0f;
    public float bgmVolume = 1.0f;
    public float sfxVolume = 1.0f;
    public bool isMuted = false;
}

public class SoundConfigController : MonoBehaviour
{
    private bool _isChanged = false;
    private SoundData _currentData;

    // 매니저에서 접근할 수 있도록 프로퍼티(Property)로 노출
    public bool IsChanged => _isChanged;
    public SoundData CurrentData => _currentData;

    private void Awake()
    {
        _currentData = new SoundData();
    }

    public void SetMasterVolume(float volume)
    {
        if (_currentData.masterVolume != volume) { _currentData.masterVolume = volume; _isChanged = true; }
    }

    public void SetBgmVolume(float volume)
    {
        if (_currentData.bgmVolume != volume) { _currentData.bgmVolume = volume; _isChanged = true; }
    }

    public void SetSfxVolume(float volume)
    {
        if (_currentData.sfxVolume != volume) { _currentData.sfxVolume = volume; _isChanged = true; }
    }

    public void SetMute(bool mute)
    {
        if (_currentData.isMuted != mute) { _currentData.isMuted = mute; _isChanged = true; }
    }

    public void ResetChangeFlag()
    {
        _isChanged = false;
    }
}