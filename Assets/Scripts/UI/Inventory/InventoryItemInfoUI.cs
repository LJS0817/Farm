using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemInfoUI : MonoBehaviour
{
    [SerializeField] TMP_Text _itemName;
    [SerializeField] TMP_Text _itemDesc;

    [SerializeField] RectTransform _infoUI;
    [SerializeField] GameObject _downArrow;
    [SerializeField] GameObject _upArrow;

    const float _yOffset = 175f;

    int _columnCount = 0;

    public void Start()
    {
        _infoUI.gameObject.SetActive(false);
    }

    public void SetColumnCount(int n) { _columnCount = n; }

    public void SetDetails(InventorySlotUI itemSlot)
    {
        if (itemSlot == null || itemSlot.GetItemInfo() == null)
        {
            _infoUI.gameObject.SetActive(false);
            return;
        }
        _infoUI.gameObject.SetActive(true);

        _downArrow.SetActive(false);
        _upArrow.SetActive(false);

        _infoUI.position = itemSlot.transform.position;

        int slotIndex = itemSlot.transform.GetSiblingIndex();

        if (slotIndex < _columnCount)
        {
            _upArrow.SetActive(true);
            _infoUI.anchoredPosition += new Vector2(0, -_yOffset);
        }
        else
        {
            _downArrow.SetActive(true);
            _infoUI.anchoredPosition += new Vector2(0, _yOffset);
        }

        InventorySlot item = itemSlot.GetItemInfo();
        if (item != null && item.item != null)
        {
            _itemName.SetText(item.item.itemName);
            _itemDesc.SetText("아이템 설명 1234\n아이템 설명 1234567");
            //_itemDesc.SetText(item.item.itemDesc);
        }
    }
}