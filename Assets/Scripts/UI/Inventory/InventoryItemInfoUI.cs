using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemInfoUI : MonoBehaviour
{
    [SerializeField]
    TMP_Text _itemName;
    [SerializeField]
    TMP_Text _itemDesc;
    [SerializeField]
    TMP_Text _itemAmount;
    [SerializeField]
    Image _itemImg;

    [SerializeField]
    Transform _typeParent;

    readonly Color _enableColor = new Color(0.3725491f, 0.7647059f, 0.4416048f);
    readonly Color _disableColor = new Color(1f, 1f, 1f);

    public void SetDetails(InventorySlot item)
    {
        _itemName.SetText(item.item.itemName);
        //_itemDesc.SetText(item.item.itemDesc);
        _itemAmount.SetText($"x {item.count}");
        _itemImg.sprite = item.item.icon;
        EnableTypeUI(item.item.itemType);
    }

    void EnableTypeUI(ItemType t)
    {
        Image img = null;
        for (int i = 0; i < _typeParent.childCount; i++)
        {
            img = _typeParent.GetChild(i).GetComponent<Image>();
            if((int)t == i) img.color = _enableColor;
            else if(img.color.r != _disableColor.r) img.color = _disableColor;
        }
    }
}
