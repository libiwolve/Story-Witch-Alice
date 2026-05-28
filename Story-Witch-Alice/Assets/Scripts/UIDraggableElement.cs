using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIDraggableElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Data")]
    public ElementData elementData;
    public GameObject physicsPrefab;

    [Header("Settings")]
    public float returnDuration = 0.3f;
    public float initialFallVelocity = -5f;

    [Header("Inertia settings")]
    public int velocitySampleFrames = 5;

    private Image slotImage;
    private Color originalColor;
    private GameObject spawnedElement;
    private Camera mainCamera;

    // 惯性记录
    private Queue<(Vector3 pos, float time)> recentPositions = new Queue<(Vector3, float)>();

    private Vector3 currentMouseWorld; // 当前帧的鼠标世界坐标

    void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage == null)
        {
            Debug.LogWarning($"{name} 缺少 Image 组件，无法调节透明度");
            originalColor = Color.white;
        }
        else
        {
            originalColor = slotImage.color;
        }

        mainCamera = Camera.main;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (physicsPrefab == null) return;
        if (spawnedElement != null) return;

        recentPositions.Clear();

        currentMouseWorld = ScreenToWorld(eventData.position);
        spawnedElement = Instantiate(physicsPrefab, currentMouseWorld, Quaternion.identity);

        PhysicsElement pe = spawnedElement.GetComponent<PhysicsElement>();
        if (pe != null)
        {
            pe.elementData = elementData;
            pe.sourceSlot = this;
        }

        Rigidbody2D rb = spawnedElement.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        if (slotImage != null)
            slotImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (spawnedElement == null) return;

        currentMouseWorld = ScreenToWorld(eventData.position);
        spawnedElement.transform.position = currentMouseWorld;

        recentPositions.Enqueue((currentMouseWorld, Time.time));
        if (recentPositions.Count > velocitySampleFrames)
            recentPositions.Dequeue();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (spawnedElement == null) return;

        // 计算惯性速度
        Vector3 velocity = Vector3.zero;
        if (recentPositions.Count >= 2)
        {
            var oldest = recentPositions.Peek();
            float timeSpan = Time.time - oldest.time;
            if (timeSpan > 0.001f)
                velocity = (currentMouseWorld - oldest.pos) / timeSpan;
            else
                velocity = new Vector2(0, initialFallVelocity);
        }
        else
        {
            velocity = new Vector2(0, initialFallVelocity);
        }
        

        Rigidbody2D rb = spawnedElement.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 1f;
            float maxSpeed = 30f;
            if (velocity.magnitude > maxSpeed)
            {
                velocity = velocity.normalized * maxSpeed;
            }
            rb.velocity = velocity;
        }

        // 清空本地引用，由 PhysicsElement 管理后续
        spawnedElement = null;
        // 图标恢复由 PhysicsElement 触发
    }

    public void RestoreIcon()
    {
        if (slotImage != null)
            slotImage.color = originalColor;
    }

    public void OnElementMissedPot(GameObject element)
    {
        if (element == null) return;
        StartCoroutine(FlyBack(element));
    }

    private IEnumerator FlyBack(GameObject element)
    {
        Rigidbody2D rb = element.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        Collider2D col = element.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Vector3 startPos = element.transform.position;
        Vector3 targetPos = transform.position; // 物品栏槽位的世界位置

        float elapsed = 0f;
        while (elapsed < returnDuration)
        {
            if (element == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            t = 1f - Mathf.Pow(1f - t, 3f);
            element.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        Destroy(element);
        RestoreIcon();
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 screen = screenPos;
        screen.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(screen);
    }
}