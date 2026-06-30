using UnityEngine;

public class NodeDragHandler : MonoBehaviour
{
    public SynthesisGraph graph;
    public SynthesisNodeData nodeData;

    private bool isDragging = false;
    private bool isDraggingOut = false;
    private GameObject spawnedElement;
    private Vector3 originalScale;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        originalScale = transform.localScale;
    }

    void OnMouseOver()
    {
        if (!isDragging)
        {
            transform.localScale = originalScale * 1.5f;
            graph.OnNodeHoverStart(nodeData);
        }
    }

    void OnMouseExit()
    {
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
            // 左键拖拽节点
            isDragging = true;
            transform.localScale = originalScale;
            graph.OnNodeDragStart(nodeData);
        }
        else if (Input.GetMouseButton(1))
        {
            // 右键拖出物理元素
            isDraggingOut = true;
            SpawnPhysicalElement();
        }
    }

    void OnMouseDrag()
    {
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
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            graph.OnNodeDragEnd();
            if (IsMouseOver())
            {
                transform.localScale = originalScale * 1.5f;
                graph.OnNodeHoverStart(nodeData);
            }
            else
            {
                transform.localScale = originalScale;
                graph.OnNodeHoverEnd();
            }
        }
        else if (isDraggingOut)
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

    void SpawnPhysicalElement()
    {
        ElementData data = graph.GetElementDataByID(nodeData.elementID);
        if (data == null) return;

        GameObject prefab = AlchemyManager.Instance?.GetPrefabForElement(data);
        if (prefab == null) return;

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