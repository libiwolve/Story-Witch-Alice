using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class QuillController : MonoBehaviour, IPointerClickHandler
{
    [Header("引用")]
    public AlchemyManager alchemyManager;

    [Header("浮动设置")]
    public float floatAmplitude = 0.3f;
    public float floatSpeed = 1.5f;

    [Header("搅拌设置")]
    public float stirAmplitude = 1.2f;
    public float stirSpeed = 3f;
    public int stirCycles = 3;

    private bool isStirring = false;
    private Vector3 originalPosition;
    private float currentYOffset = 0f;   // 当前的 Y 偏移，搅拌和浮动共享

    void Start()
    {
        originalPosition = transform.position;
    }

    void Update()
    {
        if (isStirring) return;

        // 从 currentYOffset 继续浮动，不跳回 originalPosition
        currentYOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = originalPosition + new Vector3(0, currentYOffset, 0);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isStirring) return;

        if (eventData.button == PointerEventData.InputButton.Left)
            StartCoroutine(StirThenCombine());
        else if (eventData.button == PointerEventData.InputButton.Right)
           alchemyManager?.ClearPot();
    }

    IEnumerator StirThenCombine()
    {
        yield return StartCoroutine(StirMotion());
        alchemyManager?.ManualCombine();
    }

    IEnumerator StirThenClear()
    {
        yield return StartCoroutine(StirMotion());
        alchemyManager?.ClearPot();
    }

    IEnumerator StirMotion()
    {
        isStirring = true;

        float totalAngle = stirCycles * 2f * Mathf.PI;
        float currentAngle = 0f;
        float centerX = originalPosition.x + stirAmplitude;

        while (currentAngle < totalAngle)
        {
            currentAngle += stirSpeed * Time.deltaTime;
            if (currentAngle > totalAngle)
                currentAngle = totalAngle;

            // X 轴搅拌
            float x = centerX - stirAmplitude * Mathf.Cos(currentAngle);

            // Y 轴继续浮动，保持和 Update 里一致
            float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

            transform.position = new Vector3(x, originalPosition.y + yOffset, originalPosition.z);
            yield return null;
        }

        // 搅拌结束：X 回到最左端，Y 保持当前偏移
        currentYOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(originalPosition.x, originalPosition.y + currentYOffset, originalPosition.z);

        isStirring = false;
    }
}