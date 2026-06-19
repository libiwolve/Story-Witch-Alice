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

        // 用共享读取模式避免"文件被占用"错误
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
        int iconFoundCount = 0;
        int iconMissingCount = 0;
        HashSet<string> csvIDs = new HashSet<string>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = ParseCSVLine(line);
            if (cols.Length < 11)
            {
                Debug.LogWarning($"第{i + 1}行列数不足({cols.Length})，跳过：{line.Substring(0, Mathf.Min(50, line.Length))}");
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
            }
            else
            {
                updatedCount++;
            }

            asset.elementName = eName;
            asset.elementID = eID;
            asset.description = desc;
            asset.tags = new List<string>(tagsStr.Split(';'));

            // 自动加载图标
            Sprite icon = LoadIcon(eID);
            if (icon != null)
            {
                asset.elementIcon = icon;
                iconFoundCount++;
            }
            else
            {
                asset.elementIcon = null;
                iconMissingCount++;
            }

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

        // 清理孤立资产（CSV 中已不存在的旧 .asset 文件）
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
                Debug.Log($"已删除 {deletedCount} 个孤立资产（CSV 中不存在的旧元素）");
        }

        AssetDatabase.Refresh();
        Debug.Log($"批量处理完成！新建 {createdCount} 个，更新 {updatedCount} 个。图标匹配 {iconFoundCount} 个，缺失 {iconMissingCount} 个。");
    }

    private static Sprite LoadIcon(string elementID)
    {
        if (!AssetDatabase.IsValidFolder(iconFolder)) return null;

        string[] names = { $"{elementID}.png", $"{elementID}UI.png" };
        foreach (string name in names)
        {
            string path = iconFolder + "/" + name;
            // 确保 PNG 以 Sprite 形式导入
            EnsureImportedAsSprite(path);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
        }
        return null;
    }

    /// <summary>
    /// 强制 PNG 以 Sprite(2D and UI) 模式导入，否则 LoadAssetAtPath&lt;Sprite&gt; 会返回 null
    /// </summary>
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
