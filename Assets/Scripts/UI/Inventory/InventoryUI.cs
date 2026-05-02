using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] Transform _slotParent;
    [SerializeField] InventoryItemInfoUI _infoUI;
    [SerializeField] AgentActionController _agent;

    InventorySlotUI[] _slots;
    InventorySlotUI _focusedSlot;
    InventorySlotUI _hoveredSlot;

    void Awake()
    {
        _slots = _slotParent.GetComponentsInChildren<InventorySlotUI>();

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
        int maxSlots = _slots.Length;

        foreach (var slot in currentSlots)
        {
            if (slot != null && !slot.IsEmpty && uiSlotIndex < maxSlots)
            {
                _slots[uiSlotIndex].UpdateSlot(slot);
                uiSlotIndex++;
            }
        }

        // 아이템을 다 채우고 남은 빈 UI 슬롯들 초기화
        for (int i = uiSlotIndex; i < maxSlots; i++)
        {
            _slots[i].ClearSlot();
        }

        if (_focusedSlot != null)
        {
            if (_focusedSlot.IsEmptySlot())
            {
                _focusedSlot.UnfocusSlot();
                _focusedSlot = null;
                _infoUI.UnlockInfo();
            }
            else
            {
                _infoUI.LockInfo(_focusedSlot);
            }
        }
        else if (_hoveredSlot != null)
        {
            if (_hoveredSlot.IsEmptySlot())
            {
                _infoUI.SetDetails(null);
                _hoveredSlot = null;
            }
            else
            {
                _infoUI.SetDetails(_hoveredSlot);
            }
        }
    }

    public void OnFocusSlot(InventorySlotUI clickedSlot)
    {
        if (_focusedSlot == clickedSlot) return; // Unity에서는 오버로딩된 == 연산자 사용이 안전함

        if (_focusedSlot != null)
            _focusedSlot.UnfocusSlot();

        if (clickedSlot.IsEmptySlot())
        {
            _focusedSlot = null;
            _infoUI.UnlockInfo();
            return;
        }

        clickedSlot.FocusSlot();
        _infoUI.LockInfo(clickedSlot);
        _focusedSlot = clickedSlot;
    }

    public void OnSlotPointerEnter(InventorySlotUI hoveredSlot)
    {
        if (_hoveredSlot == hoveredSlot) return;

        _infoUI.SetDetails(hoveredSlot);
        _infoUI.ShowItemName(hoveredSlot);
        _hoveredSlot = hoveredSlot;
    }

    public void OnSlotPointerExit(InventorySlotUI clickedSlot)
    {
        _infoUI.SetDetails(null);
        _infoUI.ShowItemName(null);
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

    // 현재 포커스된 슬롯의 아이템 데이터를 반환
    public InventorySlot GetFocusedSlotItem() => _focusedSlot?.GetItemInfo();
}