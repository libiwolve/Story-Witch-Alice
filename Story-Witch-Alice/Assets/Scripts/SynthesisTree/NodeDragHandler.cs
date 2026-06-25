using UnityEngine;

public class NodeDragHandler : MonoBehaviour
{
    public SynthesisGraph graph;
    public SynthesisNodeData nodeData;

    private bool isDraggingInGraph = false;
    private bool isDraggingOutElement = false;
    private GameObject spawnedElement;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButton(0))
        {
            isDraggingInGraph = true;
            graph.OnNodeDragStart(nodeData);
        }
        else if (Input.GetMouseButton(1))
        {
            isDraggingOutElement = true;
            SpawnPhysicalElement();
        }
    }

    void OnMouseDrag()
    {
        if (isDraggingInGraph)
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            nodeData.position = mouseWorld;
            transform.position = mouseWorld;
        }
        else if (isDraggingOutElement && spawnedElement != null)
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            spawnedElement.transform.position = mouseWorld;
        }
    }

    void OnMouseUp()
    {
        if (isDraggingInGraph)
        {
            isDraggingInGraph = false;
            graph.OnNodeDragEnd();
        }
        else if (isDraggingOutElement)
        {
            isDraggingOutElement = false;
            if (spawnedElement != null)
            {
                Rigidbody2D rb = spawnedElement.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.gravityScale = 1f;
                    rb.velocity = new Vector2(0, -2f);
                }
                // 启用碰撞
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

        // 拖拽中禁用碰撞
        Collider2D[] cols = spawnedElement.GetComponents<Collider2D>();
        foreach (var col in cols) col.enabled = false;
    }
}