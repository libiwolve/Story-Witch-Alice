using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PhysicsElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("遗弃标记")]
    public GameObject markIcon;              // 垃圾桶图标预制体（在 Inspector 里拖入）
    private GameObject currentMarkIcon;      // 当前生成的图标实例
    private bool isMarkedForRemoval = false;  // 是否被标记为移除
    public ElementData elementData;
    public UIDraggableElement sourceSlot;

    [Header("Inertia Settings")]
    public int velocitySampleFrames = 5;

    [Header("Particle")]
    public ParticleSystem dirtTrail;

    // ===== 外部控制标志 =====
    [HideInInspector] public bool isControlledByNodeDrag = false;
    [HideInInspector] public bool isDraggedByOrbit = false;

    private bool isBeingDragged = false;
    private Rigidbody2D rb;
    private Camera mainCamera;

    private Queue<(Vector3 pos, float time)> recentPositions = new Queue<(Vector3, float)>();
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        if (dirtTrail != null) dirtTrail.Play();

        if (elementData != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (elementData.elementIcon != null)
                    sr.sprite = elementData.elementIcon;
                else
                {
                    Sprite fallback = ResolveFallbackSprite(elementData.elementID);
                    if (fallback != null) sr.sprite = fallback;
                }
            }

            RuntimeAnimatorController ctrl = GetElementAnimController(elementData.elementID);
            Animator anim = GetComponent<Animator>();
            if (ctrl != null)
            {
                if (anim == null) anim = gameObject.AddComponent<Animator>();
                anim.runtimeAnimatorController = ctrl;
            }
            else if (anim != null)
            {
                Destroy(anim);
            }
        }
        if (markIcon == null)
        {
            markIcon = Resources.Load<GameObject>("Icons/MarkIcon");  // 如果放在 Resources/Icons/ 下
            // 如果直接放在 Resources/ 下，改成：
            // markIcon = Resources.Load<GameObject>("MarkIcon");j
        }
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                ToggleMark();
            }
        }
    }

    // ========== EventSystem 拖拽接口 ==========

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isControlledByNodeDrag || isDraggedByOrbit) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        isBeingDragged = true;
        recentPositions.Clear();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isControlledByNodeDrag || isDraggedByOrbit) return;
        if (!isBeingDragged) return;

        transform.position = CameraUtility.MouseToWorld();

        recentPositions.Enqueue((transform.position, Time.time));
        if (recentPositions.Count > velocitySampleFrames)
            recentPositions.Dequeue();
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        // 如果被外部系统托管，忽略
        if (isControlledByNodeDrag || isDraggedByOrbit) return;
        if (!isBeingDragged) return;

        isBeingDragged = false;

        // 计算惯性速度
        Vector3 velocity = Vector3.zero;
        if (recentPositions.Count >= 2)
        {
            var oldest = recentPositions.Peek();
            var newest = transform.position;
            float timeSpan = Time.time - oldest.time;
            if (timeSpan > 0.001f)
                velocity = (newest - oldest.pos) / timeSpan;
            else
                velocity = new Vector2(0, -1f);
        }
        else
        {
            velocity = new Vector2(0, -1f);
        }

        float maxSpeed = 30f;
        if (velocity.magnitude > maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.velocity = velocity;
        }
    }

    // ========== 物理交互 ==========

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pot"))
        {
            AlchemyManager.Instance?.AddIngredient(elementData);
            if (sourceSlot != null)
                sourceSlot.RestoreIcon();
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (sourceSlot != null)
            {
                sourceSlot.RestoreIcon();
                sourceSlot = null;
            }
            else if (elementData != null)
            {
                AlchemyManager.Instance?.AddLog($"{elementData.elementName} 掉在了地上");
            }
        }
    }

    // ========== 工具方法 ==========

    

    private static Sprite ResolveFallbackSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
#if UNITY_EDITOR
        string path = $"Assets/Prefabs/Physic{id}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            SpriteRenderer psr = prefab.GetComponent<SpriteRenderer>();
            if (psr != null && psr.sprite != null)
                return psr.sprite;
        }
#endif
        return GetFallbackMosaic();
    }

    private static Sprite GetFallbackMosaic()
    {
        const int size = 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color f = new Color(0.93f, 0.20f, 0.78f);
        for (int i = 0; i < size * size; i++)
            tex.SetPixel(i % size, i / size, f);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static RuntimeAnimatorController GetElementAnimController(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
    #if UNITY_EDITOR
            string path = $"Assets/Animations/PhysicElement Animation/Physic{id}.controller";
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
    #else
            return null;
    #endif
    }
    void ToggleMark()
    {
        isMarkedForRemoval = !isMarkedForRemoval;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (isMarkedForRemoval)
        {
            // 把元素颜色变灰
            if (sr != null)
                sr.color = new Color(0.5f, 0.5f, 0.5f, 1f);  // 灰色

            // 生成垃圾桶图标（挂在元素上方）
            if (markIcon != null && currentMarkIcon == null)
            {
                currentMarkIcon = Instantiate(markIcon, transform.position + Vector3.up * 0.5f, Quaternion.identity, transform);
                currentMarkIcon.transform.localScale = Vector3.one * 0.8f;
            }

            ElementDisposalManager.Instance?.MarkElement(this);
        }
        else
        {
            // 恢复元素原色
            if (sr != null)
                sr.color = Color.white;  // 恢复原色

            // 移除图标
            if (currentMarkIcon != null)
            {
                Destroy(currentMarkIcon);
                currentMarkIcon = null;
            }

            ElementDisposalManager.Instance?.UnmarkElement(this);
        }
    }

    /// <summary>
    /// 清除标记（被管理器调用）
    /// </summary>
    public void ClearMark()
    {
        if (currentMarkIcon != null)
        {
            Destroy(currentMarkIcon);
            currentMarkIcon = null;
        }
        isMarkedForRemoval = false;
    }

    /// <summary>
    /// 强制销毁元素（被管理器调用）
    /// </summary>
    public void ForceDestroy()
    {
        ClearMark();
        Destroy(gameObject);
    }
}