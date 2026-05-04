using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] Image _img;
    [SerializeField] TMP_Text _amount;
    [SerializeField] GameObject _focusEffect;

    InventorySlot _currentItem;

    public event Action<InventorySlotUI> OnSlotClickedEvent;
    public event Action<InventorySlotUI> OnPointerEnterEvent;
    public event Action<InventorySlotUI> OnPointerExitEvent;

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
    }

    public void FocusSlot()
    {
        if (!_focusEffect.activeSelf && _currentItem != null) _focusEffect.SetActive(true);
    }

    public void UnfocusSlot()
    {
        if (_focusEffect.activeSelf) _focusEffect.SetActive(false);
    }

    public InventorySlot GetItemInfo() => _currentItem;

    public void OnSlotClicked() => OnSlotClickedEvent?.Invoke(this);
    public void OnPointerEnter() => OnPointerEnterEvent?.Invoke(this);
    public void OnPointerExit() => OnPointerExitEvent?.Invoke(this);
}