using UnityEngine;
using UnityEngine.UI;

public class ConfingTabController : MonoBehaviour
{
    [System.Serializable]
    public class TabElements
    {
        public Image tabImage;
        public GameObject uiPanel;
    }

    [Header("UI 탭 리스트")]
    public TabElements[] tabs;

    [Header("색상 설정")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.9f, 0.9f, 0.9f); // 마우스가 올라갔을 때의 색상 (밝은 회색)
    public Color selectedColor = new Color(0.7f, 0.7f, 0.7f);

    // 현재 선택된 탭의 번호를 기억하는 변수
    private int currentSelectedIndex = -1;

    void Start()
    {
        if (tabs.Length > 0)
        {
            OnTabSelected(0);
        }
    }

    // 1. 탭을 클릭했을 때
    public void OnTabSelected(int selectedIndex)
    {
        currentSelectedIndex = selectedIndex; // 클릭한 탭을 현재 선택된 탭으로 저장

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isSelected = (i == selectedIndex);

            if (tabs[i].uiPanel != null)
            {
                tabs[i].uiPanel.SetActive(isSelected);
            }

            if (tabs[i].tabImage != null)
            {
                tabs[i].tabImage.color = isSelected ? selectedColor : normalColor;
            }
        }
    }

    // 2. 마우스 커서가 탭 위에 올라갔을 때 (Hover Enter)
    public void OnTabEnter(int index)
    {
        // 현재 선택된 탭이라면 호버 색상으로 바꾸지 않고 무시합니다.
        if (index == currentSelectedIndex) return;

        if (tabs[index].tabImage != null)
        {
            tabs[index].tabImage.color = hoverColor;
        }
    }

    // 3. 마우스 커서가 탭에서 벗어났을 때 (Hover Exit)
    public void OnTabExit(int index)
    {
        // 현재 선택된 탭이라면 기본 색상으로 돌아가지 않고 무시합니다.
        if (index == currentSelectedIndex) return;

        if (tabs[index].tabImage != null)
        {
            tabs[index].tabImage.color = normalColor;
        }
    }
}