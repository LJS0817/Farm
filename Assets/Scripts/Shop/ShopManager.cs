using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField]
    RectTransform _shopWindow;

    public ShopContent shopContents;
    public Transform shopContensParentLocation;
    public ItemSO[] shopItemDatabase;

    void Start()
    {
        RefreshShop();
        CloseShop();
    }

    public List<ItemSO> GetShopItems()
    {
        List<ItemSO> shopItems = new();

        foreach (ItemSO item in shopItemDatabase)
        {
            if (item == null || !item.canBuy)
            {
                continue;
            }

            shopItems.Add(item);
        }

        return shopItems;
    }

    public void RefreshShop()
    {
        ClearShopContents();

        List<ItemSO> shopItems = GetShopItems();

        foreach (ItemSO item in shopItems)
        {
            ShopContent newShopContent = Instantiate(shopContents, shopContensParentLocation);
            newShopContent.Initialize(item);
        }
    }

    void ClearShopContents()
    {
        for (int i = shopContensParentLocation.childCount - 1; i >= 0; i--)
        {
            Destroy(shopContensParentLocation.GetChild(i).gameObject);
        }
    }

    public void OpenShop()
    {
        if (!TryGetShopWindow(out RectTransform shopWindow))
        {
            return;
        }

        shopWindow.offsetMin = Vector2.zero;
        shopWindow.offsetMax = Vector2.zero;
    }

    public void CloseShop()
    {
        if (!TryGetShopWindow(out RectTransform shopWindow))
        {
            return;
        }

        shopWindow.offsetMin = new Vector2(2000f, 2000f);
        shopWindow.offsetMax = new Vector2(2000f, 2000f);
    }

    bool TryGetShopWindow(out RectTransform shopWindow)
    {
        shopWindow = _shopWindow;

        if (shopWindow != null)
        {
            return true;
        }

        shopWindow = GetComponent<RectTransform>();

        if (shopWindow == null)
        {
            Debug.LogWarning("[ShopManager] Shop window RectTransform reference is missing.", this);
            return false;
        }

        return true;
    }
}
