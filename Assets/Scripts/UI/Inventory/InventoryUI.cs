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

    public void ResetInventoryUIState()
    {
        // 1. 선택(포커스)된 슬롯 초기화
        if (_focusedSlot != null)
        {
            _focusedSlot.UnfocusSlot(); // 슬롯 포커스 알파값 0으로
            _focusedSlot = null;        // 변수 비우기
        }

        // 2. 마우스 호버 상태 초기화
        if (_hoveredSlot != null)
        {
            // OnPointerExit()을 강제로 호출하면 슬롯의 호버 알파값이 0이 되고, 
            // 연결된 이벤트를 통해 _infoUI 데이터도 지워지며 _hoveredSlot 변수도 null로 정리됩니다.
            _hoveredSlot.OnPointerExit();
        }

        // 3. 우측 정보창(Info UI) 잠금 해제 및 데이터 초기화 (페이드 아웃)
        if (_infoUI != null)
        {
            _infoUI.UnlockInfo();
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