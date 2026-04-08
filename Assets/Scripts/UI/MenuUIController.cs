using UnityEngine;

public class MenuUIManager : MonoBehaviour
{ 
    public void OpenUI(RectTransform ui)
    {
        if (ui == null) return;
        if(ui.anchoredPosition.x > 0f) ui.anchoredPosition = new Vector2(-30f, 30f);
        else ui.anchoredPosition = new Vector2(3000f, 3000f);
    }
}
