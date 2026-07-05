using UnityEngine;

public class NodeDragHandler : MonoBehaviour
{
    public SynthesisGraph graph;
    public SynthesisNodeData nodeData;

    private bool isDragging = false;
    private bool isDraggingOut = false;
    private bool isHovering = false;
    private GameObject spawnedElement;
    private Vector3 originalScale;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        originalScale = transform.localScale;
    }

    void SpawnPhysicalElement()
    {
        ElementData data = graph.GetElementDataByID(nodeData.elementID);
        if (data == null) return;

        spawnedElement = PhysicsElementSpawner.SpawnForDrag(data);
        if (spawnedElement == null) return;
        if (spawnedElement != null)
        {
            spawnedElement.layer = LayerMask.NameToLayer("StarChart");
            
            // 同时需要把子物体也切换层
            foreach (Transform child in spawnedElement.transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("StarChart");
            }
        }

        var pe = spawnedElement.GetComponent<PhysicsElement>();
        if (pe != null)
        {
            pe.elementData = data;
            pe.sourceSlot = null;
            pe.isControlledByNodeDrag = true;
        }
    }

    void Update()
    {
        // 右键拖出
        if (isHovering && Input.GetMouseButtonDown(1))
        {
            isDraggingOut = true;
            SpawnPhysicalElement();
        }

        // 拖拽中
        if (isDraggingOut && spawnedElement != null)
        {
            spawnedElement.transform.position = CameraUtility.MouseToWorld();
        }

        // 右键松手
        if (isDraggingOut && Input.GetMouseButtonUp(1))
        {
            isDraggingOut = false;
            if (spawnedElement != null)
            {
                spawnedElement.layer = LayerMask.NameToLayer("Default");
                foreach (Transform child in spawnedElement.transform)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Default");
                }
                PhysicsElementSpawner.Release(spawnedElement);
                var pe = spawnedElement.GetComponent<PhysicsElement>();
                if (pe != null) pe.isControlledByNodeDrag = false;
                // 给个向下初速度
                var rb = spawnedElement.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = new Vector2(0, -2f);
            }
            spawnedElement = null;
        }
    }

    void OnMouseOver()
    {
        if (!isHovering && !isDragging)
        {
            isHovering = true;
            transform.localScale = originalScale * 1.5f;
            graph.OnNodeHoverStart(nodeData);
        }
    }

    void OnMouseExit()
    {
        isHovering = false;
        if (!isDragging)
        {
            transform.localScale = originalScale;
            graph.OnNodeHoverEnd();
        }
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButton(0))
        {
            isDragging = true;
            transform.localScale = originalScale;
            graph.OnNodeDragStart(nodeData);
        }
    }

    void OnMouseDrag()
    {
        // 左键拖拽逻辑保留为空，因为 Update 里已经处理了
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            graph.OnNodeDragEnd();
            // 检查鼠标是否还在节点上
            if (IsMouseOver())
            {
                isHovering = true;
                transform.localScale = originalScale * 1.5f;
                graph.OnNodeHoverStart(nodeData);
            }
            else
            {
                isHovering = false;
                transform.localScale = originalScale;
                graph.OnNodeHoverEnd();
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