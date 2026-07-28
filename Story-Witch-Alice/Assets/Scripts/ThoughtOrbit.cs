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
    public int maxElements = 20;

    [Header("Orbit")]
    public float orbitSpeed = 25f;
    public float wobbleAmplitude = 0.3f;
    public float wobbleSpeed = 1.5f;
    public RectTransform parchmentRect;
    public float edgePadding = 0.85f;
    public float maxRadiusOverride = 0f;
    public float minRadius = 0.8f;

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
    
    private float maxRadius = 4f;

    public static ThoughtOrbit Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

            (float angle, float radius) = FindNonOverlappingPosition();
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
        for (int i = 0; i < orbitElements.Count; i++)
        {
            OrbitElement oe = orbitElements[i];
            if (oe == null) continue;

            oe.currentAngle += orbitSpeed * Mathf.Deg2Rad * Time.deltaTime;

            float phase = time * wobbleSpeed + oe.currentAngle * 0.5f;
            float wobble = Mathf.Sin(phase) * wobbleAmplitude;
            float currentRadius = oe.targetRadius + wobble;

            Vector3 pos = transform.position + new Vector3(
                Mathf.Cos(oe.currentAngle) * currentRadius,
                Mathf.Sin(oe.currentAngle) * currentRadius,
                0
            );
            oe.transform.position = pos;
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
        (float angle, float radius) = FindNonOverlappingPosition();
        
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
    (float angle, float radius) FindNonOverlappingPosition()
    {
        // 如果还没有元素，放在最外圈随机位置
        if (orbitElements.Count == 0)
        {
            return (Random.Range(0f, 2f * Mathf.PI), maxRadius);
        }

        // 需要的最小距离（根据图标大小估算）
        float minDistance = orbitElementScale * 0.8f;  // 假设图标直径约1单位，scale 2.5时约2.5单位直径
        
        // 多轮尝试
        for (int attempt = 0; attempt < 100; attempt++)
        {
            // 半径：从 maxRadius 到 minRadius 之间随机
            float r = Random.Range(minRadius, maxRadius);
            
            // 角度：完全随机，但是如果尝试次数较多，尝试固定间隔
            float a;
            if (attempt < 20)
            {
                a = Random.Range(0f, 2f * Mathf.PI);
            }
            else
            {
                // 固定角度间隔，均匀分布在圆上
                float angleStep = 2f * Mathf.PI / (orbitElements.Count + 1);
                int slotIndex = attempt - 20;
                a = (slotIndex % (orbitElements.Count + 1)) * angleStep;
            }
            
            // 检查这个位置是否远离已有元素
            bool tooClose = false;
            foreach (var oe in orbitElements)
            {
                if (oe == null) continue;
                Vector3 otherPos = transform.position + new Vector3(
                    Mathf.Cos(oe.currentAngle) * oe.targetRadius,
                    Mathf.Sin(oe.currentAngle) * oe.targetRadius,
                    0
                );
                Vector3 testPos = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * r;
                float dist = Vector3.Distance(otherPos, testPos);
                if (dist < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (!tooClose)
            {
                return (a, r);
            }
        }
        
        // 实在找不到位置，放在随机位置
        return (Random.Range(0f, 2f * Mathf.PI), Random.Range(minRadius, maxRadius));
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
        float minDistance = orbitElementScale * 0.8f;
        foreach (var oe in orbitElements)
        {
            if (oe == null) continue;
            Vector3 pos = oe.transform.position;
            DrawCircle(pos, minDistance / 2f, Color.red, 0.2f);
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