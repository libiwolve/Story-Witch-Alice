using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("拖拽手柄")]
    public RectTransform dragHandle;          // TitleBar 的 RectTransform

    [Header("需要一起移动的物体")]
    public RectTransform titleBarRect;         // TitleBar 的 RectTransform
    public RectTransform backgroundRect;       // StarChartBackground 的 RectTransform
    public Transform starChartTransform;       // StarChart 的 Transform（世界空间）

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(dragHandle, eventData.position, eventData.pressEventCamera))
        {
            eventData.pointerDrag = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.delta;

        // 移动 UI 物体
        if (titleBarRect != null) titleBarRect.anchoredPosition += delta;
        if (backgroundRect != null) backgroundRect.anchoredPosition += delta;

        // 移动 StarChart（世界空间）
        if (starChartTransform != null)
        {
            float worldPerPixel = Camera.main.orthographicSize * 2f / Screen.height;
            Vector3 worldDelta = new Vector3(delta.x * worldPerPixel, delta.y * worldPerPixel, 0);
            starChartTransform.position += worldDelta;

            var graph = starChartTransform.GetComponent<SynthesisGraph>();
            if (graph != null)
                graph.SyncNodePositionsAfterDrag(worldDelta);
        }
    }
}