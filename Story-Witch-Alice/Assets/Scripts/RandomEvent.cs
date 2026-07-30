using UnityEngine;
using System.Collections;

public class RandomEvent : MonoBehaviour
{
    [Header("随机设置")]
    public float minInterval = 30f;
    public float maxInterval = 120f;
    public float eventDuration = 4f;

    [Header("Animator")]
    public Animator animator;
    public string triggerName = "Appear";

    [Header("可见性控制")]
    public SpriteRenderer spriteRenderer;

    void Start()
    {
        gameObject.SetActive(true);
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // 初始可见，透明度正常
        SetVisible(false);
        StartCoroutine(RandomLoop());
    }

    IEnumerator RandomLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            // 出现
            SetVisible(true);
            if (animator != null)
                animator.SetTrigger(triggerName);

            yield return new WaitForSeconds(eventDuration);

            // 隐藏
            SetVisible(false);
        }
    }

    /// <summary>
    /// 由 Animation Event 在 Rat2 最后一帧调用，提前隐藏
    /// </summary>
    public void HideSelf()
    {
        SetVisible(false);
    }

    void SetVisible(bool visible)
    {
        if (spriteRenderer == null) return;
        Color c = spriteRenderer.color;
        c.a = visible ? 1f : 0f;
        spriteRenderer.color = c;
    }
}