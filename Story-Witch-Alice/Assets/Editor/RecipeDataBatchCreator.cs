using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class RecipeDataBatchCreator
{
    private const string csvPath = "Assets/Data/RecipeData/Recipes.csv";
    private const string saveFolder = "Assets/Data/RecipeData";

    [MenuItem("Tools/Batch Create/Update RecipeData from CSV")]
    public static void CreateFromCSV()
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"找不到 CSV 文件：{csvPath}");
            return;
        }

        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            System.IO.Directory.CreateDirectory(saveFolder);
            AssetDatabase.Refresh();
        }

        // 共享读取模式
        string[] lines;
        using (var fs = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var sr = new StreamReader(fs))
        {
            string content = sr.ReadToEnd();
            lines = content.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        }

        if (lines.Length < 2)
        {
            Debug.LogError("CSV 文件为空或没有数据行");
            return;
        }

        int createdCount = 0;
        int updatedCount = 0;

        // 收集所有产物的 ID，用于孤儿清理
        HashSet<string> csvProductIDs = new HashSet<string>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = ParseCSVLine(line);
            if (cols.Length < 4)
            {
                Debug.LogWarning($"第{i + 1}行列数不足({cols.Length})，跳过");
                continue;
            }

            string ing1ID = cols[0];
            string ing2ID = cols[1];
            string ing3ID = cols[2];
            string productID = cols[3];
            string condition = cols.Length > 4 ? cols[4] : "";

            csvProductIDs.Add(productID);

            // 加载元素
            ElementData ing1 = LoadElement(ing1ID);
            ElementData ing2 = LoadElement(ing2ID);
            ElementData ing3 = LoadElement(ing3ID);
            ElementData product = LoadElement(productID);

            if (ing1 == null || ing2 == null || product == null)
            {
                string missing = "";
                if (ing1 == null) missing += ing1ID + " ";
                if (ing2 == null) missing += ing2ID + " ";
                if (product == null) missing += productID;
                Debug.LogWarning($"配方跳过：找不到元素 ({missing})");
                continue;
            }

            // 生成文件名
            string fileName = ing1ID + "_" + ing2ID;
            if (!string.IsNullOrEmpty(ing3ID)) fileName += "_" + ing3ID;
            fileName += "_To_" + productID;

            string assetPath = (saveFolder + "/" + fileName + ".asset").Replace("\\", "/");
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

            asset.ingredients = new List<ElementData> { ing1, ing2 };
            if (!string.IsNullOrEmpty(ing3ID)) asset.ingredients.Add(ing3);
            asset.product = product;
            asset.condition = string.IsNullOrEmpty(condition) ? null : condition;

            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();

        // 清理孤立资产
        if (Directory.Exists(saveFolder))
        {
            string[] existingFiles = Directory.GetFiles(saveFolder, "*.asset");
            int deletedCount = 0;
            foreach (string file in existingFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                int toIndex = fileName.LastIndexOf("_To_");
                if (toIndex >= 0)
                {
                    string pid = fileName.Substring(toIndex + 4);
                    if (!csvProductIDs.Contains(pid))
                    {
                        AssetDatabase.DeleteAsset(file.Replace("\\", "/"));
                        deletedCount++;
                    }
                }
            }
            if (deletedCount > 0)
                Debug.Log($"已删除 {deletedCount} 个孤立资产（CSV 中不存在的旧配方）");
        }

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
                inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes)
            {
                fields.Add(field);
                field = "";
            }
            else
                field += c;
        }
        fields.Add(field);
        return fields.ToArray();
    }
}
