using UnityEngine;
using System.Collections.Generic;

// 단순 인벤토리 저장소.
// 수확 보상을 슬롯에 적재하고 현재 상태를 문자열 또는 로그로 확인할 수 있다.
public class InventoryManager : MonoBehaviour
{
    public ItemSO[] itemDatabase;
    public int slotCount = 20;

    public List<InventorySlot> slots = new();
    Dictionary<string, ItemSO> _items = new Dictionary<string, ItemSO>();

    [SerializeField]
    InventoryUI _inventoryUI; // UI를 제어
    [SerializeField]
    ItemFeedbackUIController _itemFeedback;
    [SerializeField]
    Transform _feedbackOffset;

    private void Awake()
    {
        InitializeSlots();
    }

    private void Start()
    {
        RefreshUI();
    }

    private void InitializeSlots()
    {
        if (slots.Count > 0)
        {
            return;
        }

        for (int i = 0; i < slotCount; i++)
        {
            slots.Add(new InventorySlot());
        }

        foreach (ItemSO ele in itemDatabase) _items.Add(ele.itemName, ele);
    }

    private void RefreshUI()
    {
        if (_inventoryUI != null)
        {
            _inventoryUI.UpdateUI(slots);
        }
    }

    public ItemSO GetItemSoWithName(string iName) { return _items.GetValueOrDefault(iName); }

    // 아이템을 기존 스택 또는 빈 슬롯에 추가한다.
    public bool AddItem(ItemSO item, int amount = 1)
    {
        if (item == null)
        {
            Debug.LogWarning("[InventoryManager] AddItem failed: item is null.", this);
            return false;
        }

        if (amount <= 0)
        {
            Debug.LogWarning($"[InventoryManager] AddItem failed: invalid amount {amount}.", this);
            return false;
        }

        int remainingAmount = amount;

        if (item.stackable)
        {
            foreach (InventorySlot slot in slots)
            {
                if (!slot.CanStack(item))
                {
                    continue;
                }

                remainingAmount = slot.AddAmount(remainingAmount);

                if (remainingAmount <= 0)
                {
                    Debug.Log($"[InventoryManager] Added {amount} x {item.itemName}.", this);
                    _itemFeedback.ShowItemFeedback(_feedbackOffset.position, item.icon, amount);
                    RefreshUI(); // 아이템 획득 완료 후 UI 갱신
                    return true;
                }
            }
        }

        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty)
            {
                continue;
            }

            int amountToSet = item.stackable ? Mathf.Min(item.maxStack, remainingAmount) : 1;
            slot.Set(item, amountToSet);
            remainingAmount -= amountToSet;

            if (remainingAmount <= 0)
            {
                Debug.Log($"[InventoryManager] Added {amount} x {item.itemName}.", this);
                _itemFeedback.ShowItemFeedback(_feedbackOffset.position, item.icon, amount);
                RefreshUI(); // 아이템 획득 완료 후 UI 갱신
                return true;
            }
        }

        int addedAmount = amount - remainingAmount;
        Debug.LogWarning(
            $"[InventoryManager] Inventory is full. Added {addedAmount}/{amount} x {item.itemName}.",
            this);
        _itemFeedback.ShowItemFeedback(_feedbackOffset.position, item.icon, addedAmount);
        RefreshUI(); // 일부만 들어갔을 경우를 대비해 갱신
        return false;
    }

    public bool HasItem(ItemSO item, int amount = 1)
    {
        int totalCount = 0;
        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty && slot.item == item)
            {
                totalCount += slot.count;
                if (totalCount >= amount) return true;
            }
        }
        return false;
    }

    public bool RemoveItem(string itemName) { return RemoveItem(GetItemSoWithName(itemName)); }

    public bool RemoveItem(ItemSO item, int amount = 1)
    {
        if (!HasItem(item, amount)) return false;

        int remainingToRemove = amount;

        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (!slots[i].IsEmpty && slots[i].item == item)
            {
                if (slots[i].count > remainingToRemove)
                {
                    _itemFeedback.ShowItemFeedback(_feedbackOffset.position, item.icon, -remainingToRemove);
                    slots[i].count -= remainingToRemove;
                    remainingToRemove = 0;

                }
                else
                {
                    _itemFeedback.ShowItemFeedback(_feedbackOffset.position, item.icon, -slots[i].count);
                    remainingToRemove -= slots[i].count;
                    slots[i].item = null;
                    slots[i].count = 0;
                }

                if (remainingToRemove <= 0) break;
            }
        }

        RefreshUI(); // 아이템 소모 완료 후 UI 갱신
        return true;
    }

    // 현재 인벤토리 상태를 콘솔에 출력한다.
    public void LogInventory()
    {
        InitializeSlots();

        Debug.Log(GetInventoryLog(), this);
    }

    // 현재 인벤토리 상태를 사람이 읽기 쉬운 문자열로 반환한다.
    public string GetInventoryLog()
    {
        InitializeSlots();

        List<string> lines = new()
        {
            $"[InventoryManager] Current Inventory ({slots.Count} slots)"
        };

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot == null || slot.IsEmpty)
            {
                lines.Add($"Slot {i}: Empty");
                continue;
            }

            lines.Add($"Slot {i}: {slot.item.itemName} x {slot.count}");
        }

        return string.Join("\n", lines);
    }
}
