using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Events;

public class CustomSlicedSlider : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public RectMask2D fillMask;
    public RectTransform handleArea;
    public RectTransform handle;
    public TMP_Text valueText;

    public Image handleImage;
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Settings")]
    [Range(0f, 1f)]
    public float value = 0f;

    [Header("Events")]
    public UnityEvent<float> onValueChanged = new UnityEvent<float>();

    // 상태 체크용 변수
    private bool isPointerDown = false;
    private bool isPointerInside = false;
    private bool isDragging = false;

    private void OnValidate()
    {
        UpdateUI();
    }

    // --- 시각적 상태 업데이트 (버튼 기능 직접 구현) ---
    private void UpdateHandleColor()
    {
        if (handleImage == null) return;

        if (isPointerDown)
        {
            handleImage.color = pressedColor;
        }
        else if (isPointerInside || isDragging)
        {
            handleImage.color = hoverColor;
        }
        else
        {
            handleImage.color = normalColor;
        }
    }

    // --- 이벤트 핸들러 ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        UpdateHandleColor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        UpdateHandleColor();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        UpdateHandleColor();
        UpdateSlider(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        isDragging = false;
        UpdateHandleColor();
    }

    public void OnDrag(PointerEventData eventData)
    {
        isDragging = true;
        UpdateHandleColor();
        UpdateSlider(eventData);
    }

    // --- 슬라이더 로직 ---
    private void UpdateSlider(PointerEventData eventData)
    {
        if (handleArea == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handleArea, eventData.position, eventData.pressEventCamera, out localPoint);

        float percentage = Mathf.InverseLerp(
            handleArea.rect.xMin,
            handleArea.rect.xMax,
            localPoint.x
        );

        float newValue = Mathf.Clamp01(percentage);

        // 4. 값이 이전과 다르게 실제로 변경되었을 때만 이벤트 호출
        if (value != newValue)
        {
            value = newValue;
            UpdateUI();
            onValueChanged.Invoke(value); // 등록된 함수들 실행
        }
    }

    private void UpdateUI()
    {
        if (fillMask != null)
        {
            float fullWidth = handleArea.rect.width;
            float currentScaleX = fillMask.rectTransform.lossyScale.x;

            float paddingRight = (fullWidth * currentScaleX) * (1f - value) + handle.rect.width * currentScaleX * 0.5f;

            fillMask.padding = new Vector4(0, 0, paddingRight, 0);
        }

        // 2. Handle 위치 업데이트
        if (handle != null && handleArea != null)
        {
            handle.anchorMin = new Vector2(value, handle.anchorMin.y);
            handle.anchorMax = new Vector2(value, handle.anchorMax.y);
            handle.anchoredPosition = new Vector2(0, handle.anchoredPosition.y);
        }

        // 3. 퍼센트 텍스트 업데이트
        if (valueText != null)
        {
            valueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }

    public void SetValueWithoutNotify(float newValue)
    {
        value = Mathf.Clamp01(newValue);
        UpdateUI();
    }
}