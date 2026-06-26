using UnityEngine;

public class NodeDragHandler : MonoBehaviour
{
    public SynthesisGraph graph;
    public SynthesisNodeData nodeData;

    private bool isDragging = false;
    private bool isHovering = false;
    private Vector3 originalScale;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        originalScale = transform.localScale;
    }

    void OnMouseOver()
    {
        if (!isHovering && !isDragging)
        {
            isHovering = true;
            // 只放大自己
            transform.localScale = originalScale * 1.5f;
            // 高亮自己和相关节点
            graph.OnNodeHoverStart(nodeData);
        }
    }

    void OnMouseExit()
    {
        if (isHovering)
        {
            isHovering = false;
            transform.localScale = originalScale;
            graph.OnNodeHoverEnd();
        }
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButton(0))
        {
            isDragging = true;
            // 拖拽时恢复原始大小
            transform.localScale = originalScale;
            graph.OnNodeDragStart(nodeData);
        }
        else if (Input.GetMouseButton(1))
        {
            // 右键拖出元素逻辑
        }
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        transform.position = mouseWorld;
        nodeData.position = mouseWorld;
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            graph.OnNodeDragEnd();
            // 如果鼠标还在节点上，重新放大
            if (IsMouseOver())
            {
                isHovering = true;
                transform.localScale = originalScale * 1.5f;
                graph.OnNodeHoverStart(nodeData);
            }
            else
            {
                transform.localScale = originalScale;
            }
        }
    }

    bool IsMouseOver()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }
}