using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
// System.Text.RegularExpressions no longer needed

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
            if (cols.Length < 5) continue;

            // 新格式: itemName,description,price,rewardElement,remnantID
            string name = cols[0];
            string desc = cols[1];
            int price = int.TryParse(cols[2], out int p) ? p : 0;
            string rewardID = cols[3];
            string remnantID = cols.Length > 4 ? cols[4] : "";

            // itemID = rewardElement 的英文 ID（文件名也用它）
            string id = string.IsNullOrEmpty(rewardID) ? name : rewardID;
            csvIDs.Add(id);

            // 加载 ElementData
            ElementData reward = null;
            if (!string.IsNullOrEmpty(rewardID))
            {
                string epath = "Assets/Data/ElementData/" + rewardID + ".asset";
                reward = AssetDatabase.LoadAssetAtPath<ElementData>(epath);
            }

            // 按遗民编号分文件夹：ShopData/{remnantID}/{id}.asset
            string subFolder = string.IsNullOrEmpty(remnantID) ? saveFolder : saveFolder + "/" + remnantID;
            if (!AssetDatabase.IsValidFolder(subFolder))
            {
                System.IO.Directory.CreateDirectory(subFolder.Replace("Assets/", ""));
                AssetDatabase.Refresh();
            }
            string assetPath = (subFolder + "/" + id + ".asset").Replace("\\", "/");
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
            asset.rewardElement = reward;
            asset.remnantID = remnantID;

            // 图标（忽略大小写）
            string iconDir = "Assets/Data/IconData";
            Sprite icon = FindIconIgnoreCase(iconDir, id);
            if (icon == null) icon = FindIconIgnoreCase(iconDir, id + "UI");
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

    /// <summary>在图标文件夹中忽略大小写查找 Sprite，并保证导入类型为 Sprite</summary>
    private static Sprite FindIconIgnoreCase(string folder, string name)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return null;
        string[] guids = AssetDatabase.FindAssets($"{name} t:Texture2D", new[] { folder });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            // 强制设为 Sprite 类型
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
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
