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

    private bool isBeingDragged = false;
    public ParticleSystem dirtTrail;

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

            // 删除 UpdateScaleAndCollider()
        }
    }

    // ★ 删除了整个 LateUpdate() ★

    // ==================== 保留辅助方法 ====================

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

    // ==================== 交互逻辑（完全保留） ====================

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
}