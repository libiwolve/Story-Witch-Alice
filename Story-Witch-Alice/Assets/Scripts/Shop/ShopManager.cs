using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 商店管理器 — 手动布局模式。
/// 在场景中直接摆好商品，所见即所得。
/// 每个商品子对象挂 ItemEntryUI。
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("左 1/4 - 遗民")]
    public GameObject remnantPanel;
    public Image remnantPortraitImage;

    [Header("右 3/4 - 商店")]
    public GameObject shopPanel;
    public TextMeshProUGUI thoughtsBalanceText;
    public Transform itemListParent;     // Content，下面放你手动摆好的商品

    [Header("商店数据")]
    public RemnantData currentRemnant;
    public List<ShopItemData> shopItems; // 与场景中商品顺序对应

    private HashSet<string> purchasedItemIDs = new HashSet<string>();

    void Start()
    {
        if (remnantPanel != null) remnantPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnThoughtsChanged += UpdateThoughtsDisplay;
    }

    public void OpenShop(RemnantData remnant)
    {
        currentRemnant = remnant;

        if (remnantPanel != null) remnantPanel.SetActive(true);
        if (remnantPortraitImage != null && remnant.portrait != null)
            remnantPortraitImage.sprite = remnant.portrait;

        if (shopPanel != null) shopPanel.SetActive(true);
        int thoughts = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentThoughts : 0;
        UpdateThoughtsDisplay(thoughts);
        RefreshItemStates();
    }

    [ContextMenu("Open Shop (Debug)")]
    public void OpenShopDebug()
    {
        if (currentRemnant != null) OpenShop(currentRemnant);
    }

    public void CloseShop()
    {
        if (remnantPanel != null) remnantPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    /// <summary>
    /// 不创建/销毁，只更新现有子对象的数据和状态
    /// </summary>
    void RefreshItemStates()
    {
        for (int i = 0; i < itemListParent.childCount && i < shopItems.Count; i++)
        {
            GameObject child = itemListParent.GetChild(i).gameObject;
            ShopItemData item = shopItems[i];

            if (item == null) continue;

            ItemEntryUI ui = child.GetComponent<ItemEntryUI>();
            if (ui != null) ui.Setup(item, this);

            bool bought = purchasedItemIDs.Contains(item.itemID);
            Image bg = child.GetComponent<Image>();
            if (bg != null) bg.color = bought ? Color.gray : Color.white;
            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.interactable = !bought;
            child.SetActive(!bought || true); // 已购隐藏
        }
    }

    public void BuyItem(ShopItemData item)
    {
        if (item == null || purchasedItemIDs.Contains(item.itemID)) return;

        int price = item.hasDiscount
            ? Mathf.RoundToInt(item.price * (1f - item.discountRate))
            : item.price;

        if (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendThoughts(price))
        {
            if (item.rewardElement != null && AlchemyManager.Instance != null)
            {
                AlchemyManager.Instance.OnElementCrafted(item.rewardElement);
                Debug.Log($"购买成功！解锁：{item.rewardElement.elementName}");
            }

            purchasedItemIDs.Add(item.itemID);
            OnItemPurchased?.Invoke(item);
            RefreshItemStates();
        }
    }

    void UpdateThoughtsDisplay(int current)
    {
        if (thoughtsBalanceText != null)
            thoughtsBalanceText.text = $"思绪：{current}";
    }

    public System.Action<ShopItemData> OnItemPurchased;
}
