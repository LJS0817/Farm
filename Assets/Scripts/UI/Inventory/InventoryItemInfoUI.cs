using System.Collections; // 코루틴 사용을 위해 추가
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class InventoryItemInfoUI : MonoBehaviour
{
    [SerializeField] Image _itemIcon;
    [SerializeField] TMP_Text _itemName;
    [SerializeField] TMP_Text _itemDesc;
    [SerializeField] TMP_Text _itemAmountText;
    [SerializeField] TMP_Text _itemTypeText;
    [SerializeField] TMP_Text _itemValue;

    [SerializeField] RectTransform _infoUI;
    CanvasGroup _infoCanvas;
    [SerializeField] RectTransform _itemNameUI;
    [SerializeField] TMP_Text _itemNameUIText;

    [SerializeField] GameObject _useButton;

    GameObject _itemValueObj;
    bool _lockInfo;
    InventorySlotUI _currentItemSlot;
    Coroutine _fadeCoroutine; // 페이드 코루틴 추적용

    const float _yOffset = 140f;
    const float _fadeDuration = 0.01f; // 페이드 시간

    public void Start()
    {
        _lockInfo = false;

        // 코루틴이 실행되려면 UI 오브젝트 자체가 꺼져있으면 안 됨
        if (!_infoUI.gameObject.activeSelf)
        {
            _infoUI.gameObject.SetActive(true);
        }

        _infoCanvas = _infoUI.GetComponent<CanvasGroup>();

        // 시작 시 투명하게 만들고 클릭 차단
        _infoCanvas.alpha = 0f;
        _infoCanvas.interactable = false;
        _infoCanvas.blocksRaycasts = false;

        _itemValueObj = _itemValue.transform.parent.gameObject;
    }

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleSelectedLocaleChanged;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleSelectedLocaleChanged;
    }

    public void LockInfo(InventorySlotUI item)
    {
        _lockInfo = false;
        SetDetails(item);
        _lockInfo = true;
    }

    public void UnlockInfo()
    {
        _lockInfo = false;
        SetDetails(null);
    }

    public void ShowItemName(InventorySlotUI itemSlot)
    {
        if (itemSlot == null || itemSlot?.GetItemInfo() == null)
        {
            if (_itemNameUI.gameObject.activeSelf)
                _itemNameUI.gameObject.SetActive(false);
            return;
        }
        if (!_itemNameUI.gameObject.activeSelf)
            _itemNameUI.gameObject.SetActive(true);

        _itemNameUIText.SetText(itemSlot.GetItemInfo().item.GetLocalizedName());
        _itemNameUI.position = itemSlot.transform.position;
        _itemNameUI.anchoredPosition += new Vector2(0, -_yOffset);
    }

    public void SetDetails(InventorySlotUI itemSlot)
    {
        if (_lockInfo) return;

        _currentItemSlot = itemSlot;
        InventorySlot itemInfo = itemSlot?.GetItemInfo();
        var itemData = itemInfo?.item; // 중복 접근을 막기 위한 캐싱

        // 아이템이 없으면 페이드 아웃 (끄기)
        if (itemData == null)
        {
            if (_infoCanvas.alpha > 0f || _fadeCoroutine != null)
                FadeTo(0f);
            return;
        }

        // 아이템이 있으면 페이드 인 (켜기)
        if (_infoCanvas.alpha < 1f || _fadeCoroutine != null)
            FadeTo(1f);

        _itemIcon.sprite = itemData.icon;
        _itemName.SetText(itemData.GetLocalizedName());
        _itemDesc.SetText(itemData.GetLocalizedDesc());

        // 판매 가능 여부에 따른 UI 처리 최적화
        bool canSell = itemData.canSell;
        bool canUse = (itemData.itemType == ItemType.Usable);

        if (_itemValueObj.activeSelf != canSell)
        {
            _itemValueObj.SetActive(canSell);
        }

        if (canSell)
        {
            _itemValue.SetText(itemData.sellPrice.ToString());
        }

        if (_useButton.activeSelf != canUse)
        {
            _useButton.SetActive(canUse);
        }

        _itemAmountText.SetText($"{itemInfo.count}");
        _itemTypeText.SetText(itemData.GetLocalizedTypeName());
        _itemTypeText.color = GetItemTypeColor(itemData.itemType);

        StartCoroutine(RebuildLayoutRoutine());
    }

    private IEnumerator RebuildLayoutRoutine()
    {
        // UI 컴포넌트들이 크기를 계산할 시간을 한 프레임 줍니다.
        yield return null; // 또는 yield return new WaitForEndOfFrame();
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(_infoUI);
    }

    void HandleSelectedLocaleChanged(Locale locale)
    {
        if (_currentItemSlot == null)
        {
            return;
        }

        InventorySlot itemInfo = _currentItemSlot.GetItemInfo();
        ItemSO itemData = itemInfo?.item;
        if (itemData == null)
        {
            return;
        }

        _itemName.SetText(itemData.GetLocalizedName());
        _itemDesc.SetText(itemData.GetLocalizedDesc());
        _itemTypeText.SetText(itemData.GetLocalizedTypeName());
        LayoutRebuilder.ForceRebuildLayoutImmediate(_infoUI);
    }

    // =========================================
    // CanvasGroup 페이드 코루틴 제어부
    // =========================================
    private void FadeTo(float targetAlpha)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, _fadeDuration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = _infoCanvas.alpha;
        float time = 0f;

        // 켜기 시작할 때 즉시 터치 활성화
        if (targetAlpha > 0f)
        {
            _infoCanvas.blocksRaycasts = true;
            _infoCanvas.interactable = true;
        }

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            _infoCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        _infoCanvas.alpha = targetAlpha;

        // 완전히 꺼졌을 때 터치 비활성화
        if (targetAlpha == 0f)
        {
            _infoCanvas.blocksRaycasts = false;
            _infoCanvas.interactable = false;
        }

        _fadeCoroutine = null;
    }

    public Color GetItemTypeColor(ItemType type)
    {
        return type switch
        {
            // Hex 코드를 Color32로 변환 (R, G, B, Alpha)
            ItemType.Seed => new Color32(255, 239, 0, 255),    // #FFEF00
            ItemType.Usable => new Color32(0, 255, 140, 255),  // #00FF8C
            _ => Color.white // 그 외 기본 색상 (필요시 변경 가능)
        };
    }
}
