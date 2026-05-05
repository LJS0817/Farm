using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class FontManager : MonoBehaviour
{
    [System.Serializable]
    public class LocaleFontSet
    {
        public TMP_FontAsset defaultFont;
        public TMP_FontAsset titleFont;
        public TMP_FontAsset descFont;
        public TMP_FontAsset shopFont;

        public TMP_FontAsset GetFont(LocalizedFontTarget.FontRole role)
        {
            return role switch
            {
                LocalizedFontTarget.FontRole.Title => titleFont != null ? titleFont : defaultFont,
                LocalizedFontTarget.FontRole.Desc => descFont != null ? descFont : defaultFont,
                LocalizedFontTarget.FontRole.Shop => shopFont != null ? shopFont : defaultFont,
                _ => defaultFont
            };
        }
    }

    private class FontTarget
    {
        public TMP_Text text;
        public LocalizedFontTarget.FontRole role;
    }

    [SerializeField] private LocaleFontSet englishFonts = new LocaleFontSet();
    [SerializeField] private LocaleFontSet koreanFonts = new LocaleFontSet();
    [SerializeField] private Transform searchRoot;
    [SerializeField] private bool includeInactiveObjects = true;
    [SerializeField] private bool logDebugInfo;

    private readonly List<FontTarget> localizedTextTargets = new List<FontTarget>();
    private Coroutine applyRoutine;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleSelectedLocaleChanged;
    }

    private void Start()
    {
        QueueRefreshAndApply();
    }

    public void RefreshFontTargets()
    {
        localizedTextTargets.Clear();

        Transform root = searchRoot != null ? searchRoot : transform;
        LocalizedFontTarget[] fontTargets = root.GetComponentsInChildren<LocalizedFontTarget>(includeInactiveObjects);

        HashSet<TMP_Text> uniqueTexts = new HashSet<TMP_Text>();
        for (int i = 0; i < fontTargets.Length; i++)
        {
            TMP_Text[] texts = fontTargets[i].GetComponentsInChildren<TMP_Text>(includeInactiveObjects);
            for (int j = 0; j < texts.Length; j++)
            {
                TMP_Text text = texts[j];
                if (text != null && uniqueTexts.Add(text))
                {
                    localizedTextTargets.Add(new FontTarget
                    {
                        text = text,
                        role = fontTargets[i].Role
                    });
                }
            }
        }

        if (logDebugInfo)
        {
            Debug.Log($"[FontManager] Collected {localizedTextTargets.Count} localized font target text(s).", this);
        }
    }

    public void ApplyFontForCurrentLocale()
    {
        ApplyFontForLocale(LocalizationSettings.SelectedLocale);
    }

    private void HandleSelectedLocaleChanged(Locale locale)
    {
        QueueRefreshAndApply();
    }

    private void QueueRefreshAndApply()
    {
        if (applyRoutine != null)
        {
            StopCoroutine(applyRoutine);
        }

        applyRoutine = StartCoroutine(RefreshAndApplyRoutine());
    }

    private IEnumerator RefreshAndApplyRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;
        yield return null;

        RefreshFontTargets();
        ApplyFontForCurrentLocale();
        applyRoutine = null;
    }

    private void ApplyFontForLocale(Locale locale)
    {
        LocaleFontSet fontSet = GetFontSetForLocale(locale);
        for (int i = localizedTextTargets.Count - 1; i >= 0; i--)
        {
            FontTarget target = localizedTextTargets[i];
            if (target.text == null)
            {
                localizedTextTargets.RemoveAt(i);
                continue;
            }

            TMP_FontAsset targetFont = fontSet.GetFont(target.role);
            if (targetFont != null)
            {
                target.text.font = targetFont;
                target.text.ForceMeshUpdate();
            }
            else if (logDebugInfo)
            {
                Debug.LogWarning($"[FontManager] Missing font for locale '{GetLocaleCode(locale)}' and role '{target.role}'.", target.text);
            }
        }

        if (logDebugInfo)
        {
            Debug.Log($"[FontManager] Applied fonts for locale '{GetLocaleCode(locale)}'.", this);
        }
    }

    private LocaleFontSet GetFontSetForLocale(Locale locale)
    {
        string localeCode = GetLocaleCode(locale);
        if (localeCode.StartsWith("en"))
        {
            return englishFonts;
        }

        if (localeCode.StartsWith("ko"))
        {
            return koreanFonts;
        }

        return koreanFonts;
    }

    private string GetLocaleCode(Locale locale)
    {
        return locale != null ? locale.Identifier.Code : string.Empty;
    }

}
