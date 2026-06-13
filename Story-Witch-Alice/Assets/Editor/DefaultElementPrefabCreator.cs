using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 一键生成默认元素 Prefab（粉紫色 2×2 马赛克精灵）。
/// 菜单：Tools → Create Default Element Prefab
/// </summary>
public class DefaultElementPrefabCreator
{
    [MenuItem("Tools/Create Default Element Prefab")]
    static void CreateDefaultPrefab()
    {
        // ── 1. 确保目录存在 ──
        string spritesDir = "Assets/Sprites";
        string prefabsDir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(spritesDir))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder(prefabsDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // ── 2. 生成粉紫色 2×2 纹理 ──
        const int size = 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color fuchsia = new Color(0.93f, 0.20f, 0.78f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, fuchsia);
        tex.Apply();

        string texPath = spritesDir + "/DefaultElement.png";
        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(texPath, pngData);
        Object.DestroyImmediate(tex);

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        // ── 3. 设置 Sprite 导入参数 ──
        TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = 2; // 2×2 纹理 = 世界空间 1 单位
            importer.SaveAndReimport();
        }

        AssetDatabase.Refresh();

        // ── 4. 加载生成的 Sprite ──
        Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);

        // ── 5. 创建 GameObject ──
        GameObject go = new GameObject("DefaultElement");

        // SpriteRenderer
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = defaultSprite;

        // Rigidbody2D
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.drag = 2f;
        rb.angularDrag = 5f;

        // CircleCollider2D
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.sharedMaterial = null; // 用默认物理材质

        // PhysicsElement 脚本
        go.AddComponent<PhysicsElement>();

        // ── 6. 保存为 Prefab ──
        string prefabPath = prefabsDir + "/DefaultElement.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        // ── 7. 选中生成的 Prefab ──
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log("<color=magenta>[DefaultElementPrefabCreator]</color> 默认元素 Prefab 已生成 → " + prefabPath);
    }
}
