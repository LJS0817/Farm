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
    public string itemDesc;
    public ItemType itemType;
    public Sprite icon;
    public bool stackable = true;
    public int maxStack = 99;

    //SHOP
    public int buyPrice;
    public int sellPrice;
    public bool canBuy;
    public bool canSell;
}
