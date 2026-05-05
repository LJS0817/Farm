using UnityEngine;
using UnityEngine.Localization.Settings;

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
    [Header("Localization")]
    public string TableName;
    public string itemNameEntryKey;
    public string itemDescEntryKey;
    public string itemTypeEntryKey;

    public ItemType itemType;
    public Sprite icon;
    public bool stackable = true;
    public int maxStack = 99;

    //SHOP
    public int buyPrice;
    public int sellPrice;
    public bool canBuy;
    public bool canSell;

    public string GetLocalizedName()
    {
        if (string.IsNullOrEmpty(TableName) || string.IsNullOrEmpty(itemNameEntryKey))
        {
            return itemName;
        }

        string localizedName = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, itemNameEntryKey);
        return string.IsNullOrEmpty(localizedName) ? itemName : localizedName;
    }

    public string GetLocalizedDesc()
    {
        if (string.IsNullOrEmpty(TableName) || string.IsNullOrEmpty(itemDescEntryKey))
        {
            return itemDesc;
        }

        string localizedDesc = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, itemDescEntryKey);
        return string.IsNullOrEmpty(localizedDesc) ? itemDesc : localizedDesc;
    }

    public string GetLocalizedTypeName()
    {
        string fallback = GetFallbackTypeName();
        if (string.IsNullOrEmpty(TableName) || string.IsNullOrEmpty(itemTypeEntryKey))
        {
            return fallback;
        }

        string localizedTypeName = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, itemTypeEntryKey);
        return string.IsNullOrEmpty(localizedTypeName) ? fallback : localizedTypeName;
    }

    private string GetFallbackTypeName()
    {
        return itemType switch
        {
            ItemType.Seed => "씨앗",
            ItemType.Usable => "사용 아이템",
            _ => "알 수 없음"
        };
    }
}
