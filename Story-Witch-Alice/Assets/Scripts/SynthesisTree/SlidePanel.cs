using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SlidePanel : MonoBehaviour
{
    [Header("面板引用")]
    public RectTransform panelRect;         // TagFilterPanel
    public RectTransform arrowRect;         // ArrowIcon（同时也是按钮）

    [Header("设置")]
    public float slideDuration = 0.3f;
    public float expandOffsetX = 200f;

    private Vector2 hiddenPanelPos;
    private Vector2 hiddenArrowPos;
    private bool isOpen = false;
    private Coroutine currentAnim;

    void Start()
    {
        hiddenPanelPos = panelRect.anchoredPosition;
        hiddenArrowPos = arrowRect.anchoredPosition;
        isOpen = false;
    }

    public void Toggle()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);

        Vector2 offset = isOpen ? Vector2.zero : new Vector2(expandOffsetX, 0);

        currentAnim = StartCoroutine(SlideTo(
            hiddenPanelPos + offset,
            hiddenArrowPos + offset
        ));
        isOpen = !isOpen;
    }

    IEnumerator SlideTo(Vector2 panelTarget, Vector2 arrowTarget)
    {
        Vector2 panelStart = panelRect.anchoredPosition;
        Vector2 arrowStart = arrowRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            t = 1f - Mathf.Pow(1f - t, 3f);
            panelRect.anchoredPosition = Vector2.Lerp(panelStart, panelTarget, t);
            arrowRect.anchoredPosition = Vector2.Lerp(arrowStart, arrowTarget, t);
            yield return null;
        }

        panelRect.anchoredPosition = panelTarget;
        arrowRect.anchoredPosition = arrowTarget;
    }
}