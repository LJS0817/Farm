using UnityEngine;
public class InventorySlot
{
    public ItemSO item;
    public int count;

    public bool IsEmpty => item == null || count <= 0;

    public void Clear()
    {
        item = null;
        count = 0;
    }

    public bool CanStack(ItemSO targetItem)
    {
        if (item == null || targetItem == null)
        {
            return false;
        }

        return item == targetItem && item.stackable && count < item.maxStack;
    }

    public int AddAmount(int amount)
    {
        if (item == null || amount <= 0)
        {
            return amount;
        }

        int spaceLeft = item.maxStack - count;
        int added = Mathf.Min(spaceLeft, amount);
        count += added;
        return amount - added;
    }

    public int RemoveAmount(int amount)
    {
        if (item == null || amount <= 0)
        {
            return 0;
        }

        int removed = Mathf.Min(count, amount);
        count -= removed;

        if (count <= 0)
        {
            Clear();
        }

        return removed;
    }

    public void Set(ItemSO newItem, int amount)
    {
        item = newItem;
        count = amount;
    }
}
