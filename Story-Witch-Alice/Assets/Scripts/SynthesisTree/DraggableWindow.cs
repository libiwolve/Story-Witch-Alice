using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform windowRect;   // StarChartCanvas 的 RectTransform
    public RectTransform dragHandle;   // TitleBar 的 RectTransform

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(dragHandle, eventData.position, eventData.pressEventCamera))
        {
            eventData.pointerDrag = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 用世界坐标移动，绕过 RectTransform 锚点限制
        Vector3 screenDelta = new Vector3(eventData.delta.x, eventData.delta.y, 0);
        windowRect.transform.position += screenDelta;
    }
}