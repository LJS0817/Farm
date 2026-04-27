using UnityEngine;

public class ItemFeedbackUIController : MonoBehaviour
{
    RectTransform _canvasRect;
    ItemFloatingUI[] _floatingUI;

    int _curIdx;

    private void Awake()
    {
        _curIdx = 0;
        _floatingUI = GetComponentsInChildren<ItemFloatingUI>(true);
        _canvasRect = transform.parent.GetComponent<RectTransform>();
    }

    public void ShowItemFeedback(Vector2 pos, Sprite sp, int amt)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, pos);
        Vector2 localPoint;

        // 캔버스 내에서의 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, null, out localPoint);
        _floatingUI[_curIdx].ShowUI(localPoint, sp, amt);
        _curIdx = (_curIdx + 1) % _floatingUI.Length;
    }
}
