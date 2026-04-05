using UnityEngine;

public enum ItemType
{
    Seed,
    Usable
}

[CreateAssetMenu(menuName = "Game/Item Data")]
public class ItemSO : ScriptableObject
{
    public int itemId;
    public string itemName;
    public ItemType itemType;
    public Sprite icon;
    public bool stackable = true;
    public int maxStack = 99;
}
