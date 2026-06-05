using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class TreePanelAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public RectTransform panelRect;
    public float peekAmount = 30f;        // 靠近时露出多少像素（改小就行）
    public float slideDuration = 0.3f;

    private Vector2 hiddenPos;
    private Vector2 peekPos;
    private Vector2 shownPos;
    private bool isExpanded = false;
    private Coroutine currentAnim;

    void Start()
    {
        // 用当前面板位置作为隐藏位置（就是你手动拖到的初始位置）
        hiddenPos = panelRect.anchoredPosition;
        // 靠近时向左偏移 peekAmount
        peekPos = hiddenPos + Vector2.left * peekAmount;
        // 完全展开：让面板左边缘贴到屏幕左边缘
        shownPos = new Vector2(-6.36f, hiddenPos.y);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isExpanded)
            StartSlide(peekPos);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isExpanded)
            StartSlide(hiddenPos);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isExpanded = !isExpanded;
        StartSlide(isExpanded ? shownPos : hiddenPos);
    }

    void StartSlide(Vector2 target)
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SlideTo(target));
    }

    IEnumerator SlideTo(Vector2 target)
    {
        Vector2 start = panelRect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            panelRect.anchoredPosition = Vector2.Lerp(start, target, elapsed / slideDuration);
            yield return null;
        }
        panelRect.anchoredPosition = target;
    }
}