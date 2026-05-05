using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class ShopContent : MonoBehaviour
{
    [SerializeField]
    Image _icon;
    [SerializeField]
    TMP_Text _itemNameText;
    [SerializeField]
    TMP_Text _priceText;
    [SerializeField]
    Color _defaultPriceColor = Color.white;
    [SerializeField]
    Color _insufficientPriceColor = Color.red;
    [SerializeField]
    float _blinkInterval = 0.15f;
    [SerializeField]
    int _blinkCount = 4;

    ItemSO _item;
    GoldManager _goldManager;
    InventoryManager _inventoryManager;
    Coroutine _priceBlinkCoroutine;

    public ItemSO Item => _item;

    void Awake()
    {
        _goldManager = FindFirstObjectByType<GoldManager>();
        _inventoryManager = FindFirstObjectByType<InventoryManager>();

        if (_priceText != null)
        {
            _defaultPriceColor = _priceText.color;
        }
    }

    void OnEnable()
    {
        if (_goldManager != null)
        {
            _goldManager.GoldChanged += HandleGoldChanged;
        }

        LocalizationSettings.SelectedLocaleChanged += HandleSelectedLocaleChanged;
    }

    void OnDisable()
    {
        if (_goldManager != null)
        {
            _goldManager.GoldChanged -= HandleGoldChanged;
        }

        LocalizationSettings.SelectedLocaleChanged -= HandleSelectedLocaleChanged;
    }

    public void Initialize(ItemSO item)
    {
        _item = item;

        if (_item == null)
        {
            Debug.LogWarning("[ShopContent] Initialize failed: item is null.", this);
            return;
        }

        if (_icon != null)
        {
            _icon.sprite = _item.icon;
            _icon.enabled = _item.icon != null;
        }

        if (_itemNameText != null)
        {
            UpdateItemNameText();
        }

        if (_priceText != null)
        {
            _priceText.SetText(_item.buyPrice.ToString());
            UpdatePriceColor();
        }
    }

    public void BuyButton()
    {
        if (_item == null)
        {
            Debug.LogWarning("[ShopContent] Buy failed: item is null.", this);
            return;
        }

        if (_goldManager == null)
        {
            _goldManager = FindFirstObjectByType<GoldManager>();
        }

        if (_inventoryManager == null)
        {
            _inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (_goldManager == null || _inventoryManager == null)
        {
            Debug.LogWarning("[ShopContent] Buy failed: manager reference is missing.", this);
            return;
        }

        if (_goldManager.GetGold() < _item.buyPrice)
        {
            StartPriceBlink();
            return;
        }

        if (!_goldManager.TrySpendGold(_item.buyPrice))
        {
            StartPriceBlink();
            return;
        }

        bool itemAdded = _inventoryManager.AddItem(_item, 1);
        if (!itemAdded)
        {
            _goldManager.AddGold(_item.buyPrice);
            Debug.LogWarning($"[ShopContent] Buy failed: inventory is full for {_item.itemName}.", this);
            return;
        }

        UpdatePriceColor();
    }

    void HandleGoldChanged(int currentGold)
    {
        UpdatePriceColor();
    }

    void HandleSelectedLocaleChanged(Locale locale)
    {
        UpdateItemNameText();
    }

    void UpdateItemNameText()
    {
        if (_itemNameText == null || _item == null)
        {
            return;
        }

        _itemNameText.SetText(_item.GetLocalizedName());
    }

    void UpdatePriceColor()
    {
        if (_priceText == null || _item == null)
        {
            return;
        }

        if (_goldManager == null)
        {
            _priceText.color = _defaultPriceColor;
            return;
        }

        _priceText.color = _goldManager.GetGold() >= _item.buyPrice
            ? _defaultPriceColor
            : _insufficientPriceColor;
    }

    void StartPriceBlink()
    {
        if (_priceText == null)
        {
            return;
        }

        if (_priceBlinkCoroutine != null)
        {
            StopCoroutine(_priceBlinkCoroutine);
        }

        _priceBlinkCoroutine = StartCoroutine(BlinkPriceText());
    }

    IEnumerator BlinkPriceText()
    {
        for (int i = 0; i < _blinkCount; i++)
        {
            _priceText.color = _insufficientPriceColor;
            yield return new WaitForSeconds(_blinkInterval);
            _priceText.color = _defaultPriceColor;
            yield return new WaitForSeconds(_blinkInterval);
        }

        UpdatePriceColor();
        _priceBlinkCoroutine = null;
    }
}
