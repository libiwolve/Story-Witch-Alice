using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 单个物品条目 UI — 使用 IPointerClickHandler 直接响应点击
/// </summary>
public class ItemEntryUI : MonoBehaviour, IPointerClickHandler
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;

    ShopItemData data;
    ShopManager manager;

    void Start()
    {
        // 确保场景中有 EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // 确保父 Canvas 有 GraphicRaycaster
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    public void Setup(ShopItemData item, ShopManager mgr)
    {
        data = item;
        manager = mgr;

        // 自动找子组件
        if (iconImage == null) iconImage = GetComponentInChildren<Image>();
        if (nameText == null) nameText = GetComponentInChildren<TextMeshProUGUI>();
        if (priceText == null) priceText = GetComponentInChildren<TextMeshProUGUI>();

        if (iconImage != null && item.icon != null) iconImage.sprite = item.icon;
        if (nameText != null) nameText.text = item.itemName;
        if (priceText != null) priceText.text = $"{item.price} 思绪";

        // 根节点必须有一个 Image（全透，仅用于接收点击射线）
        Image selfImg = GetComponent<Image>();
        if (selfImg == null)
            selfImg = gameObject.AddComponent<Image>();
        selfImg.sprite = null;
        selfImg.color = new Color(0, 0, 0, 0);
        selfImg.raycastTarget = true;

        Debug.Log($"[Shop] Setup: {item.itemID} → 点击监听已挂载", gameObject);
    }

    /// <summary>点击直接在这里处理，不需要 Button 组件</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (data != null && manager != null)
            manager.BuyItem(data);
    }
}
