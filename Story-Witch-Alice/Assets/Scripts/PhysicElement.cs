using UnityEngine;
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

    // 记录最近几帧的位置和时间
    private Queue<(Vector3 pos, float time)> recentPositions = new Queue<(Vector3, float)>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
    }

    // 掉进锅里
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

    // 碰到地面时飞回
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (sourceSlot != null)
                sourceSlot.OnElementMissedPot(gameObject);
        }
    }

    // 鼠标按下 → 临时关闭重力，抓起物体
    void OnMouseDown()
    {
        isBeingDragged = true;

        // 清空惯性记录
        recentPositions.Clear();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    // 鼠标拖拽中 → 跟随鼠标
    void OnMouseDrag()
    {
        if (!isBeingDragged) return;

        Vector3 mouseWorld = GetMouseWorldPosition();
        transform.position = mouseWorld;

        // 记录当前位置和时间，用于计算惯性
        recentPositions.Enqueue((mouseWorld, Time.time));
        if (recentPositions.Count > velocitySampleFrames)
            recentPositions.Dequeue();
    }

    // 鼠标松开 → 恢复重力，丢出物体
    void OnMouseUp()
    {
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

        // 限速
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