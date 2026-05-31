using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class RecipeDataBatchCreator : EditorWindow
{
    [MenuItem("Tools/Batch Create/Update RecipeData from CSV")]
    public static void CreateFromCSV()
    {
        string csvPath = EditorUtility.OpenFilePanel("Recipes.csv", "Assets/Data/RecipeData", "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
        {
            Debug.LogError("CSV 文件为空或没有数据行");
            return;
        }

        string saveFolder = "Assets/Data/RecipeData";
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            System.IO.Directory.CreateDirectory(saveFolder);
            AssetDatabase.Refresh();
        }

        int createdCount = 0;
        int updatedCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = ParseCSVLine(line);
            if (cols.Length < 4)
            {
                Debug.LogWarning($"第{i + 1}行列数不足，跳过");
                continue;
            }

            string ing1ID = cols[0];
            string ing2ID = cols[1];
            string ing3ID = cols[2];
            string productID = cols[3];
            string condition = cols.Length > 4 ? cols[4] : "";

            // 查找元素资产
            ElementData ing1 = LoadElement(ing1ID);
            ElementData ing2 = LoadElement(ing2ID);
            ElementData ing3 = LoadElement(ing3ID);
            ElementData product = LoadElement(productID);

            if (ing1 == null || ing2 == null || product == null)
            {
                Debug.LogWarning($"配方跳过：找不到元素 ({ing1ID}, {ing2ID}, {ing3ID}, {productID})");
                continue;
            }

            // 生成文件名
            string fileName = ing1ID + "_" + ing2ID;
            if (!string.IsNullOrEmpty(ing3ID)) fileName += "_" + ing3ID;
            fileName += "_To_" + productID;

            string assetPath = Path.Combine(saveFolder, fileName + ".asset");
            RecipeData asset = AssetDatabase.LoadAssetAtPath<RecipeData>(assetPath);

            bool isNew = (asset == null);

            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<RecipeData>();
                AssetDatabase.CreateAsset(asset, assetPath);
                createdCount++;
            }
            else
            {
                updatedCount++;
            }

            // 填充数据
            asset.ingredients = new List<ElementData> { ing1, ing2 };
            if (!string.IsNullOrEmpty(ing3ID)) asset.ingredients.Add(ing3);
            asset.product = product;
            asset.condition = string.IsNullOrEmpty(condition) ? null : condition;

            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"批量处理完成！新建 {createdCount} 个，更新 {updatedCount} 个 RecipeData 资产。");
    }

    private static ElementData LoadElement(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        string path = "Assets/Data/ElementData/" + id + ".asset";
        return AssetDatabase.LoadAssetAtPath<ElementData>(path);
    }

    private static string[] ParseCSVLine(string line)
    {
        List<string> fields = new List<string>();
        bool inQuotes = false;
        string field = "";
        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(field);
                field = "";
            }
            else
            {
                field += c;
            }
        }
        fields.Add(field);
        return fields.ToArray();
    }
}