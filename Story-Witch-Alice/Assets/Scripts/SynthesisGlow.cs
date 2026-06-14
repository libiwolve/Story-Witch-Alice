using UnityEngine;

/// <summary>
/// 合成产物的光晕效果。
/// 用 Animator 播放序列帧动画（金色/蓝色），动画播完自动销毁。
/// 用法：SynthesisGlow.AttachTo(product, goldAnimController);
/// </summary>
public class SynthesisGlow : MonoBehaviour
{
    /// <summary>
    /// 在目标下创建一个子对象，挂载 SpriteRenderer + Animator 播放光晕动画。
    /// 到达悬停位置后播放一次，播完自动销毁。
    /// </summary>
    public static void AttachTo(GameObject target, RuntimeAnimatorController animController)
    {
        if (animController == null) return;

        GameObject child = new GameObject("SynthesisGlow");
        child.transform.SetParent(target.transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localScale = Vector3.one * 0.6f;

        SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1; // 高于元素本身，不会被场景遮挡

        Animator anim = child.AddComponent<Animator>();
        anim.runtimeAnimatorController = animController;

        child.AddComponent<SynthesisGlow>();
    }

}
