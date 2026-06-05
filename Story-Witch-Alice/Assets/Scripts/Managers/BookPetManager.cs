using UnityEngine;
using UnityEngine.EventSystems;

public class BookPet : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    public GameObject fullUIPanel;      // 展开后的完整 UI
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
    }

    // 鼠标进入
        public void OnPointerEnter(PointerEventData eventData)
    {
        if (isUIOpen) return;
        
        // 清掉可能残留的 Close Trigger，避免冲突
        animator.ResetTrigger(closeTrigger);
        animator.SetTrigger(hoverTrigger);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isUIOpen)
        {
            // 清掉可能残留的 Hover Trigger
            animator.ResetTrigger(hoverTrigger);
            animator.SetTrigger(closeTrigger);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isUIOpen)
        {
            OpenUI();
        }
    }

    // 打开 UI
    void OpenUI()
    {
        isUIOpen = true;
        GetComponent<Collider2D>().enabled = false;
        if (fullUIPanel != null)
            fullUIPanel.SetActive(true);
    }

    // 关闭 UI（由 X 按钮调用）
    public void CloseUI()
    {
        isUIOpen = false;
        GetComponent<Collider2D>().enabled = true;
        if (fullUIPanel != null)
            fullUIPanel.SetActive(false);
        

        // 播放合书动画
        animator.SetTrigger(closeTrigger);
    }
}