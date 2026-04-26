using UnityEngine;

[System.Serializable]
public class SoundData
{
    public float masterVolume = 1.0f;
    public float bgmVolume = 1.0f;
    public float sfxVolume = 1.0f;

    // 데이터 복사를 위한 메서드
    public SoundData Clone()
    {
        return (SoundData)this.MemberwiseClone();
    }
}

public class SoundConfigController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CustomSlicedSlider masterSlider;
    [SerializeField] private CustomSlicedSlider bgmSlider;
    [SerializeField] private CustomSlicedSlider sfxSlider;

    private bool _isChanged = false;
    private SoundData _savedData;
    private SoundData _currentData;

    public bool IsChanged => _isChanged;
    public SoundData CurrentData => _currentData;

    private void Awake()
    {
        _currentData = new SoundData();
        _savedData = new SoundData();
    }

    private void Start()
    {
        // 초기 셋업이 끝나면 현재 상태를 저장된 원본 상태로 커밋
        CommitChanges();
    }

    // --- UI 이벤트 연동 메서드 ---

    public void SetMasterVolume(float volume)
    {
        if (_currentData.masterVolume != volume)
        {
            _currentData.masterVolume = volume;
            _isChanged = true;

            //ApplySoundSettingsToUnity();
        }
    }

    public void SetBgmVolume(float volume)
    {
        if (_currentData.bgmVolume != volume)
        {
            _currentData.bgmVolume = volume;
            _isChanged = true;
            //ApplySoundSettingsToUnity();
        }
    }

    public void SetSfxVolume(float volume)
    {
        if (_currentData.sfxVolume != volume)
        {
            _currentData.sfxVolume = volume;
            _isChanged = true;
            //ApplySoundSettingsToUnity();
        }
    }

    // --- 실제 Unity 사운드 시스템(AudioMixer 등)에 적용하는 메서드 ---
    private void ApplySoundSettingsToUnity()
    {
        // 예시: AudioMixer를 사용 중이라면 여기에 적용 코드를 넣습니다.
        // float masterDB = Mathf.Log10(Mathf.Max(_currentData.masterVolume, 0.0001f)) * 20f;
        // audioMixer.SetFloat("Master", masterDB);

        // Debug.Log($"[Sound] 사운드 즉시 적용: Master({_currentData.masterVolume})");
    }

    // --- Load Data ---
    public void ApplyLoadedData(SoundData loadedData)
    {
        // 1. 불러온 데이터를 현재 데이터로 깊은 복사(Clone)
        _currentData = loadedData.Clone();

        // 2. 불러온 데이터에 맞춰 슬라이더 UI 갱신 (이벤트 중복 실행 방지를 위해 SetValueWithoutNotify 사용)
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(_currentData.masterVolume);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(_currentData.bgmVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(_currentData.sfxVolume);

        // 3. 실제 사운드 시스템에 세팅을 적용(Apply)하고, 저장된 상태(_savedData)로 확정(Commit)
        CommitChanges();
    }

    // --- Commit & Revert ---

    // ConfigManager에서 Apply(저장)할 때 호출할 메서드
    public void CommitChanges()
    {
        _savedData = _currentData.Clone();
        _isChanged = false;
        ApplySoundSettingsToUnity();
    }

    // 되돌리기(Revert) 버튼 또는 창 닫을 때 호출할 메서드
    public void RevertChanges()
    {
        if (!_isChanged) return;

        // 1. 데이터를 이전 저장된 상태로 복구
        _currentData = _savedData.Clone();
        _isChanged = false;

        // 2. CustomSlicedSlider UI를 복구된 데이터에 맞게 다시 업데이트 (이벤트 무시)
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(_currentData.masterVolume);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(_currentData.bgmVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(_currentData.sfxVolume);

        // 3. 실제 사운드 볼륨도 원래 상태(저장되어 있던 상태)로 다시 롤백
        //ApplySoundSettingsToUnity();
    }
}