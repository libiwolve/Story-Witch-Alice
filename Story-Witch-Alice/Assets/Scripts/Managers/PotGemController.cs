using UnityEngine;

public class PotGemController : MonoBehaviour
{
    private Animator animator;
    private int currentGemCount = 0;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void OnIngredientAdded(int totalCount)
    {
        if (animator == null) return;
        Debug.Log($"OnIngredientAdded 被调用, totalCount={totalCount}, animator是否为空={animator == null}");
        animator.SetInteger("GemCount", totalCount);     // 1, 2, 3 → 对应动画
                    // 无操作
    }

    public void OnPotCleared()
    {
       if (animator == null) return;
            animator.SetInteger("GemCount", 0); 
    }

    void UpdateGemAnimation()
    {
        if (animator != null)
            animator.SetInteger("GemCount", currentGemCount);
    }
}