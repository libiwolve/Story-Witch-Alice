using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("左 1/4 - 遗民")]
    public GameObject remnantPanel;
    public Image remnantPortraitImage;

    [Header("右 3/4 - 商店")]
    public GameObject shopPanel;
    public TextMeshProUGUI thoughtsBalanceText;
    public Transform itemListParent;

    [Header("商店数据")]
    public RemnantData currentRemnant;
    public List<ShopItemData> shopItems;

    void Start()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnThoughtsChanged += UpdateThoughtsDisplay;
    }

    public void OpenShop(RemnantData remnant)
    {
        currentRemnant = remnant;

        if (remnantPortraitImage != null && remnant.portrait != null)
            remnantPortraitImage.sprite = remnant.portrait;

        int thoughts = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentThoughts : 0;
        UpdateThoughtsDisplay(thoughts);
        RefreshItemStates();
    }

    void RefreshItemStates()
    {
        for (int i = 0; i < itemListParent.childCount && i < shopItems.Count; i++)
        {
            GameObject child = itemListParent.GetChild(i).gameObject;
            ShopItemData item = shopItems[i];
            if (item == null) continue;

            ItemEntryUI ui = child.GetComponent<ItemEntryUI>();
            if (ui != null) ui.Setup(item, this);

            bool bought = item.rewardElement != null && AlchemyManager.Instance != null
                && AlchemyManager.Instance.IsElementUnlocked(item.rewardElement.elementID);

            Image bg = child.GetComponent<Image>();
            if (bg != null) bg.color = bought ? Color.gray : Color.white;

            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.interactable = !bought;

            child.SetActive(!bought);
        }
    }

    public void BuyItem(ShopItemData item)
    {
        if (item == null) return;
        if (item.rewardElement != null && AlchemyManager.Instance != null
            && AlchemyManager.Instance.IsElementUnlocked(item.rewardElement.elementID))
            return;

        int price = item.hasDiscount
            ? Mathf.RoundToInt(item.price * (1f - item.discountRate))
            : item.price;

        if (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendThoughts(price))
        {
            if (item.rewardElement != null && AlchemyManager.Instance != null)
            {
                AlchemyManager.Instance.OnElementCrafted(item.rewardElement);

                if (AlchemyManager.Instance.thoughtOrbit != null)
                    AlchemyManager.Instance.thoughtOrbit.AddToFront(item.rewardElement);
            }

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