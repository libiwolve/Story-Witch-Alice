using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 一键从 ShopItemData .asset 生成场景中的购买选项 UI
/// </summary>
public class ShopUIGenerator : EditorWindow
{
    [SerializeField] private GameObject goodsPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private string remnantID = "3";

    [MenuItem("Tools/Generate Shop UI from Assets")]
    public static void ShowWindow()
    {
        GetWindow<ShopUIGenerator>("商店 UI 生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("生成商店购买选项", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        goodsPrefab = (GameObject)EditorGUILayout.ObjectField("GoodsPrefab", goodsPrefab, typeof(GameObject), true);
        contentParent = (Transform)EditorGUILayout.ObjectField("Content 父节点", contentParent, typeof(Transform), true);
        shopManager = (ShopManager)EditorGUILayout.ObjectField("ShopManager", shopManager, typeof(ShopManager), true);
        remnantID = EditorGUILayout.TextField("遗民编号", remnantID);

        EditorGUILayout.Space();

        if (GUILayout.Button("生成 UI 条目", GUILayout.Height(40)))
        {
            GenerateUI();
        }
    }

    private void GenerateUI()
    {
        if (goodsPrefab == null) { Debug.LogError("请先拖入 GoodsPrefab"); return; }
        if (contentParent == null) { Debug.LogError("请先拖入 Content 父节点"); return; }
        if (shopManager == null) { Debug.LogError("请先拖入 ShopManager"); return; }

        // 查找所有 ShopItemData 资产
        string[] guids = AssetDatabase.FindAssets("t:ShopItemData", new[] { "Assets/Data/ShopData" });
        List<ShopItemData> foundItems = new List<ShopItemData>();
        List<ShopItemData> generatedItems = new List<ShopItemData>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ShopItemData item = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
            if (item != null) foundItems.Add(item);
        }

        if (foundItems.Count == 0)
        {
            Debug.LogError("未找到任何 ShopItemData 资产，请先运行 Batch Create Shop Data");
            return;
        }

        // 清空 Content 下已有条目
        Undo.RecordObject(contentParent, "清空旧条目");
        while (contentParent.childCount > 0)
        {
            Undo.DestroyObjectImmediate(contentParent.GetChild(0).gameObject);
        }

        // 遍历并生成
        foreach (var item in foundItems)
        {
            // 按遗民编号过滤
            if (!string.IsNullOrEmpty(remnantID) && item.remnantID != remnantID)
                continue;

            GameObject entry = (GameObject)PrefabUtility.InstantiatePrefab(goodsPrefab, contentParent);
            entry.name = item.itemID;
            Undo.RegisterCreatedObjectUndo(entry, $"创建商品 {item.itemID}");

            // 找到子 Text 组件并填入信息
            TextMeshProUGUI[] texts = entry.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                if (txt.name.Contains("Name") || txt.name.Contains("name"))
                    txt.text = item.itemName;
                else if (txt.name.Contains("Price") || txt.name.Contains("price") || txt.name.Contains("Cost"))
                    txt.text = item.price.ToString();
                else if (txt.name.Contains("Desc") || txt.name.Contains("desc") || txt.name.Contains("Description"))
                    txt.text = item.description;
            }

            // 设置图标
            Image[] imgs = entry.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                if (img.name.Contains("Icon") || img.name.Contains("icon"))
                {
                    if (item.icon != null)
                        img.sprite = item.icon;
                    break;
                }
            }

            // 挂载 ItemEntryUI 并绑定数据
            ItemEntryUI entryUI = entry.GetComponent<ItemEntryUI>();
            if (entryUI == null) entryUI = entry.AddComponent<ItemEntryUI>();
            entryUI.Setup(item, shopManager);

            generatedItems.Add(item);
        }

        // 更新 ShopManager.shopItems 列表
        Undo.RecordObject(shopManager, "更新 ShopItems");
        shopManager.shopItems.Clear();
        shopManager.shopItems.AddRange(generatedItems);
        EditorUtility.SetDirty(shopManager);

        AssetDatabase.SaveAssets();
        Debug.Log($"商店 UI 生成完成！共添加 {generatedItems.Count} 个商品条目。");
    }
}
