using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField]
    Transform _slotParent;
    [SerializeField]
    InventoryItemInfoUI _infoUI;

    InventorySlotUI[] _slots;

    InventorySlotUI _focusedSlot;
    InventorySlotUI _hoveredSlot;

    void Awake()
    {
        _slots = _slotParent.GetComponentsInChildren<InventorySlotUI>();

        _infoUI.SetColumnCount(_slotParent.GetComponent<GridLayoutGroup>().constraintCount);

        foreach (var slotUI in _slots)
        {
            slotUI.OnSlotClickedEvent += OnFocusSlot;
            slotUI.OnPointerEnterEvent += OnSlotPointerEnter;
            slotUI.OnPointerExitEvent += OnSlotPointerExit;
        }
    }

    public void UpdateUI(List<InventorySlot> currentSlots)
    {
        if (currentSlots == null) return;

        int uiSlotIndex = 0;

        for (int i = 0; i < currentSlots.Count; i++)
        {
            InventorySlot slot = currentSlots[i];
            if (slot != null && !slot.IsEmpty)
            {
                if (uiSlotIndex < _slots.Length)
                {
                    _slots[uiSlotIndex].UpdateSlot(slot);
                    uiSlotIndex++;
                }
            }
        }

        // 아이템을 다 채우고 남은 빈 UI 슬롯들 초기화
        for (int i = uiSlotIndex; i < _slots.Length; i++)
        {
            _slots[i].ClearSlot();
        }
    }

    public void OnFocusSlot(InventorySlotUI clickedSlot)
    {
        if (GameObject.ReferenceEquals(_focusedSlot, clickedSlot)) return;

        if (_focusedSlot != null)
            _focusedSlot.UnfocusSlot();

        clickedSlot.FocusSlot();
        _focusedSlot = clickedSlot;
    }

    public void OnSlotPointerEnter(InventorySlotUI hoveredSlot)
    {
        if (GameObject.ReferenceEquals(_hoveredSlot, hoveredSlot)) return;

        _infoUI.SetDetails(hoveredSlot);
        _hoveredSlot = hoveredSlot;
    }

    public void OnSlotPointerExit(InventorySlotUI clickedSlot)
    {
        _infoUI.SetDetails(null);
        _hoveredSlot = null;
    }

    void OnDestroy()
    {
        if (_slots == null) return;
        foreach (var slotUI in _slots)
        {
            slotUI.OnSlotClickedEvent -= OnFocusSlot;
            slotUI.OnPointerEnterEvent -= OnSlotPointerEnter;
            slotUI.OnPointerExitEvent -= OnSlotPointerExit;
        }
    }
}