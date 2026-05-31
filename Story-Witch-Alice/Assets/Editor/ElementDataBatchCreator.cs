using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ElementDataBatchCreator : EditorWindow
{
    // 图标存放路径
    private const string iconFolder = "Assets/Data/IconData";

    [MenuItem("Tools/Batch Create/Update ElementData from CSV")]
    public static void CreateFromCSV()
    {
        string csvPath = EditorUtility.OpenFilePanel("Select elements.csv", "Assets/Data/ElementData", "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
        {
            Debug.LogError("CSV 文件为空或没有数据行");
            return;
        }

        string saveFolder = "Assets/Data/ElementData";
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            System.IO.Directory.CreateDirectory(saveFolder);
            AssetDatabase.Refresh();
        }

        // 确保图标文件夹存在
        if (!AssetDatabase.IsValidFolder(iconFolder))
        {
            System.IO.Directory.CreateDirectory(iconFolder);
            AssetDatabase.Refresh();
        }

        int createdCount = 0;
        int updatedCount = 0;
        int iconFoundCount = 0;
        int iconMissingCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = ParseCSVLine(line);
            if (cols.Length < 11)
            {
                Debug.LogWarning($"第{i + 1}行（{cols[0]}）列数不足，跳过");
                continue;
            }

            string eName = cols[0];
            string eID = cols[1];
            string desc = cols[2];
            string tagsStr = cols[3];
            float order = float.Parse(cols[4]);
            float creativity = float.Parse(cols[5]);
            float empathy = float.Parse(cols[6]);
            float desire = float.Parse(cols[7]);
            float mystery = float.Parse(cols[8]);
            float vitality = float.Parse(cols[9]);
            string physStr = cols[10];

            string assetPath = Path.Combine(saveFolder, eID + ".asset");
            ElementData asset = AssetDatabase.LoadAssetAtPath<ElementData>(assetPath);

            bool isNew = (asset == null);

            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<ElementData>();
                AssetDatabase.CreateAsset(asset, assetPath);
                createdCount++;
            }
            else
            {
                updatedCount++;
            }

            // 更新字段
            asset.elementName = eName;
            asset.elementID = eID;
            asset.description = desc;
            asset.tags = new List<string>(tagsStr.Split(';'));

            // 自动加载图标（新增）
            Sprite icon = LoadIcon(eID);
            if (icon != null)
            {
                asset.elementIcon = icon;
                iconFoundCount++;
            }
            else
            {
                iconMissingCount++;
            }

            // 重建属性列表
            asset.Properties = new List<ElementProperty>();
            AddProperty(asset.Properties, "order", order);
            AddProperty(asset.Properties, "creativity", creativity);
            AddProperty(asset.Properties, "empathy", empathy);
            AddProperty(asset.Properties, "desire", desire);
            AddProperty(asset.Properties, "mystery", mystery);
            AddProperty(asset.Properties, "vitality", vitality);

            if (!string.IsNullOrEmpty(physStr))
            {
                string[] physPairs = physStr.Split(';');
                foreach (string pair in physPairs)
                {
                    string[] kv = pair.Split(':');
                    if (kv.Length == 2 && float.TryParse(kv[1], out float val))
                    {
                        AddProperty(asset.Properties, kv[0], val);
                    }
                }
            }

            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();

        // 孤立资产检查
        HashSet<string> csvIDs = new HashSet<string>();
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = ParseCSVLine(lines[i].Trim());
            if (cols.Length >= 2) csvIDs.Add(cols[1]);
        }

        string[] existingFiles = Directory.GetFiles(saveFolder, "*.asset");
        List<string> orphaned = new List<string>();
        foreach (string file in existingFiles)
        {
            string id = Path.GetFileNameWithoutExtension(file);
            if (!csvIDs.Contains(id))
                orphaned.Add(id);
        }

        if (orphaned.Count > 0)
        {
            Debug.LogWarning($"以下资产在 CSV 中不存在，可能需要手动删除：{string.Join(", ", orphaned)}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"批量处理完成！新建 {createdCount} 个，更新 {updatedCount} 个。图标匹配 {iconFoundCount} 个，缺失 {iconMissingCount} 个。");
    }

    // 自动加载图标：根据 elementID 在 iconFolder 下查找同名图片
    private static Sprite LoadIcon(string elementID)
    {
        string[] extensions = { ".png", ".jpg", ".jpeg", ".psd" };
        foreach (string ext in extensions)
        {
            string iconPath = Path.Combine(iconFolder, elementID + ext);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite != null) return sprite;
        }
        return null;
    }

    private static void AddProperty(List<ElementProperty> list, string key, float value)
    {
        list.Add(new ElementProperty { key = key, value = value });
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