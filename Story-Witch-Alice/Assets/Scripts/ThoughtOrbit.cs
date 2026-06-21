using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; 
#endif

/// <summary>
/// 鱼缸轨道系统。
/// 维护一个元素队列（上限20），元素绕中心做固定环绕运动，
/// 带径向简谐波动效果，点击元素可拖出到场景。
/// </summary>
public class ThoughtOrbit : MonoBehaviour
{
    [Header("Queue")]
    public int maxElements = 20;

    [Header("Orbit")]
    public float orbitSpeed = 25f;              // 环绕速度（度/秒）
    public float wobbleAmplitude = 0.3f;        // 径向波动幅度
    public float wobbleSpeed = 1.5f;            // 波动频率
    public RectTransform parchmentRect;         // Parchment 面板（用于计算轨道半径范围）
    public float edgePadding = 0.85f;           // 半径边距系数（<1 留白边）
    public float maxRadiusOverride = 0f;        // >0 时强制使用这个值，不自动计算
    public float minRadius = 0.8f;              // 最内圈最小半径

    [Header("Visual")]
    public float minAlpha = 0.2f;
    public float maxAlpha = 1f;
    public float fadeOutDuration = 0.6f;        // 被挤出队列时淡出时间

    [Header("Prefab")]
    public GameObject orbitElementPrefab;       // 轨道元素预制体
    public float orbitElementScale = 2.5f;      // 图标放大倍数

    [Header("Layer")]
    public string orbitLayerName = "Orbit";     // 轨道层名（需人工在 Project Settings 添加）

    private List<ElementData> queue = new List<ElementData>();
    private List<OrbitElement> orbitElements = new List<OrbitElement>();

    // 计算出的轨道半径分布
    private float maxRadius = 4f;

    // =================== 初始化 ===================

    void Start()
    {
        InitializeQueue();
    }

    void InitializeQueue()
    {
        string[] initialIDs = { "fire", "air", "water", "soil" };
        foreach (string id in initialIDs)
        {
            ElementData data = FindElementByID(id);
            if (data != null) InternalAddToFront(data, false);
        }
        RecalculateAllParameters();
    }

    // =================== 固定轨道更新 ===================

    void Update()
    {
        float time = Time.time;
        for (int i = 0; i < orbitElements.Count; i++)
        {
            OrbitElement oe = orbitElements[i];
            if (oe == null) continue;

            // 角度匀速递增
            oe.currentAngle += orbitSpeed * Mathf.Deg2Rad * Time.deltaTime;

            // 径向简谐运动（在圆轨迹上小幅度波动）
            float phase = time * wobbleSpeed + oe.currentAngle * 0.5f;
            float wobble = Mathf.Sin(phase) * wobbleAmplitude;
            float currentRadius = oe.targetRadius + wobble;

            // 计算位置
            Vector3 pos = transform.position + new Vector3(
                Mathf.Cos(oe.currentAngle) * currentRadius,
                Mathf.Sin(oe.currentAngle) * currentRadius,
                0
            );
            oe.transform.position = pos;
        }
    }

    // =================== 队列管理 ===================

    public void AddToFront(ElementData element)
    {
        int idx = queue.FindIndex(e => e.elementID == element.elementID);
        if (idx >= 0)
        {
            MoveToFront(idx);
            return;
        }
        InternalAddToFront(element, true);
    }

    public void MoveToFront(string elementID)
    {
        int idx = queue.FindIndex(e => e.elementID == elementID);
        if (idx < 0) return;
        MoveToFront(idx);
    }

