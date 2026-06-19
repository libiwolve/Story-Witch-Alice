using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 一键生成 OrbitElement 预制体（带物理碰撞的行星轨道元素模板）。
/// 菜单：Tools → Create Orbit Element Prefab
/// </summary>
public class OrbitElementPrefabCreator
{
    [MenuItem("Tools/Create Orbit Element Prefab")]
    static void CreateOrbitElementPrefab()
    {
        string prefabsDir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabsDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // ── 创建 GameObject ──
        GameObject go = new GameObject("OrbitElement");

        // SpriteRenderer（放一个默认的小圆，用户后续替换为实际图标）
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 0;

        // 生成一个默认的小白圆作为占位 sprite
        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dx = x - 7.5f, dy = y - 7.5f;
                bool inside = (dx * dx + dy * dy) <= 56f;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        string texPath = prefabsDir + "/OrbitElementIcon.png";
        File.WriteAllBytes(texPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();

        TextureImporter imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.filterMode = FilterMode.Point;
            imp.spritePixelsPerUnit = 16;
            imp.SaveAndReimport();
        }
        AssetDatabase.Refresh();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);

        // Rigidbody2D（物理引擎驱动运动）
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;          // 不受重力，由向心力驱动
        rb.drag = 0.5f;
        rb.angularDrag = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        // CircleCollider2D（物理碰撞，弹开其他轨道元素）
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = false;

        // OrbitElement 脚本
        go.AddComponent<OrbitElement>();

        // ── 保存为 Prefab ──
        string prefabPath = prefabsDir + "/OrbitElement.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.Refresh();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log("<color=cyan>[OrbitElementPrefabCreator]</color> 轨道元素 Prefab 已生成 → " + prefabPath);
    }
}
