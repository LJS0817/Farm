using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIManager : MonoBehaviour
{
    RectTransform _prevActiveUI = null;

    [SerializeField]
    RectTransform _settingWindow;
    Image _settingWindowBg;
    Color _settingBgColorEnable;
    Color _settingBgColorDisable;

    [Header("UI Settings")]
    public float fadeDuration = 0.3f; // 페이드 효과가 걸리는 시간

    private void Start()
    {
        _settingWindowBg = _settingWindow.GetChild(0).GetComponent<Image>();
        _settingBgColorDisable = _settingBgColorEnable = _settingWindowBg.color;
        _settingBgColorDisable.a = 0;
    }

    public void ControlWindow(RectTransform ui)
    {
        if (ui == null) return;
        if (_prevActiveUI != null && !ReferenceEquals(_prevActiveUI.gameObject, ui.gameObject)) closeUI(_prevActiveUI);
        if (ui.anchoredPosition.x > 0f) openUI(ui);
        else closeUI(ui);
        _prevActiveUI = ui;
    }
    void openUI(RectTransform ui)
    {
        ui.anchoredPosition = ui.pivot.x == 0.5f ? Vector2.zero : new Vector2(-30f, 30f);

        CanvasGroup cg = ui.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = true;
            StartCoroutine(FadeUI(cg, cg.alpha, 1f));
        }
    }

    void closeUI(RectTransform ui)
    {
        CanvasGroup cg = ui.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = false;

            StartCoroutine(FadeUI(cg, cg.alpha, 0f, () => {
                ui.anchoredPosition = new Vector2(3000f, 3000f);
            }));
        }
        else
        {
            ui.anchoredPosition = new Vector2(3000f, 3000f);
        }
    }

    private IEnumerator FadeUI(CanvasGroup cg, float startAlpha, float targetAlpha, Action onComplete = null)
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;

            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        cg.alpha = targetAlpha;

        onComplete?.Invoke();
    }

    public void OnClickPauseGame(RectTransform ui)
    {
        Time.timeScale = 0f;
        // AudioListener.pause = true;
        openUI(ui);
    }

    public void OnClickResumeGame(RectTransform ui)
    {
        Time.timeScale = 1f;
        // AudioListener.pause = false;
        closeUI(ui); 
    }

    public void OnClickOpenSettingWindow(bool isLogo)
    {
        if (isLogo && _settingWindowBg.color.a != _settingBgColorEnable.a) _settingWindowBg.color = _settingBgColorEnable;
        else if (!isLogo && _settingWindowBg.color.a != _settingBgColorDisable.a) _settingWindowBg.color = _settingBgColorDisable;
        openUI(_settingWindow);
    }

    public void OnClickCloseSettingWindow()
    {
        closeUI(_settingWindow);
    }
    
    public void OnClickReturnMenu()
    {
        //SceneManager.LoadScene(1);
        if (_prevActiveUI != null) closeUI(_prevActiveUI);
    }
}
