using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 一键搭建完整的商店场景（结构 + ShopManager + 商品）
/// </summary>
public class ShopSceneSetup : EditorWindow
{
    [SerializeField] private GameObject goodsPrefab;

    [MenuItem("Tools/Setup Complete Shop Scene")]
    public static void ShowWindow()
    {
        GetWindow<ShopSceneSetup>("一键商店搭建");
    }

    private void OnGUI()
    {
        GUILayout.Label("一键搭建完整商店场景", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        goodsPrefab = (GameObject)EditorGUILayout.ObjectField("GoodsPrefab", goodsPrefab, typeof(GameObject), false);
        EditorGUILayout.Space();

        if (GUILayout.Button("搭建完整商店场景", GUILayout.Height(40)))
        {
            if (goodsPrefab == null)
            {
                Debug.LogError("请先拖入 GoodsPrefab");
                return;
            }
            string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            if (!scenePath.Contains("Shop") && !scenePath.Contains("shop"))
            {
                if (!EditorUtility.DisplayDialog("确认", "当前场景不是 Shop 场景，确定继续？", "继续", "取消"))
                    return;
            }
            SetupEverything();
        }
    }

    void SetupEverything()
    {
        // 1. 查找现有的或创建新的 Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject go = new GameObject("Canvas");
            canvas = go.AddComponent<Canvas>();
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }
        // ★★★★★ 强制设为 ScreenSpaceOverlay（不管之前是什么）★★★★★
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 2. 创建 UI 布局（清理旧的同级对象）
        Transform oldLeft = canvas.transform.Find("RemnantPanel");
        if (oldLeft != null) DestroyImmediate(oldLeft.gameObject);
        Transform oldRight = canvas.transform.Find("ShopPanel");
        if (oldRight != null) DestroyImmediate(oldRight.gameObject);

        // ---------- 左 1/4：遗民面板 ----------
        GameObject leftPanel = new GameObject("RemnantPanel");
        leftPanel.transform.SetParent(canvas.transform, false);
        RectTransform lr = leftPanel.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = new Vector2(0.25f, 1f);
        lr.sizeDelta = Vector2.zero;
        Image leftBg = leftPanel.AddComponent<Image>();
        leftBg.color = new Color(0.12f, 0.12f, 0.18f, 1);

        GameObject portraitObj = new GameObject("RemnantPortrait");
        portraitObj.transform.SetParent(leftPanel.transform, false);
        RectTransform pr = portraitObj.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.1f, 0.3f);
        pr.anchorMax = new Vector2(0.9f, 0.7f);
        pr.sizeDelta = Vector2.zero;
        Image portraitImg = portraitObj.AddComponent<Image>();
        portraitImg.preserveAspect = true;

        // ---------- 右 3/4：商店面板 ----------
        GameObject rightPanel = new GameObject("ShopPanel");
        rightPanel.transform.SetParent(canvas.transform, false);
        RectTransform rr = rightPanel.AddComponent<RectTransform>();
        rr.anchorMin = new Vector2(0.25f, 0);
        rr.anchorMax = Vector2.one;
        rr.sizeDelta = Vector2.zero;
        Image rightBg = rightPanel.AddComponent<Image>();
        rightBg.color = new Color(0.2f, 0.2f, 0.27f, 1);

        // 余额
        GameObject balanceObj = new GameObject("ThoughtsBalance");
        balanceObj.transform.SetParent(rightPanel.transform, false);
        RectTransform br = balanceObj.AddComponent<RectTransform>();
        br.anchorMin = new Vector2(0.5f, 0.92f);
        br.anchorMax = new Vector2(0.95f, 0.98f);
        br.sizeDelta = Vector2.zero;
        TextMeshProUGUI balanceText = balanceObj.AddComponent<TextMeshProUGUI>();
        balanceText.text = "思绪：99999";
        balanceText.fontSize = 26;
        balanceText.alignment = TextAlignmentOptions.Right;
        balanceText.color = new Color(1f, 0.9f, 0.5f);

