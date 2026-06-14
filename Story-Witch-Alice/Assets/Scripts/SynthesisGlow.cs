using UnityEngine;

/// <summary>
/// 合成产物的光晕效果。
/// 用 Animator 播放序列帧动画（金色/蓝色），播放完自动销毁。
/// 用法：SynthesisGlow.AttachTo(product, goldAnimController);
/// </summary>
public class SynthesisGlow : MonoBehaviour
{
    /// <summary>
    /// 在目标下创建一个子对象，挂载 SpriteRenderer + Animator 播放光晕动画。
    /// 动画播放完后自动销毁子对象。
    /// </summary>
    public static void AttachTo(GameObject target, RuntimeAnimatorController animController)
    {
        if (animController == null) return;

        GameObject child = new GameObject("SynthesisGlow");
        child.transform.SetParent(target.transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localScale = Vector3.one * 2f; // 比元素本身大两圈

        SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -1; // 渲染在元素后面

        Animator anim = child.AddComponent<Animator>();
        anim.runtimeAnimatorController = animController;

        // 获取动画长度 → 结束后自动销毁
        child.AddComponent<SynthesisGlow>().Init(sr);
    }

    private SpriteRenderer glowSr;

    void Init(SpriteRenderer sr)
    {
        glowSr = sr;
        StartCoroutine(AutoDestroyAfterAnimation());
    }

    System.Collections.IEnumerator AutoDestroyAfterAnimation()
    {
        // 等一帧让 Animator 开始播放
        yield return null;

        Animator anim = GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            float duration = state.length;
            yield return new WaitForSeconds(duration);
        }
        else
        {
            // fallback：等 2 秒后销毁
            yield return new WaitForSeconds(2f);
        }

        Destroy(gameObject);
    }
}
