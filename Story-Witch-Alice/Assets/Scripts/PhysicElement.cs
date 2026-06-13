using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhysicsElement : MonoBehaviour
{
    public ElementData elementData;
    public UIDraggableElement sourceSlot;

    [Header("inertia settings")]
    public int velocitySampleFrames = 5;

    private bool isBeingDragged = false;
    private Rigidbody2D rb;
    private Camera mainCamera;

    private Queue<(Vector3 pos, float time)> recentPositions = new Queue<(Vector3, float)>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        // 有 icon → 覆盖 prefab 默认 sprite；无 icon → 保留 prefab 外观
        if (elementData != null && elementData.elementIcon != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = elementData.elementIcon;
        }
    }

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
                // 物品栏拖出的元素落地 → 恢复槽位图标，元素留在场景
                sourceSlot.RestoreIcon();
                sourceSlot = null; // 切断关联，变成独立可拾取元素
            }
            else if (elementData != null)
            {
                // 合成产物落地 → 元素留在场景（可拖回锅继续合成）
                AlchemyManager.Instance?.AddLog($"{elementData.elementName} 掉在了地上");
            }
        }
    }

    void OnMouseDown()
    {
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

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mouseScreen);
    }
}
