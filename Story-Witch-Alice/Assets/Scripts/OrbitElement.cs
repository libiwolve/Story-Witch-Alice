using UnityEngine;

/// <summary>
/// 轨道上的单个元素。
/// 由 ThoughtOrbit 管理轨道参数（槽位系统），物理引擎处理碰撞弹开。
/// 鼠标点击 → 提到队首 + 拖出物理元素跟随鼠标。
/// </summary>
public class OrbitElement : MonoBehaviour
{
    [HideInInspector] public ElementData elementData;
    [HideInInspector] public ThoughtOrbit orbitManager;

    [Header("Orbit (set by ThoughtOrbit)")]
    public float targetRadius = 3f;
    public float currentAngle;

    // 槽位系统
    private int slotIndex = -1;
    private float slotAngle;
    private float slotRadius;

    private SpriteRenderer sr;
    private Collider2D col;

    // 点击拖出的实例
    private GameObject dragInstance;
    private Camera mainCamera;

    private static Texture2D _fallbackTex;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        mainCamera = Camera.main;
    }

    public void SetAlpha(float alpha)
    {
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    // ========== 槽位系统方法 ==========

    /// <summary>
    /// 初始化槽位（创建时调用）
    /// </summary>
    public void InitializeSlot(int index, float angle, float radius)
    {
        slotIndex = index;
        slotAngle = angle;
        slotRadius = radius;
        currentAngle = angle;
        targetRadius = radius;
    }

    /// <summary>
    /// 分配到新槽位（队列重排时调用）
    /// </summary>
    public void AssignToSlot(int index, float angle, float radius)
    {
        slotIndex = index;
        slotAngle = angle;
        slotRadius = radius;
        targetRadius = radius;
        
        // 平滑过渡：计算最短角度路径
        float angleDiff = angle - currentAngle;
        // 标准化到 [-PI, PI]
        while (angleDiff > Mathf.PI) angleDiff -= 2f * Mathf.PI;
        while (angleDiff < -Mathf.PI) angleDiff += 2f * Mathf.PI;
        
        // 如果角度差异太大，直接设置（避免旋转一整圈）
        if (Mathf.Abs(angleDiff) > Mathf.PI * 0.5f)
        {
            currentAngle = angle;
        }
        // 否则让Update中的自然旋转处理过渡
    }

    /// <summary>
    /// 释放槽位（元素被移除时调用）
    /// </summary>
    public void ReleaseSlot()
    {
        slotIndex = -1;
    }

    public int GetSlotIndex()
    {
        return slotIndex;
    }

    // ========== 静态方法 ==========

    /// <summary>
    /// 由 ThoughtOrbit 外部调用生成粉紫色马赛克（静态，不依赖实例 sr）
    /// </summary>
    public static Sprite GetFallbackSpriteStatic()
    {
        return CreateFallbackSprite();
    }

    static Sprite CreateFallbackSprite()
    {
        if (_fallbackTex == null)
        {
            _fallbackTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            _fallbackTex.filterMode = FilterMode.Point;
            Color f = new Color(0.93f, 0.20f, 0.78f);
            for (int i = 0; i < 4; i++)
                _fallbackTex.SetPixel(i % 2, i / 2, f);
            _fallbackTex.Apply();
        }
        return Sprite.Create(_fallbackTex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2);
    }

    // ========== 点击拖拽 ==========

    void OnMouseDown()
    {
        if (orbitManager == null || elementData == null) return;

        // 提到队首
        orbitManager.MoveToFront(elementData.elementID);

        // 生成物理元素到鼠标位置
        Vector3 mouseWorld = GetMouseWorldPosition();
        GameObject prefab = AlchemyManager.Instance?.GetPrefabForElement(elementData);
        if (prefab == null) return;

        dragInstance = Instantiate(prefab, mouseWorld, Quaternion.identity);

        PhysicsElement pe = dragInstance.GetComponent<PhysicsElement>();
        if (pe != null)
        {
            pe.elementData = elementData;
            pe.sourceSlot = null;
        }

        // 禁用物理，跟随鼠标
        Rigidbody2D rb = dragInstance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        Collider2D[] cols = dragInstance.GetComponents<Collider2D>();
        foreach (var c in cols) c.enabled = true;
    }

    void OnMouseDrag()
    {
        if (dragInstance == null) return;
        dragInstance.transform.position = GetMouseWorldPosition();
    }

    void OnMouseUp()
    {
        if (dragInstance == null) return;

        // 松手 → 开启物理，让元素下落
        Rigidbody2D rb = dragInstance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 1f;
        }

        dragInstance = null;
    }

    // ========== 悬停高亮 ==========

    void OnMouseOver()
    {
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Min(c.a + 0.1f, 1f);
            sr.color = c;
        }
    }

    void OnMouseExit()
    {
        if (orbitManager != null && elementData != null)
            orbitManager.UpdateElementAlpha(this);
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 screen = Input.mousePosition;
        screen.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(screen);
    }
}