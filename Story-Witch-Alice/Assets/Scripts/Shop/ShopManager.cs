using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店管理器 — 遗民商店 UI 与交互。
/// 
/// 布局：左 1/4 遗民立绘 + 名称，
///       右 3/4 物品列表 + 思绪余额。
/// 点击物品直接购买（无确认弹窗）。
/// 
/// 扩展预留：remnantID 过滤、好感度解锁、折扣。
/// </summary>
public class ShopManager : MonoBehaviour
{
    // ============================
    // 左 1/4 — 遗民区域
    // ============================
    [Header("左 1/4 - 遗民")]
    public GameObject remnantPanel;           // 遗民面板根
    public Image remnantPortraitImage;        // 立绘
    public Text remnantNameText;              // 名称
    public Text remnantDescText;              // 描述

    // ============================
    // 右 3/4 — 商店区域
    // ============================
    [Header("右 3/4 - 商店")]
    public GameObject shopPanel;              // 商店面板根
    public Text thoughtsBalanceText;          // 思绪余额

    [Header("物品列表")]
    public Transform itemListParent;          // Content
    public GameObject itemPrefab;             // 物品条目预制体

    [Header("商店数据")]
    public RemnantData currentRemnant;        // 当前遗民
    public List<ShopItemData> shopItems;      // 当前商店物品列表

    // 备忘录：购买的物品 ID，购买后灰显
    private HashSet<string> purchasedItemIDs = new HashSet<string>();

    void Start()
    {
        if (remnantPanel != null) remnantPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnThoughtsChanged += UpdateThoughtsDisplay;
    }

    // ======================== 打开 / 关闭 ========================

    public void OpenShop(RemnantData remnant)
    {
        currentRemnant = remnant;

        // 左 1/4
        if (remnantPanel != null) remnantPanel.SetActive(true);
        if (remnantPortraitImage != null && remnant.portrait != null)
            remnantPortraitImage.sprite = remnant.portrait;
        if (remnantNameText != null) remnantNameText.text = remnant.remnantName;
        if (remnantDescText != null) remnantDescText.text = remnant.description;

        // 右 3/4
        if (shopPanel != null) shopPanel.SetActive(true);
        int thoughts = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentThoughts : 0;
        UpdateThoughtsDisplay(thoughts);
        DisplayAllItems();
    }

    public void CloseShop()
    {
        if (remnantPanel != null) remnantPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    // ======================== 展示所有物品 ========================

    void DisplayAllItems()
    {
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        foreach (var item in shopItems)
        {
            if (item == null) continue;
            if (!string.IsNullOrEmpty(item.remnantID) && currentRemnant != null
                && item.remnantID != currentRemnant.remnantID) continue;

            GameObject entry = Instantiate(itemPrefab, itemListParent);
            ItemEntryUI ui = entry.GetComponent<ItemEntryUI>();
            if (ui != null) ui.Setup(item, this);

            // 已购买 → 灰显
            bool bought = purchasedItemIDs.Contains(item.itemID);
            Image bg = entry.GetComponent<Image>();
            if (bg != null) bg.color = bought ? Color.gray : Color.white;
            Button btn = entry.GetComponent<Button>();
            if (btn != null) btn.interactable = !bought;
        }
    }

    // ======================== 直接购买 ========================

    public void BuyItem(ShopItemData item)
    {
        if (item == null || purchasedItemIDs.Contains(item.itemID)) return;

        int price = item.hasDiscount
            ? Mathf.RoundToInt(item.price * (1f - item.discountRate))
            : item.price;

        if (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendThoughts(price))
        {
            // 解锁元素
            if (item.rewardElement != null && AlchemyManager.Instance != null)
            {
                AlchemyManager.Instance.OnElementCrafted(item.rewardElement);
                Debug.Log($"购买成功！解锁：{item.rewardElement.elementName}");
            }

            purchasedItemIDs.Add(item.itemID);
            OnItemPurchased?.Invoke(item);

            // 刷新列表（已购变灰）
            DisplayAllItems();
        }
    }

    // ======================== UI ========================

    void UpdateThoughtsDisplay(int current)
    {
        if (thoughtsBalanceText != null)
            thoughtsBalanceText.text = $"思绪：{current}";
    }

    // ======================== 事件 ========================

    public System.Action<ShopItemData> OnItemPurchased;
}

/// <summary>
/// 单个物品条目（挂载到 itemPrefab 上）
/// </summary>
public class ItemEntryUI : MonoBehaviour
{
    public Image iconImage;
    public Text nameText;
    public Text priceText;
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
