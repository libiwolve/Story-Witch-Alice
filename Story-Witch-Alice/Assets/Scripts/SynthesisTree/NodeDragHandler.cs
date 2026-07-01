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

    void Update()
    {
        // 右键检测（放在 Update 里更可靠）
        if (isHovering && Input.GetMouseButtonDown(1))
        {
            isDraggingOut = true;
            SpawnPhysicalElement();
        }

        // 拖拽中
        if (isDragging)
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            transform.position = mouseWorld;
            nodeData.position = mouseWorld;
        }
        else if (isDraggingOut && spawnedElement != null)
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            spawnedElement.transform.position = mouseWorld;
        }

        // 右键松手
        if (isDraggingOut && Input.GetMouseButtonUp(1))
        {
            isDraggingOut = false;
            if (spawnedElement != null)
            {
                Rigidbody2D rb = spawnedElement.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.gravityScale = 1f;
                    rb.velocity = new Vector2(0, -2f);
                }
                Collider2D[] cols = spawnedElement.GetComponents<Collider2D>();
                foreach (var col in cols) col.enabled = true;
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

    void SpawnPhysicalElement()
    {
        Debug.Log($"右键拖出元素: {nodeData.elementID}");

        ElementData data = graph.GetElementDataByID(nodeData.elementID);
        if (data == null)
        {
            Debug.LogError($"找不到 ElementData: {nodeData.elementID}");
            return;
        }

        GameObject prefab = AlchemyManager.Instance?.GetPrefabForElement(data);
        if (prefab == null)
        {
            Debug.LogError($"找不到物理预制体: {data.elementID}");
            return;
        }

        Vector3 spawnPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        spawnPos.z = 0;
        spawnedElement = Instantiate(prefab, spawnPos, Quaternion.identity);

        PhysicsElement pe = spawnedElement.GetComponent<PhysicsElement>();
        if (pe != null)
        {
            pe.elementData = data;
            pe.sourceSlot = null;
        }

        Rigidbody2D rb = spawnedElement.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        Collider2D[] cols = spawnedElement.GetComponents<Collider2D>();
        foreach (var col in cols) col.enabled = false;
    }

    bool IsMouseOver()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }
}