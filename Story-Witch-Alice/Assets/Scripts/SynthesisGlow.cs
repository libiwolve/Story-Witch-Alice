using UnityEngine;

/// <summary>
/// 合成产物的光效：脉冲闪烁后自动消失。
/// 由 AlchemyManager 在产物飞出到悬停位置时创建。
/// 用法：SynthesisGlow.AttachTo(product, color, iconSprite);
/// </summary>
public class SynthesisGlow : MonoBehaviour
{
    public float lifetime = 2.5f;
    public float pulseSpeed = 4f;

    private SpriteRenderer glowRenderer;
    private float elapsed;

    /// <summary>
    /// 在目标上挂载一个光晕子对象，返回 Glow 组件引用。
    /// </summary>
    public static SynthesisGlow AttachTo(GameObject target, Color color, Sprite icon)
    {
        GameObject child = new GameObject("SynthesisGlow");
        child.transform.SetParent(target.transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localScale = Vector3.one * 1.6f; // 比元素本身大一圈

        SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = icon;
        sr.sortingOrder = -1; // 渲染在元素后面
        sr.color = color;

        SynthesisGlow glow = child.AddComponent<SynthesisGlow>();
        glow.glowRenderer = sr;
        return glow;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // 脉冲：正弦波在 minAlpha ~ maxAlpha 之间
        float pulse = Mathf.Sin(elapsed * pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
        float alpha = Mathf.Lerp(0.15f, 0.7f, pulse);

        // 最后 30% 时间逐渐淡出
        float t = elapsed / lifetime;
        if (t > 0.7f)
            alpha *= 1f - (t - 0.7f) / 0.3f;

        Color c = glowRenderer.color;
        c.a = alpha;
        glowRenderer.color = c;
    }
}
