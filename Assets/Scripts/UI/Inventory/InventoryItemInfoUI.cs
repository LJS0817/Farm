using TMPro;
using UnityEngine;
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
    [SerializeField] RectTransform _itemNameUI;
    [SerializeField] TMP_Text _itemNameUIText;

    [SerializeField] GameObject _useButton;

    GameObject _itemValueObj;
    bool _lockInfo;

    const float _yOffset = 140f;

    public void Start()
    {
        _lockInfo = false;
        _infoUI.gameObject.SetActive(false);
        _itemValueObj = _itemValue.transform.parent.gameObject;
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
        if(itemSlot == null || itemSlot?.GetItemInfo() == null)
        {
            if (_itemNameUI.gameObject.activeSelf)
                _itemNameUI.gameObject.SetActive(false);
            return;
        }
        if(!_itemNameUI.gameObject.activeSelf) 
            _itemNameUI.gameObject.SetActive(true);
        _itemNameUIText.SetText(itemSlot.GetItemInfo().item.itemName);
        _itemNameUI.position = itemSlot.transform.position;
        _itemNameUI.anchoredPosition += new Vector2(0, -_yOffset);
    }

    public void SetDetails(InventorySlotUI itemSlot)
    {
        if (_lockInfo) return;

        InventorySlot itemInfo = itemSlot?.GetItemInfo();
        var itemData = itemInfo?.item; // 중복 접근을 막기 위한 캐싱

        // 아이템이 없으면 UI 끄기
        if (itemData == null)
        {
            if (_infoUI.gameObject.activeSelf)
                _infoUI.gameObject.SetActive(false);
            return;
        }

        // 아이템이 있으면 UI 켜기
        if (!_infoUI.gameObject.activeSelf)
            _infoUI.gameObject.SetActive(true);

        _itemIcon.sprite = itemData.icon;
        _itemName.SetText(itemData.itemName);
        _itemDesc.SetText(itemData.itemDesc);

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

        _itemAmountText.SetText($"{itemInfo.count}개");
        _itemTypeText.SetText(GetItemTypeKorean(itemData.itemType));
        _itemTypeText.color = GetItemTypeColor(itemData.itemType);

        LayoutRebuilder.ForceRebuildLayoutImmediate(_infoUI);
    }

    public string GetItemTypeKorean(ItemType type)
    {
        return type switch
        {
            ItemType.Seed => "씨앗",
            ItemType.Usable => "사용 아이템",
            _ => "알 수 없음"
        };
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