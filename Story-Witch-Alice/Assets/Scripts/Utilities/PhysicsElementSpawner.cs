using UnityEngine;

public static class PhysicsElementSpawner
{
    /// <summary>
    /// 在鼠标位置生成物理元素
    /// </summary>
    public static GameObject SpawnAtMouse(ElementData elementData, float z = 0f)
    {
        if (elementData == null)
        {
            Debug.LogError("[PhysicsElementSpawner] elementData 为空！");
            return null;
        }

        GameObject prefab = AlchemyManager.Instance?.GetPrefabForElement(elementData);
        if (prefab == null)
        {
            Debug.LogError($"[PhysicsElementSpawner] 找不到物理预制体: {elementData.elementID}");
            return null;
        }

        Vector3 spawnPos = CameraUtility.MouseToWorld(z);
        GameObject go = GameObject.Instantiate(prefab, spawnPos, Quaternion.identity);

        PhysicsElement pe = go.GetComponent<PhysicsElement>();
        if (pe != null)
        {
            pe.elementData = elementData;
            pe.sourceSlot = null;
        }

        return go;
    }

    /// <summary>
    /// 在指定位置生成物理元素，并禁用物理（用于拖拽）
    /// </summary>
    public static GameObject SpawnForDrag(ElementData elementData, float z = 0f)
    {
        GameObject go = SpawnAtMouse(elementData, z);
        if (go == null) return null;

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        Collider2D[] cols = go.GetComponents<Collider2D>();
        foreach (var col in cols) col.enabled = false;

        return go;
    }

    /// <summary>
    /// 释放物理元素（开启物理）
    /// </summary>
    public static void Release(GameObject go)
    {
        if (go == null) return;

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 1f;
        }

        Collider2D[] cols = go.GetComponents<Collider2D>();
        foreach (var col in cols) col.enabled = true;
    }
}