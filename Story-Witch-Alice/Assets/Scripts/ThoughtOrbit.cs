using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor; 
#endif

public class ThoughtOrbit : MonoBehaviour
{
    [Header("Queue")]
    public int maxElements = 12;

    [Header("Orbit")]
    public float orbitSpeed = 25f;
    public float wobbleAmplitude = 0.3f;
    public float wobbleSpeed = 1.5f;
    public RectTransform parchmentRect;
    public float edgePadding = 0.85f;
    public float maxRadiusOverride = 0f;
    public float minRadius = 0.8f;

    [Header("Overlap Prevention")]
    [Min(0f)] public float overlapPadding = 0.1f;
    [Range(1, 64)] public int overlapSolverIterations = 64;

    [Header("Visual")]
    public float minAlpha = 0.2f;
    public float maxAlpha = 1f;
    public float fadeOutDuration = 0.6f;

    [Header("Prefab")]
    public GameObject orbitElementPrefab;
    public float orbitElementScale = 2.5f;

    [Header("Layer")]
    public string orbitLayerName = "Orbit";

    private List<ElementData> queue = new List<ElementData>();
    private List<OrbitElement> orbitElements = new List<OrbitElement>();
    private readonly List<Vector2> framePositions = new List<Vector2>(12);
    private readonly List<float> frameRadii = new List<float>(12);
    
    private float maxRadius = 4f;

