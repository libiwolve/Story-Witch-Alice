using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 轨道上的单个元素。
/// 由 ThoughtOrbit 管理轨道参数（槽位系统）。
/// 点击拖拽 → 提到队首 + 生成物理元素跟随鼠标。
/// </summary>
public class OrbitElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    private Camera baseCamera;

    private static Texture2D _fallbackTex;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        baseCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        if (baseCamera == null)
        {
            Debug.LogError("[OrbitElement] 未找到 Tag 为 'MainCamera' 的相机！");
        }
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

    public void InitializeSlot(int index, float angle, float radius)
    {
        slotIndex = index;
        slotAngle = angle;
        slotRadius = radius;
        currentAngle = angle;
        targetRadius = radius;
    }

    public void AssignToSlot(int index, float angle, float radius)
    {
        slotIndex = index;
        slotAngle = angle;
        slotRadius = radius;
        targetRadius = radius;

        float angleDiff = angle - currentAngle;
        while (angleDiff > Mathf.PI) angleDiff -= 2f * Mathf.PI;
        while (angleDiff < -Mathf.PI) angleDiff += 2f * Mathf.PI;

        if (Mathf.Abs(angleDiff) > Mathf.PI * 0.5f)
        {
            currentAngle = angle;
        }
    }

    public void ReleaseSlot()
    {
        slotIndex = -1;
    }

    public int GetSlotIndex()
    {
        return slotIndex;
    }

    // ========== 静态方法 ==========

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

    // ========== EventSystem 拖拽接口 ==========

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (orbitManager == null || elementData == null) return;

        if (dragInstance != null)
        {
            Destroy(dragInstance);
            dragInstance = null;
        }

        orbitManager.MoveToFront(elementData.elementID);
        
        dragInstance = PhysicsElementSpawner.SpawnForDrag(elementData);
        if (dragInstance == null) return;

        // 标记被 Orbit 托管
        var pe = dragInstance.GetComponent<PhysicsElement>();
        if (pe != null) pe.isDraggedByOrbit = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragInstance == null) return;
        dragInstance.transform.position = CameraUtility.MouseToWorld();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragInstance == null) return;

        var pe = dragInstance.GetComponent<PhysicsElement>();
        if (pe != null) pe.isDraggedByOrbit = false;

        PhysicsElementSpawner.Release(dragInstance);
        dragInstance = null;
    }

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

    // ========== 工具方法 ==========

   
}