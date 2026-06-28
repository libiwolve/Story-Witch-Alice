using UnityEngine;

/// <summary>
/// 商店物品分类
/// </summary>
public enum ShopItemCategory
{
    Material,       // 物质类
    AbstractConcept // 抽象概念类
}

/// <summary>
/// 商店物品数据资产。右键 Create → Shop/ItemData 创建。
/// </summary>
[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/ItemData", order = 2)]
public class ShopItemData : ScriptableObject
{
    [Header("基本信息")]
    public string itemID;
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("价格")]
    public int price;                    // 思绪价格
    public ShopItemCategory category;    // 分类

    [Header("奖励")]
    public ElementData rewardElement;    // 购买后解锁的元素

    [Header("所属遗民")]
    public string remnantID;             // 扩展：多遗民过滤

    [Header("好感度系统（预留）")]
    public int requiredAffinity;
    public bool hasDiscount;
    [Range(0f, 1f)]
    public float discountRate;
}
