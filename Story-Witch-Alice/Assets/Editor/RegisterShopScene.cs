using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 Shop.unity 添加到 Build Settings 中。
/// 使用菜单 Tools → Register Shop Scene 执行。
/// </summary>
public static class RegisterShopScene
{
    private const string ShopScenePath = "Assets/Scenes/Shop.unity";

    [MenuItem("Tools/Register Shop Scene")]
    public static void AddShopScene()
    {
        var shopScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ShopScenePath);
        if (shopScene == null)
        {
            EditorUtility.DisplayDialog("错误", $"找不到场景文件：{ShopScenePath}", "确定");
            return;
        }

        var scenes = EditorBuildSettings.scenes;
        foreach (var scene in scenes)
        {
            if (scene.path == ShopScenePath)
            {
                EditorUtility.DisplayDialog("提示", "Shop.unity 已在 Build Settings 中。", "确定");
                return;
            }
        }

        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        System.Array.Copy(scenes, newScenes, scenes.Length);
        newScenes[scenes.Length] = new EditorBuildSettingsScene(ShopScenePath, true);
        EditorBuildSettings.scenes = newScenes;

        Debug.Log($"✅ 已添加 Shop.unity 到 Build Settings。当前共 {newScenes.Length} 个场景。");
    }

    [MenuItem("Tools/Register Shop Scene", true)]
    public static bool ValidateAddShopScene()
    {
        return AssetDatabase.LoadAssetAtPath<SceneAsset>(ShopScenePath) != null;
    }
}
