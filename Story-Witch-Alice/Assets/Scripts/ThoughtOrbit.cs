using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; 
#endif

/// <summary>
/// 鱼缸轨道系统。
/// 维护一个元素队列（上限20），元素绕中心做行星运动，
/// 物理引擎处理碰撞弹开，点击元素可拖出到场景。
/// </summary>
public class ThoughtOrbit : MonoBehaviour
{
    [Header("Queue")]
    public int maxElements = 20;

    [Header("Orbit Physics")]
    public float centerGravity = 5f;         // 向心引力强度（越小越慢）
    public float baseRadius = 4f;            // 最内圈半径
    public float radiusStep = 0.35f;         // 每增加一个位置半径增量
    public float baseTangentialSpeed = 1.2f; // 切向速度基数（越小越慢）

    [Header("Visual")]
    public float minAlpha = 0.2f;
    public float maxAlpha = 1f;
    public float fadeOutDuration = 0.6f;     // 被挤出队列时淡出时间

    [Header("Prefab")]
    public GameObject orbitElementPrefab;    // 轨道元素预制体
    public float orbitElementScale = 2.5f;   // 图标放大倍数

    [Header("Layer")]
    public string orbitLayerName = "Orbit";  // 轨道层名（需人工在 Project Settings 添加）

    private List<ElementData> queue = new List<ElementData>();
    private List<OrbitElement> orbitElements = new List<OrbitElement>();

    void Start()
    {
        InitializeQueue();
    }

    // =================== 队列管理 ===================

    void InitializeQueue()
    {
        string[] initialIDs = { "air", "water", "fire", "stone" };
        foreach (string id in initialIDs)
        {
            ElementData data = FindElementByID(id);
            if (data != null) InternalAddToFront(data, false);
        }
        RecalculateAllParameters();
    }

    /// <summary>
    /// 新元素合成 → 加入队首
    /// </summary>
    public void AddToFront(ElementData element)
    {
        // 已在队列中 → 直接提到队首
        int idx = queue.FindIndex(e => e.elementID == element.elementID);
        if (idx >= 0)
        {
            MoveToFront(idx);
            return;
        }

        InternalAddToFront(element, true);
    }

    /// <summary>
    /// 元素入锅 → 提到队首
    /// </summary>
    public void MoveToFront(string elementID)
    {
        int idx = queue.FindIndex(e => e.elementID == elementID);
        if (idx < 0) return;
        MoveToFront(idx);
    }

    /// <summary>
    /// 更新单个元素的 alpha（由 OrbitElement 悬停恢复时调用）
    /// </summary>
    public void UpdateElementAlpha(OrbitElement oe)
    {
        int idx = orbitElements.IndexOf(oe);
        if (idx < 0) return;
        float t = queue.Count > 1 ? idx / (float)(queue.Count - 1) : 0f;
        float alpha = Mathf.Lerp(maxAlpha, minAlpha, t);
        oe.SetAlpha(alpha);
    }

