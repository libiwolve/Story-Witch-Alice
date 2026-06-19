using UnityEngine;
using UnityEngine.EventSystems;

public class BookPet : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    public GameObject fullUIPanel;      // 展开后的完整 UI
    public GameObject orbitCenter;      // 鱼缸轨道系统（替代合成树面板）
    public Animator animator;           // 书宠的 Animator
    public GameObject closeButton;      // UI 里的 X 按钮

    [Header("Trigger Names")]
    public string hoverTrigger = "Hover";
    public string closeTrigger = "Close";

    private bool isUIOpen = false;

    void Start()
    {
        if (fullUIPanel != null)
            fullUIPanel.SetActive(false);
        if (orbitCenter != null)
            orbitCenter.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isUIOpen) return;

        animator.ResetTrigger(closeTrigger);
        animator.SetTrigger(hoverTrigger);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isUIOpen)
        {
            animator.ResetTrigger(hoverTrigger);
            animator.SetTrigger(closeTrigger);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isUIOpen)
            OpenUI();
    }

    void OpenUI()
    {
        isUIOpen = true;
        GetComponent<Collider2D>().enabled = false;
        if (fullUIPanel != null)
            fullUIPanel.SetActive(true);
        if (orbitCenter != null)
            orbitCenter.SetActive(true);
    }

    public void CloseUI()
    {
        isUIOpen = false;
        GetComponent<Collider2D>().enabled = true;
        if (fullUIPanel != null)
            fullUIPanel.SetActive(false);
        if (orbitCenter != null)
            orbitCenter.SetActive(false);

        animator.SetTrigger(closeTrigger);
    }
}
