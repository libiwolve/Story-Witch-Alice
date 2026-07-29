using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ElementDataBatchCreator
{
    private const string csvPath = "Assets/Data/ElementData/Selectelement.csv";
    private const string saveFolder = "Assets/Data/ElementData";
    private const string iconFolder = "Assets/Data/ArtResourceData/Design/Icon";

    [MenuItem("Tools/Batch Create/Update ElementData from CSV")]
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
        int iconAutoMatchedCount = 0;
        int iconSkippedCount = 0;
        HashSet<string> csvIDs = new HashSet<string>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = ParseCSVLine(line);
            if (cols.Length < 11)
            {
                Debug.LogWarning($"第{i + 1}行列数不足({cols.Length})，跳过");
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

            csvIDs.Add(eID);

            string assetPath = Path.Combine(saveFolder, eID + ".asset").Replace("\\", "/");
            ElementData asset = AssetDatabase.LoadAssetAtPath<ElementData>(assetPath);

            bool isNew = (asset == null);
            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<ElementData>();
                AssetDatabase.CreateAsset(asset, assetPath);
                createdCount++;

                // ★ 只有新创建的才自动匹配图标
                Sprite icon = LoadIcon(eID);
                asset.elementIcon = icon;
                if (icon != null)
                    iconAutoMatchedCount++;
            }
            else
            {
                updatedCount++;
                // ★ 已存在的元素保留原有 elementIcon，不做任何改动
                iconSkippedCount++;
            }

            // 更新其他字段
            asset.elementName = eName;
            asset.elementID = eID;
            asset.description = desc;
            asset.tags = new List<string>(tagsStr.Split(';'));

            // 重建属性
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
                        AddProperty(asset.Properties, kv[0], val);
                }
            }

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
                string id = Path.GetFileNameWithoutExtension(file);
                if (!csvIDs.Contains(id))
                {
                    AssetDatabase.DeleteAsset(file.Replace("\\", "/"));
                    deletedCount++;
                }
            }
            if (deletedCount > 0)
                Debug.Log($"已删除 {deletedCount} 个孤立资产");
        }

        AssetDatabase.Refresh();
        Debug.Log($"处理完成！新建 {createdCount} 个（图标自动匹配 {iconAutoMatchedCount} 个），更新 {updatedCount} 个（图标保留原值 {iconSkippedCount} 个）。");
    }

    /// <summary>
    /// 强制刷新所有元素的图标（当你新画了图标或移动了图标文件时使用）
    /// </summary>
    [MenuItem("Tools/Force Refresh All Element Icons")]
    public static void ForceRefreshAllIcons()
    {
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            Debug.LogError($"找不到文件夹：{saveFolder}");
            return;
        }

        string[] files = Directory.GetFiles(saveFolder, "*.asset");
        int matchedCount = 0;
        int missingCount = 0;

        foreach (string file in files)
        {
            string assetPath = file.Replace("\\", "/");
            ElementData asset = AssetDatabase.LoadAssetAtPath<ElementData>(assetPath);
            if (asset == null) continue;

            Sprite icon = LoadIcon(asset.elementID);
            asset.elementIcon = icon;
            EditorUtility.SetDirty(asset);

            if (icon != null) matchedCount++;
            else missingCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"图标强制刷新完成！匹配 {matchedCount} 个，缺失 {missingCount} 个。");
    }

    private static Sprite LoadIcon(string elementID)
    {
        if (!AssetDatabase.IsValidFolder(iconFolder)) return null;

        string[] names = { $"{elementID}.png", $"{elementID}UI.png" };
        foreach (string name in names)
        {
            string path = iconFolder + "/" + name;
            EnsureImportedAsSprite(path);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
        }
        return null;
    }

    private static void EnsureImportedAsSprite(string path)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
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