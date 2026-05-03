using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[System.Serializable]
public class LanguageData
{
    public string localeCode = "ko-KR";

    public LanguageData Clone()
    {
        return (LanguageData)MemberwiseClone();
    }
}

public class LanguageConfigController : MonoBehaviour
{
    [System.Serializable]
    private class LanguageOption
    {
        public string localeCode;
        public string displayName;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Language Options")]
    [SerializeField] private List<LanguageOption> languageOptions = new List<LanguageOption>
    {
        new LanguageOption { localeCode = "ko-KR", displayName = "한국어" },
        new LanguageOption { localeCode = "en", displayName = "English" }
    };

    private bool _isChanged;
    private LanguageData _savedData;
    private LanguageData _currentData;
    private Coroutine _applyLocaleCoroutine;

    public bool IsChanged => _isChanged;
    public bool NeedsApply => _isChanged || GetSelectedLocaleCode() != _currentData.localeCode;
    public LanguageData CurrentData => _currentData;

    private void Awake()
    {
        string selectedLocaleCode = GetSelectedLocaleCode();
        _currentData = new LanguageData { localeCode = selectedLocaleCode };
        _savedData = _currentData.Clone();

        InitLanguageOptions();
        UpdateUIFromData();
        CommitChanges();
    }

    private void OnEnable()
    {
        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.AddListener(SetLanguageByIndex);
        }
    }

    private void OnDisable()
    {
        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveListener(SetLanguageByIndex);
        }
    }

    private void InitLanguageOptions()
    {
        if (languageDropdown == null)
        {
            return;
        }

        languageDropdown.ClearOptions();

        List<string> optionLabels = new List<string>(languageOptions.Count);
        for (int i = 0; i < languageOptions.Count; i++)
        {
            optionLabels.Add(languageOptions[i].displayName);
        }

        languageDropdown.AddOptions(optionLabels);
        languageDropdown.RefreshShownValue();
    }

    public void SetLanguageByIndex(int index)
    {
        if (index < 0 || index >= languageOptions.Count)
        {
            return;
        }

        string localeCode = languageOptions[index].localeCode;
        if (_currentData.localeCode != localeCode)
        {
            _currentData.localeCode = localeCode;
            _isChanged = true;
        }
    }

    public void SyncCurrentDataFromUI()
    {
        if (languageDropdown != null)
        {
            SetLanguageByIndex(languageDropdown.value);
        }
    }

    public void ApplyLoadedData(LanguageData loadedData)
    {
        _currentData = loadedData.Clone();
        EnsureSupportedLocale();
        UpdateUIFromData();
        CommitChanges();
    }

    public void CommitChanges()
    {
        EnsureSupportedLocale();
        ApplyLanguageSettingsToUnity();
        _savedData = _currentData.Clone();
        _isChanged = false;
    }

    public void RevertChanges()
    {
        if (!_isChanged)
        {
            return;
        }

        _currentData = _savedData.Clone();
        _isChanged = false;
        UpdateUIFromData();
    }

    private void ApplyLanguageSettingsToUnity()
    {
        if (_applyLocaleCoroutine != null)
        {
            StopCoroutine(_applyLocaleCoroutine);
        }

        _applyLocaleCoroutine = StartCoroutine(ApplyLanguageSettingsRoutine());
    }

    private IEnumerator ApplyLanguageSettingsRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        Locale locale = LocalizationSettings.AvailableLocales.GetLocale(_currentData.localeCode);
        if (locale != null && LocalizationSettings.SelectedLocale != locale)
        {
            LocalizationSettings.SelectedLocale = locale;
        }

        _applyLocaleCoroutine = null;
    }

    private void UpdateUIFromData()
    {
        if (languageDropdown == null)
        {
            return;
        }

        int index = FindOptionIndex(_currentData.localeCode);
        languageDropdown.SetValueWithoutNotify(index);
        languageDropdown.RefreshShownValue();
    }

    private void EnsureSupportedLocale()
    {
        if (FindOptionIndex(_currentData.localeCode) >= 0)
        {
            return;
        }

        _currentData.localeCode = languageOptions.Count > 0 ? languageOptions[0].localeCode : "ko-KR";
    }

    private int FindOptionIndex(string localeCode)
    {
        for (int i = 0; i < languageOptions.Count; i++)
        {
            if (languageOptions[i].localeCode == localeCode)
            {
                return i;
            }
        }

        return 0;
    }

    private string GetSelectedLocaleCode()
    {
        Locale selectedLocale = LocalizationSettings.SelectedLocale;
        if (selectedLocale != null)
        {
            return selectedLocale.Identifier.Code;
        }

        return languageOptions.Count > 0 ? languageOptions[0].localeCode : "ko-KR";
    }
}
