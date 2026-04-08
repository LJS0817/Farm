using System; // Action을 사용하기 위해 필요
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    private Image _img;
    private TMP_Text _amount;
    private InventorySlot _currentItem;

    public event Action<InventorySlot> OnSlotClickedEvent;

    void Awake()
    {
        _img = transform.GetChild(0).GetComponent<Image>();
        _amount = transform.GetChild(1).GetComponent<TMP_Text>();
    }

    public void UpdateSlot(InventorySlot item)
    {
        if (item != null && item.item != null)
        {
            _currentItem = item;
            _img.sprite = item.item.icon;
            _img.enabled = true;
            _amount.text = item.count > 1 ? $"x {item.count}" : "";
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        _currentItem = null;
        if (_img != null) { _img.sprite = null; _img.enabled = false; }
        if (_amount != null) { _amount.text = ""; }
    }

    // 유니티 Button의 OnClick에 연결될 함수
    public void OnSlotClicked()
    {
        if (_currentItem != null)
        {
            OnSlotClickedEvent?.Invoke(_currentItem);
        }
    }
}