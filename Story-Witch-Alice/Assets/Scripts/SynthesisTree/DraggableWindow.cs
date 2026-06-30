using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableWindow : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    public RectTransform windowRect;   // 窗口的 RectTransform
    public RectTransform dragHandle;   // 标题栏（只有拖这里才会移动窗口）

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 检查鼠标是否在 dragHandle 范围内
        if (!RectTransformUtility.RectangleContainsScreenPoint(dragHandle, eventData.position, eventData.pressEventCamera))
        {
            eventData.pointerDrag = null; // 取消拖拽
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        windowRect.anchoredPosition += eventData.delta;
    }
}