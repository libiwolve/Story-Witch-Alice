using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 单个物品条目 UI（挂载到 GoodsPrefab 上）
/// </summary>
public class ItemEntryUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    ShopItemData data;
    ShopManager manager;

    public void Setup(ShopItemData item, ShopManager mgr)
    {
        data = item;
        manager = mgr;

        if (iconImage != null && item.icon != null) iconImage.sprite = item.icon;
        if (nameText != null) nameText.text = item.itemName;
        if (priceText != null) priceText.text = $"{item.price} 思绪";

        if (buyButton != null)
            buyButton.onClick.AddListener(() => manager.BuyItem(item));
    }
}
