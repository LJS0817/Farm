using System;
using System.Collections; // 코루틴을 위해 추가
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] Image _img;
    [SerializeField] TMP_Text _amount;
    [SerializeField] GameObject _focusEffect;

    CanvasGroup _focusCanvasGroup;
    bool _isFocused = false;

    InventorySlot _currentItem;

    // 코루틴 추적용 변수 및 페이드 시간 설정
    Coroutine _fadeCoroutine;
    const float _fadeDuration = 0.05f;

    public event Action<InventorySlotUI> OnSlotClickedEvent;
    public event Action<InventorySlotUI> OnPointerEnterEvent;
    public event Action<InventorySlotUI> OnPointerExitEvent;

    private void Awake()
    {
        if (_focusEffect != null)
        {
            _focusEffect.SetActive(true);

            if (!_focusEffect.TryGetComponent(out _focusCanvasGroup))
            {
                _focusCanvasGroup = _focusEffect.AddComponent<CanvasGroup>();
            }

            _focusCanvasGroup.alpha = 0f;
            _focusCanvasGroup.blocksRaycasts = false;
            _focusCanvasGroup.interactable = false;
        }
    }

    public void UpdateSlot(InventorySlot item)
    {
        if (item != null && item.item != null)
        {
            _currentItem = item;
            _img.sprite = item.item.icon;

            if (!_img.enabled) _img.enabled = true;

            if (item.count > 1)
            {
                if (!_amount.enabled) _amount.enabled = true;
                _amount.SetText("x {0}", item.count);
            }
            else
            {
                if (_amount.enabled) _amount.enabled = false;
            }
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        _currentItem = null;

        if (_img != null && _img.enabled)
        {
            _img.sprite = null;
            _img.enabled = false;
        }

        if (_amount != null && _amount.enabled)
        {
            _amount.enabled = false;
        }

        UnfocusSlot();
    }

    public void FocusSlot()
    {
        if (_currentItem != null && _focusCanvasGroup != null)
        {
            _isFocused = true;
            FadeTo(1f); // 1.0으로 페이드 인
        }
    }

    public void UnfocusSlot()
    {
        _isFocused = false;
        if (_focusCanvasGroup != null)
        {
            FadeTo(0f); // 0.0으로 페이드 아웃
        }
    }

    public InventorySlot GetItemInfo() => _currentItem;
    public bool IsEmptySlot() => _currentItem == null;

    public void OnSlotClicked() => OnSlotClickedEvent?.Invoke(this);

    public void OnPointerEnter()
    {
        if (!_isFocused && _currentItem != null && _focusCanvasGroup != null)
        {
            FadeTo(0.25f); // 호버 시 0.5로 페이드 인
        }

        OnPointerEnterEvent?.Invoke(this);
    }

    public void OnPointerExit()
    {
        if (!_isFocused && _focusCanvasGroup != null)
        {
            FadeTo(0f); // 마우스가 나가면 0.0으로 페이드 아웃
        }

        OnPointerExitEvent?.Invoke(this);
    }

    // =========================================
    // CanvasGroup 페이드 로직 (슬롯용)
    // =========================================
    private void FadeTo(float targetAlpha)
    {
        // 1. 기존에 실행 중인 페이드가 있다면 중단
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        // 2. 오브젝트가 활성화된 상태에서만 코루틴 실행 
        // (인벤토리 창을 갑자기 닫을 때 오류가 발생하는 것을 방지)
        if (gameObject.activeInHierarchy)
        {
            _fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
        }
        else
        {
            // 만약 오브젝트가 꺼져있다면 즉시 목표값 적용
            _focusCanvasGroup.alpha = targetAlpha;
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = _focusCanvasGroup.alpha;
        float time = 0f;

        while (time < _fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            _focusCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / _fadeDuration);
            yield return null;
        }

        _focusCanvasGroup.alpha = targetAlpha;
        _fadeCoroutine = null;
    }
}