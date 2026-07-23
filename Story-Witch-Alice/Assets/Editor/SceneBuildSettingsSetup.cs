using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 Shop.unity 添加到 Build Settings 中。
/// 使用菜单 Tools → Add Shop Scene to Build Settings 执行。
/// </summary>
public static class SceneBuildSettingsSetup
{
    private const string ShopScenePath = "Assets/Scenes/Shop.unity";

    [MenuItem("Tools/Add Shop Scene to Build Settings")]
    public static void AddShopScene()
    {
        // 检查场景文件是否存在
        var shopScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ShopScenePath);
        if (shopScene == null)
        {
            EditorUtility.DisplayDialog("错误", $"找不到场景文件：{ShopScenePath}\n请检查路径是否正确。", "确定");
            return;
        }

        // 检查是否已经在 Build Settings 中
        var scenes = EditorBuildSettings.scenes;
        foreach (var scene in scenes)
        {
            if (scene.path == ShopScenePath)
            {
                EditorUtility.DisplayDialog("提示", "Shop.unity 已在 Build Settings 中。", "确定");
                return;
            }
        }

        // 添加
        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        System.Array.Copy(scenes, newScenes, scenes.Length);
        newScenes[scenes.Length] = new EditorBuildSettingsScene(ShopScenePath, true);
        EditorBuildSettings.scenes = newScenes;

        Debug.Log($"✅ 已添加 Shop.unity 到 Build Settings。当前共 {newScenes.Length} 个场景。");
    }

    /// <summary>
    /// 验证菜单项是否可用（场景文件存在时启用）
    /// </summary>
    [MenuItem("Tools/Add Shop Scene to Build Settings", true)]
    public static bool ValidateAddShopScene()
    {
        return AssetDatabase.LoadAssetAtPath<SceneAsset>(ShopScenePath) != null;
    }
}
