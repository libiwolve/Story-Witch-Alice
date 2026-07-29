using UnityEngine;
using UnityEngine.EventSystems;

public class CartographerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Animator animator;
    public string hoverTrigger = "Pointer";   // 悬停时触发
    public string closeTrigger = "Close";     // 移开时触发

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (animator != null)
            animator.SetTrigger(hoverTrigger);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (animator != null)
            animator.SetTrigger(closeTrigger);
    }
}