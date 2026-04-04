using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
     public ItemSO[] itemDatabase;
    public int slotCount = 20;

    public List<InventorySlot> slots = new();

    private void Awake()
    {
        InitializeSlots();
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
    }

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
                return true;
            }
        }

        int addedAmount = amount - remainingAmount;
        Debug.LogWarning(
            $"[InventoryManager] Inventory is full. Added {addedAmount}/{amount} x {item.itemName}.",
            this);
        return false;
    }

    public void LogInventory()
    {
        InitializeSlots();

        Debug.Log(GetInventoryLog(), this);
    }

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
