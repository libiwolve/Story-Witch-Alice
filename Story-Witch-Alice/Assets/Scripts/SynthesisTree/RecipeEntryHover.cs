using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RecipeEntryHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Animator checkmarkAnimator;
    public GameObject checkmarkObject;

    void Start()
    {
        if (checkmarkObject != null)
            checkmarkObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (checkmarkObject != null)
            checkmarkObject.SetActive(true);
        if (checkmarkAnimator != null)
            checkmarkAnimator.SetTrigger("Show");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 鼠标移走时隐藏打勾
        if (checkmarkObject != null)
            checkmarkObject.SetActive(false);
    }
}