    public void UpdateElementAlpha(OrbitElement oe)
    {
        int idx = orbitElements.IndexOf(oe);
        if (idx < 0) return;
        float t = queue.Count > 1 ? idx / (float)(queue.Count - 1) : 0f;
        float alpha = Mathf.Lerp(maxAlpha, minAlpha, t);
        oe.SetAlpha(alpha);
    }

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

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.velocity = Vector2.down * 2f;
        }
    }

    // =================== 内部实现 ===================

    void InternalAddToFront(ElementData element, bool animate)
    {
        if (queue.Count >= maxElements)
        {
            int lastIdx = queue.Count - 1;
            OrbitElement last = orbitElements[lastIdx];
            StartCoroutine(FadeOutAndDestroy(last));
            queue.RemoveAt(lastIdx);
            orbitElements.RemoveAt(lastIdx);
        }

        queue.Insert(0, element);
        OrbitElement oe = CreateOrbitElement(element);
        orbitElements.Insert(0, oe);

        // 不重排已有轨道，只更新透明度
        RecalculateAlphas();

        if (animate)
            StartCoroutine(AnimateElementEntry(oe));
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

        // 只更新透明度，不改变已有的轨道位置
        RecalculateAlphas();
    }

    OrbitElement CreateOrbitElement(ElementData data)
{
    // =================== 计算半径（找最大空隙插入，避免全挤同一轨道） ===================
    float r = FindBestRadius();

    // =================== 计算初始角度（避免和已有元素重叠） ===================
    float angle;
    if (orbitElements.Count == 0)
    {
        // 第一个元素，随机角度
        angle = Random.Range(0f, 2f * Mathf.PI);
    }
    else
    {
        // 找到已有元素中角度间隔最大的空隙，把新元素放在空隙中间
        float bestAngle = 0f;
        float bestGap = -1f;
        
        // 收集所有已有元素的角度，排序
        List<float> existingAngles = new List<float>();
        foreach (var elem in orbitElements)
        {
            if (elem != null)
                existingAngles.Add(elem.currentAngle);
        }
        existingAngles.Sort();
        
        // 检查相邻元素之间的空隙（包括首尾之间）
        for (int i = 0; i < existingAngles.Count; i++)
        {
            int next = (i + 1) % existingAngles.Count;
            float gap;
            if (next == 0)
                // 首尾之间的空隙：最后一个到 2π，加上 0 到第一个
                gap = (2f * Mathf.PI - existingAngles[i]) + existingAngles[next];
            else
                gap = existingAngles[next] - existingAngles[i];
            
            if (gap > bestGap)
            {
                bestGap = gap;
                // 新元素放在空隙中间
                float midAngle = existingAngles[i] + gap / 2f;
                if (midAngle > 2f * Mathf.PI) midAngle -= 2f * Mathf.PI;
                bestAngle = midAngle;
            }
        }
        
        // 如果只有一个元素，放在它对面
        if (orbitElements.Count == 1)
        {
            bestAngle = existingAngles[0] + Mathf.PI;
            if (bestAngle > 2f * Mathf.PI) bestAngle -= 2f * Mathf.PI;
        }
        
        angle = bestAngle;
    }

    // =================== 生成物体 ===================
    Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * r;
    GameObject go = Instantiate(orbitElementPrefab, pos, Quaternion.identity);
    go.transform.SetParent(transform);
    go.name = "Orbit_" + data.elementID;

    // 碰撞层
    int orbitLayer = LayerMask.NameToLayer(orbitLayerName);
    if (orbitLayer >= 0) go.layer = orbitLayer;

    // Rigidbody2D 设为 kinematic，不参与物理但保留碰撞检测
    Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
    if (rb != null)
    {
        rb.isKinematic = true;
        rb.simulated = true;
    }

    OrbitElement oe = go.GetComponent<OrbitElement>();
    if (oe == null) oe = go.AddComponent<OrbitElement>();

    oe.elementData = data;
    oe.orbitManager = this;
    oe.currentAngle = angle;
    oe.targetRadius = r;

    // 贴图
    SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
    if (sr == null) sr = go.AddComponent<SpriteRenderer>();
    sr.sprite = ResolveElementSprite(data);
    sr.sortingLayerName = "Foreground";
    sr.sortingOrder = 10;

    go.transform.localScale = Vector3.one * orbitElementScale;

    // 动画
    Animator anim = go.GetComponent<Animator>();
    if (anim == null) anim = go.AddComponent<Animator>();
    RuntimeAnimatorController animCtrl = GetElementAnimController(data.elementID);
    if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;

    return oe;
}

    void RecalculateAllParameters()
    {
        UpdateMaxRadiusFromParchment();

        int count = orbitElements.Count;
        for (int i = 0; i < count; i++)
        {
            OrbitElement oe = orbitElements[i];
            if (oe == null) continue;

            // 从外到内均匀分布：队首（i=0）在最外圈，队尾在最内圈
            float r;
            if (count <= 1)
                r = maxRadius;
            else
                r = Mathf.Lerp(maxRadius, minRadius, i / (float)(count - 1));
            oe.targetRadius = r;

            
           
            float t = count > 1 ? i / (float)(count - 1) : 0f;
            float alpha = Mathf.Lerp(maxAlpha, minAlpha, t);
            oe.SetAlpha(alpha);
        }
    }

    /// <summary>
    /// 只更新透明度（队列重排时调用），不改轨道位置
    /// </summary>
    void RecalculateAlphas()
    {
        int count = orbitElements.Count;
        for (int i = 0; i < count; i++)
        {
            OrbitElement oe = orbitElements[i];
            if (oe == null) continue;
            float t = count > 1 ? i / (float)(count - 1) : 0f;
            float alpha = Mathf.Lerp(maxAlpha, minAlpha, t);
            oe.SetAlpha(alpha);
        }
    }

    void UpdateMaxRadiusFromParchment()
    {
        if (maxRadiusOverride > 0f)
        {
            maxRadius = maxRadiusOverride;
            return;
        }
        if (parchmentRect == null) return;

        Vector3[] corners = new Vector3[4];
        parchmentRect.GetWorldCorners(corners);
        float width = Vector3.Distance(corners[0], corners[3]);
        float height = Vector3.Distance(corners[0], corners[1]);
        float calculated = Mathf.Min(width, height) / 2f * edgePadding;
        if (calculated > 0.5f) maxRadius = calculated;
    }

    // =================== 入场 / 淡出动画 ===================

    IEnumerator AnimateElementEntry(OrbitElement oe)
    {
        if (oe == null) yield break;

        Vector3 targetPos = oe.transform.position;
        float targetScale = oe.transform.localScale.x;

        // 从中心出发，缩放 + 位置一起飞入轨道
        oe.transform.position = transform.position;
        oe.transform.localScale = Vector3.zero;
        oe.SetAlpha(0f);

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (oe == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = 1f - Mathf.Pow(1f - t, 3f);

            oe.transform.position = Vector3.Lerp(transform.position, targetPos, t);
            float s = Mathf.Lerp(0f, targetScale, t);
            oe.transform.localScale = Vector3.one * s;
            oe.SetAlpha(Mathf.Lerp(0f, 1f, t));

            yield return null;
        }

        if (oe != null)
        {
            oe.transform.position = targetPos;
            oe.transform.localScale = Vector3.one * targetScale;
        }
    }

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

        if (AlchemyManager.Instance.allElements != null)
        {
            foreach (var elem in AlchemyManager.Instance.allElements)
            {
                if (elem != null && elem.elementID == id)
                    return elem;
            }
        }

        var recipes = AlchemyManager.Instance.allRecipes;
        if (recipes != null)
        {
            foreach (var recipe in recipes)
            {
                if (recipe.product != null && recipe.product.elementID == id)
                    return recipe.product;
            }
        }

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

    Sprite ResolveElementSprite(ElementData data)
    {
        if (data.elementIcon != null)
            return data.elementIcon;

        string id = data.elementID;
        if (string.IsNullOrEmpty(id)) return OrbitElement.GetFallbackSpriteStatic();

#if UNITY_EDITOR
        string iconDir = "Assets/Data/ArtResourceData/Design/Icon/";
        string[] candidates = {
            $"{iconDir}{id}.png"
        };
        foreach (string p in candidates)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (s != null) return s;
        }

        string prefabPath = $"Assets/Prefabs/Physic{id}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            SpriteRenderer psr = prefab.GetComponent<SpriteRenderer>();
            if (psr != null && psr.sprite != null)
                return psr.sprite;
        }
