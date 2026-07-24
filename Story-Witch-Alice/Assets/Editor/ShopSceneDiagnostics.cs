using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// 商店场景诊断工具 — 测试基本 UI 能否显示
/// </summary>
public class ShopSceneDiagnostics : EditorWindow
{
    [MenuItem("Tools/Diagnose Shop Scene")]
    public static void Diagnose()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ 场景中没有 Canvas！");
            return;
        }

        Debug.Log($"✅ Canvas: {canvas.name}");
        Debug.Log($"   渲染模式: {canvas.renderMode}");
        Debug.Log($"   活动状态: {canvas.gameObject.activeInHierarchy}");

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        Debug.Log($"   GraphicRaycaster: {(raycaster != null ? "✅ 存在" : "❌ 缺失")}");

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        Debug.Log($"   CanvasScaler: {(scaler != null ? "✅ 存在" : "⚠️ 缺失")}");

        // 检查子对象
        var panels = canvas.GetComponentsInChildren<RectTransform>();
        foreach (var rt in panels)
        {
            Debug.Log($"   子对象: {rt.name} | 锚点: {rt.anchorMin}-{rt.anchorMax} | 尺寸: {rt.sizeDelta} | 活跃: {rt.gameObject.activeInHierarchy}", rt.gameObject);
        }

        // 检查 EventSystem
        var es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        Debug.Log($"EventSystem: {(es != null ? "✅ 存在" : "❌ 缺失")}");

        // 检查 ShopManager
        ShopManager sm = FindObjectOfType<ShopManager>();
        if (sm != null)
        {
            Debug.Log($"✅ ShopManager 存在");
            Debug.Log($"   itemListParent: {(sm.itemListParent != null ? sm.itemListParent.name : "❌ null")}");
            Debug.Log($"   shopItems 数量: {(sm.shopItems != null ? sm.shopItems.Count : 0)}");

            if (sm.itemListParent != null)
                Debug.Log($"   Content 子对象数: {sm.itemListParent.childCount}");
        }
        else
        {
            Debug.Log("❌ ShopManager 不存在");
        }

        // 在 Canvas 上创建一个测试文字（确认 Canvas 能显示 UI）
        GameObject testGO = new GameObject("TEST_VISIBLE");
        testGO.transform.SetParent(canvas.transform, false);
        RectTransform tr = testGO.AddComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 0);
        tr.anchorMax = new Vector2(1, 1);
        tr.sizeDelta = Vector2.zero;
        TextMeshProUGUI testText = testGO.AddComponent<TextMeshProUGUI>();
        testText.text = "TEST - 如果能看到这行字，Canvas 工作正常";
        testText.fontSize = 36;
        testText.alignment = TextAlignmentOptions.Center;
        testText.color = Color.green;
        testText.fontStyle = TMPro.FontStyles.Bold;

        Debug.Log("🟢 测试文字已添加到 Canvas 正中央！场景中应该能看到绿色文字。");
    }
}
