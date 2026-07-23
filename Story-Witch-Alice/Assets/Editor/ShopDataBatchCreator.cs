using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ShopDataBatchCreator
{
    private const string csvPath = "Assets/Data/ShopData/ShopItems.csv";
    private const string saveFolder = "Assets/Data/ShopData";

    [MenuItem("Tools/Batch Create/Update Shop Data from CSV")]
    public static void CreateFromCSV()
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"找不到 CSV 文件：{csvPath}，请先创建");
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
            lines = sr.ReadToEnd().Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        }

        if (lines.Length < 2)
        {
            Debug.LogError("CSV 文件为空");
            return;
        }

        int created = 0, updated = 0;
        HashSet<string> csvIDs = new HashSet<string>();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = ParseCSVLine(lines[i].Trim());
            if (cols.Length < 6) continue;

            string id = cols[0];
            string name = cols[1];
            string desc = cols[2];
            int price = int.TryParse(cols[3], out int p) ? p : 0;
            string catStr = cols[4];
            string rewardID = cols[5];
            string remnantID = cols.Length > 6 ? cols[6] : "";

            csvIDs.Add(id);

            // 加载 ElementData
            ElementData reward = null;
            if (!string.IsNullOrEmpty(rewardID))
            {
                string epath = "Assets/Data/ElementData/" + rewardID + ".asset";
                reward = AssetDatabase.LoadAssetAtPath<ElementData>(epath);
            }

            ShopItemCategory cat = catStr == "AbstractConcept" ? ShopItemCategory.AbstractConcept : ShopItemCategory.Material;

            string assetPath = (saveFolder + "/" + id + ".asset").Replace("\\", "/");
            ShopItemData asset = AssetDatabase.LoadAssetAtPath<ShopItemData>(assetPath);
            bool isNew = asset == null;

            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<ShopItemData>();
                AssetDatabase.CreateAsset(asset, assetPath);
                created++;
            }
            else
            {
                updated++;
            }

            asset.itemID = id;
            asset.itemName = name;
            asset.description = desc;
            asset.price = price;
            asset.category = cat;
            asset.rewardElement = reward;
            asset.remnantID = remnantID;

            // 加载图标
            string iconPath = $"Assets/Data/ArtResourceData/Design/Icon/{id}.png";
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icon == null) icon = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Data/ArtResourceData/Design/Icon/{id}UI.png");
            asset.icon = icon;

            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();

        // 清理孤立
        if (Directory.Exists(saveFolder))
        {
            foreach (string file in Directory.GetFiles(saveFolder, "*.asset"))
            {
                string fid = Path.GetFileNameWithoutExtension(file);
                if (!csvIDs.Contains(fid) && fid != "ShopItems")
                    AssetDatabase.DeleteAsset(file.Replace("\\", "/"));
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"店铺数据导入完成！新建 {created}，更新 {updated} 个。");
    }

    private static string[] ParseCSVLine(string line)
    {
        List<string> fields = new List<string>();
        bool inQuotes = false;
        string field = "";
        foreach (char c in line)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes) { fields.Add(field); field = ""; }
            else field += c;
        }
        fields.Add(field);
        return fields.ToArray();
    }
}