#endif

        return OrbitElement.GetFallbackSpriteStatic();
    }

    RuntimeAnimatorController GetElementAnimController(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
#if UNITY_EDITOR
        string path = $"Assets/Animations/PhysicElement Animation/Physic{id}.controller";
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
#endif
        return null;
    }

    /// <summary>
    /// 在已有元素的半径之间找最大空隙插入，避免新元素全部挤在同一轨道
    /// </summary>
    float FindBestRadius()
    {
        if (orbitElements.Count == 0) return maxRadius;

        // 收集所有已有元素的半径
        List<float> existingRadii = new List<float>();
        foreach (var oe in orbitElements)
        {
            if (oe != null)
                existingRadii.Add(oe.targetRadius);
        }
        existingRadii.Sort();

        // 检查每个空隙（包括边缘空隙：minRadius~第一个元素，最后一个元素~maxRadius）
        float bestRadius = maxRadius;
        float bestGap = -1f;

        // 下边缘空隙：minRadius ~ 第一个元素
        float lowerGap = existingRadii[0] - minRadius;
        if (lowerGap > bestGap)
        {
            bestGap = lowerGap;
            bestRadius = minRadius + lowerGap / 2f;
        }

        // 元素之间的空隙
        for (int i = 0; i < existingRadii.Count - 1; i++)
        {
            float gap = existingRadii[i + 1] - existingRadii[i];
            if (gap > bestGap)
            {
                bestGap = gap;
                bestRadius = existingRadii[i] + gap / 2f;
            }
        }

        // 上边缘空隙：最后一个元素 ~ maxRadius
        float upperGap = maxRadius - existingRadii[existingRadii.Count - 1];
        if (upperGap > bestGap)
        {
            bestRadius = existingRadii[existingRadii.Count - 1] + upperGap / 2f;
        }

        return Mathf.Clamp(bestRadius, minRadius, maxRadius);
    }

    public List<ElementData> GetQueue() => queue;
}