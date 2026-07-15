using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 商店管理器 — 遗民商店 UI 与交互。
/// 
/// 布局：左 1/4 遗民立绘，
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
    public GameObject remnantPanel;
    public Image remnantPortraitImage;

    // ============================
    // 右 3/4 — 商店区域
    // ============================
    [Header("右 3/4 - 商店")]
    public GameObject shopPanel;
    public TextMeshProUGUI thoughtsBalanceText;

    [Header("物品列表")]
    public Transform itemListParent;
    public GameObject goodsPrefab;

    [Header("商店数据")]
    public RemnantData currentRemnant;
    public List<ShopItemData> shopItems;

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
        DisplayAllItems();
    }

    [ContextMenu("Open Shop (Debug)")]
    public void OpenShopDebug()
    {
        if (currentRemnant != null)
            OpenShop(currentRemnant);
    }

    public void CloseShop()
    {
        if (remnantPanel != null) remnantPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    void DisplayAllItems()
    {
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        foreach (var item in shopItems)
        {
            if (item == null) continue;
            if (!string.IsNullOrEmpty(item.remnantID) && currentRemnant != null
                && item.remnantID != currentRemnant.remnantID) continue;

            GameObject entry = Instantiate(goodsPrefab, itemListParent);
            ItemEntryUI ui = entry.GetComponent<ItemEntryUI>();
            if (ui != null) ui.Setup(item, this);

            bool bought = purchasedItemIDs.Contains(item.itemID);
            Image bg = entry.GetComponent<Image>();
            if (bg != null) bg.color = bought ? Color.gray : Color.white;
            Button btn = entry.GetComponent<Button>();
            if (btn != null) btn.interactable = !bought;
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
            DisplayAllItems();
        }
    }

    void UpdateThoughtsDisplay(int current)
    {
        if (thoughtsBalanceText != null)
            thoughtsBalanceText.text = $"思绪：{current}";
    }

    public System.Action<ShopItemData> OnItemPurchased;
}
