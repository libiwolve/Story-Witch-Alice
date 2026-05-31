using UnityEditor;
using UnityEngine;

public class ElementDataCreator : EditorWindow
{
   private static readonly string[] metaKeys = {"order","creativity","empathy","desire","mystery","vitality"};
   private static readonly float defaultValue=0f;
   [MenuItem("Assets/Create/ScriptableObjects/Element (With Meta)", false, 0)]
   public static void CreateElementWithMeta()
    {
        // 使用默认创建方式
        ElementData asset = ScriptableObject.CreateInstance<ElementData>();

        // 自动填充元属性
        asset.Properties = new System.Collections.Generic.List<ElementProperty>();
        foreach (string key in metaKeys)
        {
            asset.Properties.Add(new ElementProperty { key = key, value = defaultValue });
        }

        // 保存资产
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Data/ElementData/NewElement.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 在Project窗口选中新资产
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}
