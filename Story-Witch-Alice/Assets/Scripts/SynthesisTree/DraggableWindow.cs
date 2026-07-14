using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("拖拽手柄")]
    public RectTransform dragHandle;          // TitleBar 的 RectTransform

    [Header("整个星盘的根物体")]
    public Transform starChartRoot;           // StarChart 的 Transform

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

        // 把屏幕像素位移转成世界空间位移
        float worldPerPixel = Camera.main.orthographicSize * 2f / Screen.height;
        Vector3 worldDelta = new Vector3(delta.x * worldPerPixel, delta.y * worldPerPixel, 0);

        // 移动根物体，所有子物体自动跟随
        starChartRoot.position += worldDelta;
    }
}