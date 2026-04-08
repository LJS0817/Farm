using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField]
    Transform _slotParent;
    [SerializeField]
    InventoryItemInfoUI _infoUI;

    List<InventorySlotUI> _slots;

    // Manager가 먼저 호출할 수 있으므로 Awake에서 리스트를 준비합니다.
    void Awake()
    {
        _slots = new List<InventorySlotUI>();

        for (int pIdx = 0; pIdx < _slotParent.childCount; pIdx++)
        {
            Transform row = _slotParent.GetChild(pIdx);
            for (int cIdx = 0; cIdx < row.childCount; cIdx++)
            {
                InventorySlotUI slotUI = row.GetChild(cIdx).GetComponent<InventorySlotUI>();
                if (slotUI != null)
                {
                    _slots.Add(slotUI);
                    slotUI.OnSlotClickedEvent += ShowDetails;
                }
            }
        }
    }

    // InventoryManager에서 인벤토리 데이터를 넘겨주며 호출하는 함수
    public void UpdateUI(List<InventorySlot> currentSlots)
    {
        if (_slots == null || currentSlots == null) return;

        // 1. 전달받은 데이터에서 빈칸을 제외한 실제 아이템만 추출 (시각적 압축)
        List<InventorySlot> validItems = new List<InventorySlot>();
        foreach (InventorySlot slot in currentSlots)
        {
            if (slot != null && !slot.IsEmpty)
            {
                validItems.Add(slot);
            }
        }

        // 2. 찾아둔 _slots 리스트에 차례대로 데이터 적용
        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < validItems.Count)
            {
                _slots[i].UpdateSlot(validItems[i]);
            }
            else
            {
                _slots[i].ClearSlot();
            }
        }
    }

    public void ShowDetails(InventorySlot clickedSlot)
    {
        Debug.Log($"InventoryUI에서 처리 중: {clickedSlot.item.itemName}의 상세 정보를 표시합니다.");

        _infoUI.SetDetails(clickedSlot);
    }
}