using UnityEngine;
using UnityEngine.UI;
using System.Collections; // 코루틴을 사용하기 위해 필수

public class ConfigTabController : MonoBehaviour
{
    [System.Serializable]
    public class TabElements
    {
        public Image tabImage;
        public GameObject uiPanel;

        // 각 탭마다 실행 중인 코루틴을 개별적으로 추적하기 위한 변수 (인스펙터엔 숨김)
        [HideInInspector]
        public Coroutine colorCoroutine;
    }

    [Header("UI 탭 리스트")]
    public TabElements[] tabs;

    [Header("색상 설정")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.9f, 0.9f, 0.9f);
    public Color selectedColor = new Color(0.7f, 0.7f, 0.7f);

    [Header("애니메이션 설정")]
    public float fadeDuration = 0.15f; // 색상이 변하는 데 걸리는 시간

    private int currentSelectedIndex = -1;

    void Start()
    {
        if (tabs.Length > 0)
        {
            // 게임 시작 시에는 즉시(instant: true) 변경
            ApplyTabChange(0, true);
        }
    }

    public void OnTabSelected(int selectedIndex)
    {
        // UI 클릭 시에는 부드럽게(instant: false) 변경
        ApplyTabChange(selectedIndex, false);
    }

    private void ApplyTabChange(int selectedIndex, bool instant)
    {
        currentSelectedIndex = selectedIndex;

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isSelected = (i == selectedIndex);

            if (tabs[i].uiPanel != null)
            {
                tabs[i].uiPanel.SetActive(isSelected);
            }

            if (tabs[i].tabImage != null)
            {
                Color targetColor = isSelected ? selectedColor : normalColor;

                if (instant)
                {
                    // 즉시 변경 (코루틴 정지 후 바로 색상 적용)
                    if (tabs[i].colorCoroutine != null) StopCoroutine(tabs[i].colorCoroutine);
                    tabs[i].tabImage.color = targetColor;
                }
                else
                {
                    // 부드럽게 코루틴으로 변경
                    PlayColorAnimation(i, targetColor);
                }
            }
        }
    }

    public void OnTabEnter(int index)
    {
        if (index == currentSelectedIndex) return;
        PlayColorAnimation(index, hoverColor);
    }

    public void OnTabExit(int index)
    {
        if (index == currentSelectedIndex) return;
        PlayColorAnimation(index, normalColor);
    }

    // --------------------------------------------------------
    // 애니메이션 제어 메서드
    // --------------------------------------------------------

    private void PlayColorAnimation(int index, Color targetColor)
    {
        if (tabs[index].tabImage == null) return;

        // 1. 해당 탭에서 이미 다른 색상 변경 코루틴이 돌고 있다면 정지!
        // (빠르게 마우스를 움직일 때 깜빡이는 버그 방지)
        if (tabs[index].colorCoroutine != null)
        {
            StopCoroutine(tabs[index].colorCoroutine);
        }

        // 2. 새로운 색상 변경 코루틴 시작 및 추적 변수에 저장
        tabs[index].colorCoroutine = StartCoroutine(ColorLerpRoutine(index, targetColor));
    }

    private IEnumerator ColorLerpRoutine(int index, Color targetColor)
    {
        Image img = tabs[index].tabImage;
        Color startColor = img.color;
        float time = 0f;

        while (time < fadeDuration)
        {
            // Time.unscaledDeltaTime을 사용하여 게임 일시정지 중에도 UI가 반응하도록 함
            time += Time.unscaledDeltaTime;
            float t = time / fadeDuration;

            // 서서히 색상 혼합
            img.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        // 마지막에 목표 색상으로 정확히 맞춰주고 코루틴 비우기
        img.color = targetColor;
        tabs[index].colorCoroutine = null;
    }
}