    public static ThoughtOrbit Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // ★ 删除 DontDestroyOnLoad，AlchemyManager 已经处理了跨场景保留
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 重新查找场景中的 Parchment（场景切换后原引用失效）
        RelinkSceneReferences();
        // 重建被场景卸载销毁的轨道视觉元素
        RebuildOrbitVisuals();
        // 每次场景加载后同步已解锁元素（兜底，防止队列中有遗漏）
        SyncFromAlchemyManager();
    }

    void RelinkSceneReferences()
    {
        if (parchmentRect == null)
        {
            GameObject parchment = GameObject.Find("Parchment");
            if (parchment != null)
                parchmentRect = parchment.GetComponent<RectTransform>();
        }
        UpdateMaxRadiusFromParchment();
    }

    /// <summary>
    /// 场景切换后重建轨道视觉元素。
    /// queue 数据层幸存但 OrbitElement 子对象被销毁时调用。
    /// </summary>
    void RebuildOrbitVisuals()
    {
        // 清理已销毁的 OrbitElement 引用
        orbitElements.RemoveAll(oe => oe == null);

        // 如果视觉元素数量已经匹配队列数量，无需重建
        if (orbitElements.Count >= queue.Count)
            return;

        // 为每个缺少视觉元素的队列条目重建 OrbitElement
        for (int i = 0; i < queue.Count; i++)
        {
            ElementData element = queue[i];
            if (element == null) continue;

            (float angle, float radius) = FindNonOverlappingPosition(element);
            OrbitElement oe = CreateOrbitElement(element, angle, radius);
            orbitElements.Insert(i, oe);
        }
        RecalculateAllAlphas();
    }

    void Start()
    {
        UpdateMaxRadiusFromParchment();
        // 首次运行时初始化基础元素队列
        if (queue.Count == 0)
        {
            InitializeQueue();
        }
        // 同步已解锁的元素
        SyncFromAlchemyManager();
    }

    /// <summary>
    /// 从 AlchemyManager 同步已解锁的非基础元素到轨道
    /// 解决场景切换后已购买/合成的元素丢失问题
    /// </summary>
    void SyncFromAlchemyManager()
    {
        if (AlchemyManager.Instance == null || AlchemyManager.Instance.allElements == null) return;

        foreach (var element in AlchemyManager.Instance.allElements)
        {
            if (element == null) continue;
            // 基础元素已在 InitializeQueue 中添加，跳过
            if (element.elementID == "fire" || element.elementID == "air" ||
                element.elementID == "water" || element.elementID == "soil") continue;

            // 如果已解锁且不在队列中，加入轨道
            if (AlchemyManager.Instance.IsElementUnlocked(element.elementID))
            {
                AddToFront(element);
            }
        }
    }

    void InitializeQueue()
    {
        string[] initialIDs = { "fire", "air", "water", "soil" };
        foreach (string id in initialIDs)
        {
            ElementData data = FindElementByID(id);
            if (data != null) InternalAddToFront(data, false);
        }
    }

    void Update()
    {
        float time = Time.time;
        framePositions.Clear();
        frameRadii.Clear();

        for (int i = 0; i < orbitElements.Count; i++)
        {
            OrbitElement oe = orbitElements[i];
            if (oe == null)
            {
                framePositions.Add(transform.position);
                frameRadii.Add(0f);
                continue;
            }

            oe.currentAngle += orbitSpeed * Mathf.Deg2Rad * Time.deltaTime;

            float phase = time * wobbleSpeed + oe.currentAngle * 0.5f;
            float wobble = Mathf.Sin(phase) * wobbleAmplitude;
            float currentRadius = oe.targetRadius + wobble;

            Vector2 pos = (Vector2)transform.position + new Vector2(
                Mathf.Cos(oe.currentAngle) * currentRadius,
                Mathf.Sin(oe.currentAngle) * currentRadius
            );

            framePositions.Add(pos);
            frameRadii.Add(GetVisualRadius(oe));
        }

        ResolveOverlaps(framePositions, frameRadii);

        for (int i = 0; i < orbitElements.Count; i++)
        {
            OrbitElement oe = orbitElements[i];
            if (oe == null) continue;
            oe.transform.position = framePositions[i];
        }

        #if UNITY_EDITOR
        if (Application.isPlaying) DrawDebugInfo();
        #endif
    }

    // =================== 公开接口 ===================

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
        float t = orbitElements.Count > 1 ? idx / (float)(orbitElements.Count - 1) : 0f;
        float alpha = Mathf.Lerp(maxAlpha, minAlpha, t);
        oe.SetAlpha(alpha);
    }

    public void SpawnPhysicalElement(ElementData data)
    {
        Vector3 worldPos = CameraUtility.MouseToWorld();
        Debug.Log($"[ThoughtOrbit] SpawnPhysicalElement - worldPos: {worldPos}");
        
        GameObject prefab = AlchemyManager.Instance?.GetPrefabForElement(data);
        if (prefab == null) return;

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
            RemoveLastElement();
        }

        queue.Insert(0, element);
        
        // 只为新元素找一个不重叠的位置
        (float angle, float radius) = FindNonOverlappingPosition(element);
        
        OrbitElement oe = CreateOrbitElement(element, angle, radius);
        orbitElements.Insert(0, oe);

        // 只更新透明度，不移位置
        RecalculateAllAlphas();

        if (animate)
            StartCoroutine(AnimateElementEntry(oe));
    }

    void RemoveLastElement()
    {
        int lastIdx = orbitElements.Count - 1;
        OrbitElement last = orbitElements[lastIdx];
        StartCoroutine(FadeOutAndDestroy(last));
        queue.RemoveAt(lastIdx);
        orbitElements.RemoveAt(lastIdx);
    }

    void MoveToFront(int currentIndex)
    {
        if (currentIndex == 0) return;

        // 只改队列顺序和透明度，位置不动
        ElementData data = queue[currentIndex];
        queue.RemoveAt(currentIndex);
        queue.Insert(0, data);

        OrbitElement oe = orbitElements[currentIndex];
        orbitElements.RemoveAt(currentIndex);
        orbitElements.Insert(0, oe);

        RecalculateAllAlphas();
    }

    /// <summary>
    /// 找到一个与已有元素不重叠的新位置
    /// 用多轮尝试：从外到内，从不同角度，直到找到足够远离其他元素的位置
    /// </summary>
    (float angle, float radius) FindNonOverlappingPosition(ElementData element)
    {
        // 如果还没有元素，放在最外圈随机位置
        if (orbitElements.Count == 0)
        {
            return (Random.Range(0f, 2f * Mathf.PI), maxRadius);
        }

        float newElementRadius = EstimateVisualRadius(element);
        float bestClearance = float.NegativeInfinity;
        float bestAngle = Random.Range(0f, 2f * Mathf.PI);
        float bestRadius = maxRadius;
        float randomAngleOffset = Random.Range(0f, 2f * Mathf.PI);
        const float goldenAngle = 2.39996323f;

        // 优先随机尝试，之后使用黄金角补齐覆盖，避免连续随机命中同一区域。
        for (int attempt = 0; attempt < 160; attempt++)
        {
            float availableMaxRadius = Mathf.Max(minRadius, maxRadius - newElementRadius);
            float r = Random.Range(minRadius, availableMaxRadius);
            float a = attempt < 48
                ? Random.Range(0f, 2f * Mathf.PI)
                : randomAngleOffset + attempt * goldenAngle;

            Vector2 testPos = (Vector2)transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            float minimumClearance = float.PositiveInfinity;

            foreach (var oe in orbitElements)
            {
                if (oe == null) continue;
                float requiredDistance = newElementRadius + GetVisualRadius(oe) + overlapPadding;
                float clearance = Vector2.Distance(oe.transform.position, testPos) - requiredDistance;
                minimumClearance = Mathf.Min(minimumClearance, clearance);
            }

            if (minimumClearance >= 0f)
            {
                return (a, r);
            }

            if (minimumClearance > bestClearance)
            {
                bestClearance = minimumClearance;
                bestAngle = a;
                bestRadius = r;
            }
        }

        // 空间紧张时使用最宽松的候选点，随后由每帧约束继续分离。
        return (bestAngle, bestRadius);
    }

    void ResolveOverlaps(List<Vector2> positions, List<float> radii)
    {
        if (positions.Count < 2) return;

        Vector2 center = transform.position;
        int iterations = Mathf.Max(1, overlapSolverIterations);

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            bool foundOverlap = false;

            for (int i = 0; i < positions.Count - 1; i++)
            {
                if (radii[i] <= 0f) continue;

                for (int j = i + 1; j < positions.Count; j++)
                {
                    if (radii[j] <= 0f) continue;

                    Vector2 delta = positions[j] - positions[i];
                    float minimumDistance = radii[i] + radii[j] + overlapPadding;
                    float sqrDistance = delta.sqrMagnitude;
                    if (sqrDistance >= minimumDistance * minimumDistance) continue;

                    foundOverlap = true;
                    float distance = Mathf.Sqrt(sqrDistance);
                    Vector2 direction;
                    if (distance > 0.0001f)
                    {
                        direction = delta / distance;
                    }
                    else
                    {
                        // 完全重合时使用稳定的确定性方向，避免随机抖动。
                        float angle = (i * 37f + j * 83f) * Mathf.Deg2Rad;
                        direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    }

                    Vector2 correction = direction * ((minimumDistance - distance) * 0.5f + 0.0001f);
                    positions[i] -= correction;
                    positions[j] += correction;
                }
            }

            for (int i = 0; i < positions.Count; i++)
            {
                if (radii[i] <= 0f) continue;
                positions[i] = ClampToOrbitArea(positions[i], radii[i], center);
            }

            if (!foundOverlap) break;
        }

        // 极端密集或完全重合的初始布局可能让局部求解陷入死角。
        // 最后使用安全环形排列兜底，确保画面上不会保留任何重叠。
        if (HasOverlaps(positions, radii))
            ArrangeOnSafeRing(positions, radii, center);
    }

    bool HasOverlaps(List<Vector2> positions, List<float> radii)
    {
        for (int i = 0; i < positions.Count - 1; i++)
        {
            if (radii[i] <= 0f) continue;

            for (int j = i + 1; j < positions.Count; j++)
            {
                if (radii[j] <= 0f) continue;
                float minimumDistance = radii[i] + radii[j] + overlapPadding;
                if ((positions[j] - positions[i]).sqrMagnitude < minimumDistance * minimumDistance)
                    return true;
            }
        }

        return false;
    }

    void ArrangeOnSafeRing(List<Vector2> positions, List<float> radii, Vector2 center)
    {
        List<int> activeIndices = new List<int>();
        float largestRadius = 0f;

        for (int i = 0; i < positions.Count; i++)
        {
            if (radii[i] <= 0f) continue;
            activeIndices.Add(i);
            largestRadius = Mathf.Max(largestRadius, radii[i]);
        }

        if (activeIndices.Count == 0) return;

        activeIndices.Sort((a, b) =>
        {
            Vector2 offsetA = positions[a] - center;
            Vector2 offsetB = positions[b] - center;
            return Mathf.Atan2(offsetA.y, offsetA.x).CompareTo(Mathf.Atan2(offsetB.y, offsetB.x));
        });

        Vector2 firstOffset = positions[activeIndices[0]] - center;
        float startAngle = firstOffset.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(firstOffset.y, firstOffset.x)
            : 0f;
        float ringRadius = Mathf.Max(minRadius, maxRadius - largestRadius);
        float angleStep = 2f * Mathf.PI / activeIndices.Count;

        for (int slot = 0; slot < activeIndices.Count; slot++)
        {
            float angle = startAngle + slot * angleStep;
            positions[activeIndices[slot]] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;
        }
    }

    Vector2 ClampToOrbitArea(Vector2 position, float visualRadius, Vector2 center)
    {
        Vector2 offset = position - center;
        float distance = offset.magnitude;
        Vector2 direction = distance > 0.0001f ? offset / distance : Vector2.right;
        float maximumCenterRadius = Mathf.Max(minRadius, maxRadius - visualRadius);
        float clampedDistance = Mathf.Clamp(distance, minRadius, maximumCenterRadius);
        return center + direction * clampedDistance;
    }

    float GetVisualRadius(OrbitElement orbitElement)
    {
        if (orbitElement == null) return 0f;

        SpriteRenderer spriteRenderer = orbitElement.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector3 extents = spriteRenderer.bounds.extents;
            return Mathf.Max(extents.x, extents.y);
        }

        return Mathf.Max(0.05f, orbitElementScale * 0.15f);
    }

    float EstimateVisualRadius(ElementData element)
    {
        Sprite sprite = ResolveElementSprite(element);
        if (sprite == null)
            return Mathf.Max(0.05f, orbitElementScale * 0.15f);

        Vector3 parentScale = transform.lossyScale;
        float worldScale = orbitElementScale * Mathf.Max(Mathf.Abs(parentScale.x), Mathf.Abs(parentScale.y));
        Vector3 extents = sprite.bounds.extents;
        return Mathf.Max(extents.x, extents.y) * worldScale;
    }

    OrbitElement CreateOrbitElement(ElementData data, float angle, float radius)
    {
        Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
        
        GameObject go = Instantiate(orbitElementPrefab, pos, Quaternion.identity);
        go.transform.SetParent(transform);
        go.name = "Orbit_" + data.elementID;

        int orbitLayer = LayerMask.NameToLayer(orbitLayerName);
        if (orbitLayer >= 0) go.layer = orbitLayer;

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = go.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;
        rb.simulated = true;

        OrbitElement oe = go.GetComponent<OrbitElement>();
        if (oe == null) oe = go.AddComponent<OrbitElement>();

        oe.elementData = data;
        oe.orbitManager = this;
        oe.targetRadius = radius;
        oe.currentAngle = angle;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ResolveElementSprite(data);
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 10;

        go.transform.localScale = Vector3.one * orbitElementScale;

        Animator anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        RuntimeAnimatorController animCtrl = GetElementAnimController(data.elementID);
        if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
        ApplyElementEffect(go, data);

        return oe;
    }

    void RecalculateAllAlphas()
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

    // =================== 动画 ===================

    IEnumerator AnimateElementEntry(OrbitElement oe)
    {
        if (oe == null) yield break;

        Vector3 targetPos = oe.transform.position;
        float targetScale = oe.transform.localScale.x;

        oe.transform.position = transform.position;
        oe.transform.localScale = Vector3.zero;
        oe.SetAlpha(0f);

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (oe == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = 1f - Mathf.Pow(1f - t, 3f);
            oe.transform.position = Vector3.Lerp(transform.position, targetPos, t);
            oe.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * targetScale, t);
            oe.SetAlpha(t);
            yield return null;
        }

        if (oe != null)
        {
            oe.transform.position = targetPos;
            oe.transform.localScale = Vector3.one * targetScale;
            oe.SetAlpha(1f);
        }
    }

    IEnumerator FadeOutAndDestroy(OrbitElement oe)
    {
        if (oe == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            if (oe == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            oe.SetAlpha(1f - t);
            oe.transform.localScale = Vector3.Lerp(Vector3.one * orbitElementScale, Vector3.zero, t);
            yield return null;
        }

        if (oe != null) Destroy(oe.gameObject);
    }

    // =================== 资源查找 ===================

    ElementData FindElementByID(string id)
    {
        if (AlchemyManager.Instance == null) return null;

        if (AlchemyManager.Instance.allElements != null)
        {
            foreach (var elem in AlchemyManager.Instance.allElements)
                if (elem != null && elem.elementID == id) return elem;
        }

        var recipes = AlchemyManager.Instance.allRecipes;
        if (recipes != null)
        {
            foreach (var recipe in recipes)
            {
                if (recipe.product != null && recipe.product.elementID == id) return recipe.product;
            }
            foreach (var recipe in recipes)
            {
                if (recipe.ingredients != null)
                {
                    foreach (var ing in recipe.ingredients)
                        if (ing != null && ing.elementID == id) return ing;
                }
            }
        }
        return null;
    }

    Sprite ResolveElementSprite(ElementData data)
    {
        if (data == null) return null;
        if (data.elementIcon != null) return data.elementIcon;

        // 从编辑器加载专属预制体的 Sprite
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(data.elementID))
        {
            string path = $"Assets/Prefabs/Physic{data.elementID}.prefab";
            GameObject specific = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (specific != null)
            {
                SpriteRenderer sr = specific.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) return sr.sprite;
            }
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

    void ApplyElementEffect(GameObject go, ElementData data)
    {
        switch (data.elementID)
        {
            case "steam":
                SteamController steamCtrl = go.GetComponent<SteamController>();
                if (steamCtrl == null) steamCtrl = go.AddComponent<SteamController>();
                Transform steamPsTransform = go.transform.Find("steam");
                if (steamPsTransform != null)
                {
                    ParticleSystem ps = steamPsTransform.GetComponent<ParticleSystem>();
                    if (ps != null) steamCtrl.steamParticles = ps;
                }
                break;
        }
    }

    // =================== 调试 ===================

    #if UNITY_EDITOR
    void DrawDebugInfo()
    {
        // 显示每个元素的安全距离范围
        foreach (var oe in orbitElements)
        {
            if (oe == null) continue;
            Vector3 pos = oe.transform.position;
            DrawCircle(pos, GetVisualRadius(oe) + overlapPadding * 0.5f, Color.red, 0.2f);
        }
        
        // 显示可用区域边界
        DrawCircle(transform.position, maxRadius, Color.green, 0.3f);
        DrawCircle(transform.position, minRadius, Color.green, 0.3f);
    }

    void DrawCircle(Vector3 center, float radius, Color color, float alpha)
    {
        Color c = color;
        c.a = alpha;
        int segments = 48;
        float step = 2f * Mathf.PI / segments;
        Vector3 prev = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * step;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
            Debug.DrawLine(prev, next, c);
            prev = next;
        }
    }
    #endif

    public List<ElementData> GetQueue() => queue;
}
