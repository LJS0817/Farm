using System.Collections.Generic;
using UnityEngine;

public class ItemFeedbackUIController : MonoBehaviour
{
    ItemFloatingUI[] _floatingUI;

    int _curIdx;

    private void Awake()
    {
        _curIdx = 0;
        _floatingUI = GetComponentsInChildren<ItemFloatingUI>(true);
    }

    public void ShowItemFeedback(Vector2 pos, Sprite sp, int amt)
    {
        _floatingUI[_curIdx].ShowUI(RectTransformUtility.WorldToScreenPoint(Camera.main, pos), sp, amt);
        _curIdx = (_curIdx + 1) % _floatingUI.Length;
    }
}
