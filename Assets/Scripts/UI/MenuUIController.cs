using System;
using System.Collections.Generic;
using UnityEngine;

public class MenuUIManager : MonoBehaviour
{
    RectTransform _prevActiveUI = null;

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
    }
    void closeUI(RectTransform ui)
    {
        ui.anchoredPosition = new Vector2(3000f, 3000f);
    }
}
