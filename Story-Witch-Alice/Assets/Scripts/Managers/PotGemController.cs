using UnityEngine;

public class PotGemController : MonoBehaviour
{
    private static readonly string[] GemStateNames =
    {
        "Idle",
        "OneGem",
        "TwoGems",
        "ThreeGems"
    };

    private Animator animator;
    private int currentGemCount = 0;

    void Awake()
    {
        animator = GetComponent<Animator>();
        SetIngredientCount(0);
    }

    public void OnIngredientAdded(int totalCount)
    {
        SetIngredientCount(totalCount);
    }

    public void OnPotCleared()
    {
        SetIngredientCount(0);
    }

    public void SetIngredientCount(int totalCount)
    {
        currentGemCount = Mathf.Clamp(totalCount, 0, 3);
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null) return;

        animator.SetInteger("GemCount", currentGemCount);

        // 直接进入与锅内数量对应的状态，避免连续快速投料时 Animator
        // 还停留在上一个过渡状态，造成亮起宝石数量与原料数不一致。
        animator.Play(GemStateNames[currentGemCount], 0, 0f);
        animator.Update(0f);
    }
}
