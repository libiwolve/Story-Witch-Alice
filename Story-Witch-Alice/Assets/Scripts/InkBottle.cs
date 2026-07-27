using UnityEngine;
using UnityEngine.EventSystems;

public class InkBottle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Animator bottleAnimator;
    public ElementDisposalManager disposalManager;
    
    private bool isOpen = false;

    void Start()
    {
        if (disposalManager == null)
            disposalManager = ElementDisposalManager.Instance;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isOpen && bottleAnimator != null)
        {
            bottleAnimator.SetTrigger("Hover");
            isOpen = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isOpen && disposalManager != null && !disposalManager.IsDisposing())
        {
            bottleAnimator.SetTrigger("Close");
            isOpen = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (disposalManager != null && !disposalManager.IsDisposing())
        {
            disposalManager.TriggerDispose();
        }
    }

    public void PlayCloseAnimation()
    {
        if (isOpen && bottleAnimator != null)
        {
            bottleAnimator.SetTrigger("Close");
            isOpen = false;
        }
    }
}