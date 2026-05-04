using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class TileInfo_Plus : MonoBehaviour
{
    public enum PlusInfo
    {
        None,
        Moist
    }
    public PlusInfo plusInfo;
    public TMP_Text text;

    [SerializeField] private string tileInfoTableName = "TileInfo";

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleSelectedLocaleChanged;
    }

    public void PlusInfoInit(PlusInfo plusInfo)
    {
        this.plusInfo = plusInfo;
        RefreshText();
    }

    private void HandleSelectedLocaleChanged(Locale locale)
    {
        RefreshText();
    }

    private void RefreshText()
    {
        if (text == null)
        {
            text = GetComponentInChildren<TMP_Text>();
        }

        if (text == null)
        {
            return;
        }

        text.text = GetDisplayText(plusInfo);
    }

    private string GetDisplayText(PlusInfo plusInfo)
    {
        return plusInfo switch
        {
            PlusInfo.Moist => L("tile_info.plus.moist", "<b>촉촉함</b>: 인접한 물 타일 효과로 수확량이 2배가 됩니다."),
            _ => string.Empty
        };
    }

    private string L(string entryKey, string fallback)
    {
        if (string.IsNullOrEmpty(tileInfoTableName) || string.IsNullOrEmpty(entryKey))
        {
            return fallback;
        }

        string localized = LocalizationSettings.StringDatabase.GetLocalizedString(tileInfoTableName, entryKey);
        return string.IsNullOrEmpty(localized) ? fallback : localized;
    }
}
