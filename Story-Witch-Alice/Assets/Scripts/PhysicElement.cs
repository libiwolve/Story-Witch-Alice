using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PhysicsElement : MonoBehaviour
{
    public ElementData elementData;
    public UIDraggableElement sourceSlot;

    [Header("Inertia Settings")]
    public int velocitySampleFrames = 5;

    [Header("Size Settings")]
    [Tooltip("所有元素统一的世界空间大小（Unity单位）")]
    public float targetWorldSize = 1.5f;
    
    [Tooltip("允许的最大缩放倍数，防止某些贴图尺寸异常导致物体巨大")]
    public float maxScale = 3f;
    
    [Tooltip("允许的最小缩放倍数")]
    public float minScale = 0.3f;

    private bool isBeingDragged = false;
    public ParticleSystem dirtTrail;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Sprite lastRebuiltSprite;
    private float currentScale = 1f;

    private Queue<(Vector3 pos, float time)> recentPositions = new Queue<(Vector3, float)>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        if (dirtTrail != null) dirtTrail.Play();

        if (elementData != null)
        {
            // 第一步：设置正确的 Sprite
            if (spriteRenderer != null)
            {
                if (elementData.elementIcon != null)
                    spriteRenderer.sprite = elementData.elementIcon;
                else
                {
                    Sprite fallback = ResolveFallbackSprite(elementData.elementID);
                    if (fallback != null) spriteRenderer.sprite = fallback;
                }
            }

            // 第二步：处理动画
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

            // 第三步：统一缩放并重建碰撞体
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                UpdateScaleAndCollider();
                lastRebuiltSprite = spriteRenderer.sprite;
            }
        }
        else
        {
            // elementData 为空，保持原样
            Debug.LogWarning($"[PhysicsElement] {gameObject.name} 的 elementData 为空，跳过缩放处理");
        }
    }

    void LateUpdate()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        if (spriteRenderer.sprite != lastRebuiltSprite)
        {
            UpdateScaleAndCollider();
            lastRebuiltSprite = spriteRenderer.sprite;
        }
    }

    /// <summary>
    /// 根据当前 Sprite 的实际尺寸，计算缩放倍数并重建碰撞体
    /// </summary>
    private void UpdateScaleAndCollider()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        Sprite sp = spriteRenderer.sprite;
        float spriteWidth = sp.bounds.size.x;
        float spriteHeight = sp.bounds.size.y;

        // 取 Sprite 的最大边
        float maxSpriteSize = Mathf.Max(spriteWidth, spriteHeight);
        
        // 如果 Sprite 尺寸异常小或为 0，跳过
        if (maxSpriteSize <= 0.0001f)
        {
            Debug.LogError($"[PhysicsElement] {elementData?.elementName ?? gameObject.name} " +
                          $"的 Sprite 尺寸异常: {spriteWidth}x{spriteHeight}，使用默认缩放 1");
            currentScale = 1f;
            transform.localScale = Vector3.one;
            RebuildCollider2D(spriteRenderer);
            return;
        }

        // 计算缩放比例
        float rawScale = targetWorldSize / maxSpriteSize;

        // ★ 限制缩放范围，防止异常 ★
        currentScale = Mathf.Clamp(rawScale, minScale, maxScale);

        transform.localScale = Vector3.one * currentScale;

        // 重建碰撞体
        RebuildCollider2D(spriteRenderer);

        Debug.Log($"[{elementData?.elementName ?? gameObject.name}] " +
                  $"Sprite原始尺寸={spriteWidth:F3}x{spriteHeight:F3}, " +
                  $"原始缩放计算={rawScale:F2}, " +
                  $"限制后缩放={currentScale:F2}, " +
                  $"最终世界尺寸≈{maxSpriteSize * currentScale:F2}");
    }

    /// <summary>
    /// 根据当前 Sprite 重建 2D 碰撞体
    /// </summary>
    private void RebuildCollider2D(SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return;

        PolygonCollider2D poly = GetComponent<PolygonCollider2D>();
        if (poly != null)
        {
            int shapeCount = sr.sprite.GetPhysicsShapeCount();

            if (shapeCount > 0)
            {
                poly.pathCount = shapeCount;
                List<Vector2> shape = new List<Vector2>();
                for (int i = 0; i < shapeCount; i++)
                {
                    shape.Clear();
                    sr.sprite.GetPhysicsShape(i, shape);
                    poly.SetPath(i, shape.ToArray());
                }
            }
            else
            {
                poly.pathCount = 1;
                Bounds b = sr.sprite.bounds;
                Vector2[] rectShape = new Vector2[4]
                {
                    new Vector2(b.min.x, b.min.y),
                    new Vector2(b.min.x, b.max.y),
                    new Vector2(b.max.x, b.max.y),
                    new Vector2(b.max.x, b.min.y)
                };
                poly.SetPath(0, rectShape);
            }
        }

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Bounds b = sr.sprite.bounds;
            box.size = b.size;
            box.offset = b.center;
        }

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Bounds b = sr.sprite.bounds;
            float radius = Mathf.Max(b.extents.x, b.extents.y);
            circle.radius = radius;
            circle.offset = b.center;
        }
    }

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

    // ==================== 交互逻辑 ====================

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pot"))
        {
            DestroySynthesisGlow();
            AlchemyManager.Instance?.AddIngredient(elementData);
            if (sourceSlot != null)
                sourceSlot.RestoreIcon();
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        DestroySynthesisGlow();

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

    void OnMouseDown()
    {
        DestroySynthesisGlow();

        isBeingDragged = true;
        recentPositions.Clear();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void OnMouseDrag()
    {
        if (!isBeingDragged) return;

        Vector3 mouseWorld = GetMouseWorldPosition();
        transform.position = mouseWorld;

        recentPositions.Enqueue((mouseWorld, Time.time));
        if (recentPositions.Count > velocitySampleFrames)
            recentPositions.Dequeue();
    }

    void OnMouseUp()
    {
        isBeingDragged = false;

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

    private void DestroySynthesisGlow()
    {
        SynthesisGlow glow = GetComponentInChildren<SynthesisGlow>();
        if (glow != null) Destroy(glow.gameObject);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mouseScreen);
    }
<<<<<<< HEAD
    
}
=======

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        Gizmos.color = Color.green;
        Bounds b = spriteRenderer.sprite.bounds;
        Vector3 center = transform.position + (Vector3)b.center * currentScale;
        Vector3 size = b.size * currentScale;
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
>>>>>>> 2e25513d40661c649757a07a214758994a4bd183