        // ScrollView
        GameObject scrollGO = new GameObject("ItemScrollView");
        scrollGO.transform.SetParent(rightPanel.transform, false);
        RectTransform srRect = scrollGO.AddComponent<RectTransform>();
        srRect.anchorMin = new Vector2(0.02f, 0.02f);
        srRect.anchorMax = new Vector2(0.98f, 0.88f);
        srRect.sizeDelta = Vector2.zero;
        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        RectTransform vpr = viewport.AddComponent<RectTransform>();
        vpr.anchorMin = Vector2.zero;
        vpr.anchorMax = Vector2.one;
        vpr.sizeDelta = Vector2.zero;
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0.15f, 0.15f, 0.2f, 1f); // 有可见背景，Mask 才能正确工作
        vpImg.raycastTarget = false;
        viewport.AddComponent<Mask>();

        // ★★★ Content ★★★
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewport.transform, false);
        RectTransform cr = contentObj.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1);
        cr.anchorMax = new Vector2(1, 1);
        cr.pivot = new Vector2(0.5f, 1);
        cr.sizeDelta = new Vector2(0, 600);
        // 手动布局模式 — 不加 VerticalLayoutGroup，用户可自由拖拽摆放

        scrollRect.content = cr;
        scrollRect.viewport = vpr;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // 3. 创建或更新 ShopManager
        // 先找场景中已有的 ShopManager
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        GameObject shopMgrObj;
        if (shopManager != null)
        {
            shopMgrObj = shopManager.gameObject;
            Debug.Log("找到已有的 ShopManager");
        }
        else
        {
            shopMgrObj = new GameObject("ShopManager");
            shopManager = shopMgrObj.AddComponent<ShopManager>();
        }
        Undo.RegisterCreatedObjectUndo(shopMgrObj, "创建 ShopManager");

        // 连线
        shopManager.remnantPanel = leftPanel;
        shopManager.remnantPortraitImage = portraitImg;
        shopManager.shopPanel = rightPanel;
        shopManager.thoughtsBalanceText = balanceText;
        shopManager.itemListParent = contentObj.transform;
        EditorUtility.SetDirty(shopManager);

        // 4. 创建 CurrencyManager
        if (FindObjectOfType<CurrencyManager>() == null)
        {
            GameObject cmObj = new GameObject("CurrencyManager");
            cmObj.AddComponent<CurrencyManager>();
        }

        // 5. EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        AssetDatabase.SaveAssets();

        // 6. 自动生成商品条目
        GenerateShopItems(shopManager, contentObj.transform);

        Debug.Log("✅ 商店场景搭建完成！所有组件已自动连线。");
    }

    void GenerateShopItems(ShopManager shopManager, Transform contentParent)
    {
        // 清空旧的
        while (contentParent.childCount > 0)
            DestroyImmediate(contentParent.GetChild(0).gameObject);

        // 查找所有 ShopItemData
        string[] guids = AssetDatabase.FindAssets("t:ShopItemData", new[] { "Assets/Data/ShopData" });
        List<ShopItemData> items = new List<ShopItemData>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ShopItemData item = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
            if (item != null) items.Add(item);
        }

        if (items.Count == 0)
        {
            Debug.LogWarning("未找到 ShopItemData 资产，请先运行 Tools → Batch Create/Update Shop Data from CSV");
            return;
        }

        List<ShopItemData> order = new List<ShopItemData>();
        foreach (var item in items)
        {
            GameObject entry = (GameObject)PrefabUtility.InstantiatePrefab(goodsPrefab, contentParent);
            entry.name = item.itemID;

            // 设置文字
            foreach (var txt in entry.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (txt.name.ToLower().Contains("name")) txt.text = item.itemName;
                else if (txt.name.ToLower().Contains("price") || txt.name.ToLower().Contains("cost"))
                    txt.text = item.price.ToString();
            }

            // 图标
            foreach (var img in entry.GetComponentsInChildren<Image>(true))
            {
                if (img.name.ToLower().Contains("icon") && item.icon != null)
                { img.sprite = item.icon; break; }
            }

            // 手动布局模式：保持 GoodsPrefab 自身锚点和尺寸不变

            // 绑定点击
            ItemEntryUI ui = entry.GetComponent<ItemEntryUI>();
            if (ui == null) ui = entry.AddComponent<ItemEntryUI>();
            ui.Setup(item, shopManager);

            order.Add(item);
        }

        shopManager.shopItems.Clear();
        shopManager.shopItems.AddRange(order);
        EditorUtility.SetDirty(shopManager);

        Debug.Log($"生成 {order.Count} 个商品条目");
    }
}