    /// <summary>
    /// 点击轨道元素时调用 → 生成物理元素供玩家拖拽
    /// 复用 AlchemyManager 的智能 prefab 选择逻辑
    /// </summary>
    public void SpawnPhysicalElement(ElementData data, Vector3 worldPos)
    {
        GameObject prefab = AlchemyManager.Instance?.GetPrefabForElement(data);
        if (prefab == null)
        {
            Debug.LogWarning("ThoughtOrbit: 无法找到合适的 prefab，检查 AlchemyManager 的配置");
            return;
        }

        GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
        PhysicsElement pe = go.GetComponent<PhysicsElement>();
        if (pe != null)
        {
            pe.elementData = data;
            pe.sourceSlot = null;
        }

        // 让元素自然下落（松开鼠标时已有惯性逻辑）
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.velocity = Vector2.down * 2f; // 给一点向下的初速
        }
    }

    // =================== 内部实现 ===================

    void InternalAddToFront(ElementData element, bool animate)
    {
        // 队列满了 → 挤掉队尾
        if (queue.Count >= maxElements)
        {
            int lastIdx = queue.Count - 1;
            OrbitElement last = orbitElements[lastIdx];
            StartCoroutine(FadeOutAndDestroy(last));
            queue.RemoveAt(lastIdx);
            orbitElements.RemoveAt(lastIdx);
        }

        // 插入队首
        queue.Insert(0, element);
        OrbitElement oe = CreateOrbitElement(element);
        orbitElements.Insert(0, oe);

        RecalculateAllParameters();
    }

    void MoveToFront(int currentIndex)
    {
        if (currentIndex == 0) return;

        ElementData data = queue[currentIndex];
        queue.RemoveAt(currentIndex);
        queue.Insert(0, data);

        OrbitElement oe = orbitElements[currentIndex];
        orbitElements.RemoveAt(currentIndex);
        orbitElements.Insert(0, oe);

        RecalculateAllParameters();
    }

    OrbitElement CreateOrbitElement(ElementData data)
    {
        // 从队列索引计算该元素的轨道半径
        float r = baseRadius + orbitElements.Count * radiusStep;
        float angle = Random.Range(0f, 2f * Mathf.PI);

        // 直接生成在轨道位置上，而不是中心（避免中心引力过大吸住）
        Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * r;
        GameObject go = Instantiate(orbitElementPrefab, pos, Quaternion.identity);
        go.transform.SetParent(transform);
        go.name = "Orbit_" + data.elementID;

        // 设置碰撞层（只与自身碰撞）
        int orbitLayer = LayerMask.NameToLayer(orbitLayerName);
        if (orbitLayer >= 0) go.layer = orbitLayer;

        OrbitElement oe = go.GetComponent<OrbitElement>();
        if (oe == null) oe = go.AddComponent<OrbitElement>();

        oe.elementData = data;
        oe.orbitManager = this;
        oe.currentAngle = angle;
        oe.targetRadius = r;

        // 设置贴图
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ResolveElementSprite(data);
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 10;

        // 图标放大
        go.transform.localScale = Vector3.one * orbitElementScale;

        // 加 Animator 播放帧动画
        Animator anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        RuntimeAnimatorController animCtrl = GetElementAnimController(data.elementID);
        if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;

        // 设置切向初速度（维持圆轨道：v = sqrt(G / r)）
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 radialDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 tangentDir = new Vector2(-radialDir.y, radialDir.x);
            float orbitalSpeed = Mathf.Sqrt(centerGravity / r);
            rb.velocity = tangentDir * orbitalSpeed;
        }

        return oe;
    }

    void RecalculateAllParameters()
    {
        for (int i = 0; i < orbitElements.Count; i++)
        {
            OrbitElement oe = orbitElements[i];
            if (oe == null) continue;

            float r = baseRadius + i * radiusStep;
            oe.targetRadius = r;

            // 更新透明度
            float t = queue.Count > 1 ? i / (float)(queue.Count - 1) : 0f;
            float alpha = Mathf.Lerp(maxAlpha, minAlpha, t);
            oe.SetAlpha(alpha);
        }
    }

    // =================== 物理 ===================

    void FixedUpdate()
    {
        foreach (var oe in orbitElements)
        {
            if (oe == null) continue;

            Rigidbody2D rb = oe.GetComponent<Rigidbody2D>();
            if (rb == null) continue;

            Vector2 dir = (transform.position - oe.transform.position);
            float dist = dir.magnitude;
            if (dist < 0.01f) continue;

            // 万有引力：F = G / r²（单位质量）
            float force = centerGravity / (dist * dist + 0.1f);
            rb.AddForce(dir.normalized * force, ForceMode2D.Force);

            // 软阻尼：防止飞太远
            rb.velocity *= 0.9995f;
        }
    }

    // =================== 动画 ===================

    IEnumerator FadeOutAndDestroy(OrbitElement oe)
    {
        if (oe == null) yield break;

        float elapsed = 0f;
        SpriteRenderer sr = oe.GetComponent<SpriteRenderer>();
        Vector3 startScale = oe.transform.localScale;
        Color startColor = sr != null ? sr.color : Color.white;

        while (elapsed < fadeOutDuration)
        {
            if (oe == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            // 缩小 + 淡出
            oe.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (sr != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                sr.color = c;
            }
            yield return null;
        }

        if (oe != null) Destroy(oe.gameObject);
    }

    // =================== 工具 ===================

    ElementData FindElementByID(string id)
    {
        if (AlchemyManager.Instance == null) return null;

        // 1. 从 allElements 全量列表查找（包含基础元素和合成产物）
        if (AlchemyManager.Instance.allElements != null)
        {
            foreach (var elem in AlchemyManager.Instance.allElements)
            {
                if (elem != null && elem.elementID == id)
                    return elem;
            }
        }

        // 2. 从配方产物查找
        var recipes = AlchemyManager.Instance.allRecipes;
        if (recipes != null)
        {
            foreach (var recipe in recipes)
            {
                if (recipe.product != null && recipe.product.elementID == id)
                    return recipe.product;
            }
        }

        // 3. 备用：检查配方原料
        foreach (var recipe in recipes)
        {
            if (recipe.ingredients != null)
            {
                foreach (var ing in recipe.ingredients)
                {
                    if (ing != null && ing.elementID == id)
                        return ing;
                }
            }
        }

        return null;
    }

    // =================== 贴图查找 ===================

    /// <summary>
    /// 按优先级找轨道元素的贴图：
    /// 1. ElementData.elementIcon（用户手动在 Inspector 拖的）
    /// 2. Assets/Data/ArtResourceData/Design/Icon/{id}.png
    /// 3. Physic{id}.prefab 上 SpriteRenderer 的默认 sprite
    /// 4. 粉紫色马赛克
    /// </summary>
    Sprite ResolveElementSprite(ElementData data)
    {
        // 第一优先：ElementData 上用户拖的贴图
        if (data.elementIcon != null)
            return data.elementIcon;

        string id = data.elementID;
        if (string.IsNullOrEmpty(id)) return OrbitElement.GetFallbackSpriteStatic();

#if UNITY_EDITOR
        // 第二优先：从 Icon 目录按命名查找
        string iconDir = "Assets/Data/ArtResourceData/Design/Icon/";
        string[] candidates = {
            $"{iconDir}{id}.png"
        };
        foreach (string p in candidates)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (s != null) return s;
        }

        // 第三优先：从同名 PhysicXXX.prefab 读取默认 sprite
        string prefabPath = $"Assets/Prefabs/Physic{id}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            SpriteRenderer psr = prefab.GetComponent<SpriteRenderer>();
            if (psr != null && psr.sprite != null)
                return psr.sprite;
        }
#endif

        // 最终 fallback：粉紫色 2×2 马赛克
        return OrbitElement.GetFallbackSpriteStatic();
    }

    /// <summary>
    /// 从 Animations/PhysicElement Animation/ 查找元素的帧动画控制器。
    /// 有 .controller 文件 → 返回动画（如 fire、water）
    /// 没有 → null（保持静态贴图，如 stone）
    /// </summary>
    RuntimeAnimatorController GetElementAnimController(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
#if UNITY_EDITOR
        string path = $"Assets/Animations/PhysicElement Animation/Physic{id}.controller";
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
#endif
        return null;
    }

    public List<ElementData> GetQueue() => queue;
}
