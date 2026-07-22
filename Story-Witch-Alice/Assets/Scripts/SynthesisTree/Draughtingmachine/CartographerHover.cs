using UnityEngine;
using UnityEngine.EventSystems;

public class CartographerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Animator animator;
    public string triggerName = "Pointer";

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (animator != null)
            animator.SetTrigger(triggerName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 不需要在退出时做什么，Animator 里用 Idle 状态自动过渡即可
    }
}