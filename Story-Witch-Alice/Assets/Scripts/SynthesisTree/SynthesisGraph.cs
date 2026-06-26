using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SynthesisGraph : MonoBehaviour
{
    [Header("节点预制体")]
    public GameObject nodePrefab;

    [Header("节点视觉")]
    public float nodeBaseScale = 1f;
    public float productScaleMultiplier = 1.5f;
    public Color normalColor = Color.white;
    public Color dimColor = new Color(1, 1, 1, 0.3f);
    public Color highlightColor = Color.yellow;

    [Header("力导向参数")]
    public float repulsionForce = 5f;
    public float springForce = 2f;
    public float springRestLength = 3f;
    public float damping = 0.9f;

    [Header("Mesh连线")]
    public StarChartMesh starChartMesh;

    [Header("拖拽节点")]
    public float dragForce = 10f;

    [Header("缩放")]
    public float minScale = 0.3f;
    public float maxScale = 3f;
    public float zoomSpeed = 0.1f;
    public float zoomSmoothTime = 0.1f;

    [Header("生成范围")]
    public Vector2 spawnArea = new Vector2(6f, 6f);

    [Header("星图边界（固定，硬边界）")]
    public Vector2 mapSize = new Vector2(16f, 12f);
    public Vector2 mapCenter = Vector2.zero;
    [Header("漂浮动画")]
    public float driftAmplitude = 0.3f;   // 漂浮幅度
    public float driftFrequency = 0.5f;  // 漂浮频率

    [Header("拖拽惯性")]
    public float inertiaDamping = 0.95f;

    [Header("橡皮筋边界")]
    public float maxOverdrag = 1.5f;    // 允许超出的最大距离
    public float rubberBandForce = 8f;

    private List<SynthesisNodeData> allNodes = new List<SynthesisNodeData>();
    private SynthesisNodeData draggedNode;
    private Camera mainCamera;
    private Dictionary<string, List<string>> synthesisMap = new Dictionary<string, List<string>>();

    private bool isPanning = false;
    private Vector3 panStartMouseScreen;
    private Vector3 panStartMouseWorld;
    private Vector3 panStartPosition;

    private float targetScale;
    private float scaleVelocity;
    private Vector2 dragVelocity;

    void Start()
    {
        mainCamera = Camera.main;
        targetScale = transform.localScale.x;
        BuildSynthesisMap();
        CreateNodes();
    }

    void BuildSynthesisMap()
    {
        synthesisMap.Clear();
        var recipes = AlchemyManager.Instance.allRecipes;
        foreach (var recipe in recipes)
        {
            if (recipe.product == null) continue;
            string productID = recipe.product.elementID;
            foreach (var ing in recipe.ingredients)
            {
                if (ing == null) continue;
                string ingID = ing.elementID;
                if (!synthesisMap.ContainsKey(ingID))
                    synthesisMap[ingID] = new List<string>();
                if (!synthesisMap.ContainsKey(productID))
                    synthesisMap[productID] = new List<string>();
                synthesisMap[ingID].Add(productID);
                synthesisMap[productID].Add(ingID);
            }
        }
        foreach (var key in synthesisMap.Keys.ToList())
            synthesisMap[key] = synthesisMap[key].Distinct().ToList();
    }

    void CreateNodes()
    {
        HashSet<string> allIDs = new HashSet<string>();
        foreach (var recipe in AlchemyManager.Instance.allRecipes)
        {
            foreach (var ing in recipe.ingredients)
                if (ing != null) allIDs.Add(ing.elementID);
            if (recipe.product != null) allIDs.Add(recipe.product.elementID);
        }

        foreach (string id in allIDs)
        {
            var node = new SynthesisNodeData();
            node.elementID = id;
            node.position = new Vector2(
                Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f),
                Random.Range(-spawnArea.y / 2f, spawnArea.y / 2f)
            );
            node.nodeObject = Instantiate(nodePrefab, node.position, Quaternion.identity, transform);
            node.nodeObject.name = id;

            var drag = node.nodeObject.GetComponent<NodeDragHandler>();
            if (drag == null) drag = node.nodeObject.AddComponent<NodeDragHandler>();
            drag.graph = this;
            drag.nodeData = node;

            SpriteRenderer sr = node.nodeObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = normalColor;
                bool isProduct = IsProduct(id);
                float scale = nodeBaseScale * (isProduct ? productScaleMultiplier : 1f);
                node.nodeObject.transform.localScale = Vector3.one * scale;
            }

            allNodes.Add(node);
        }

        foreach (var node in allNodes)
        {
            if (synthesisMap.ContainsKey(node.elementID))
            {
                foreach (string neighborID in synthesisMap[node.elementID])
                {
                    if (!node.connectedNodeIDs.Contains(neighborID))
                        node.connectedNodeIDs.Add(neighborID);
                }
            }
        }
    }

    void Update()
    {
        ApplyForces();
        HandleDrag();
        ApplyInertia();   // 在 HandleDrag 之后
        HandlePan();
        HandleZoom();
        UpdateLines();
        ClampNodesToMap();
    }

    void ApplyForces()
    {
        float dt = Time.deltaTime;
        foreach (var node in allNodes)
        {
            Vector2 force = Vector2.zero;

            // 排斥力
            foreach (var other in allNodes)
            {
                if (other == node) continue;
                Vector2 dir = node.position - other.position;
                float dist = dir.magnitude;
                if (dist < 0.01f) dist = 0.01f;
                dir /= dist;
                force += dir * repulsionForce / (dist * dist);
            }

            // 弹簧力
            foreach (var connected in GetConnectedNodes(node))
            {
                Vector2 dir = connected.position - node.position;
                float dist = dir.magnitude;
                if (dist < 0.01f) dist = 0.01f;
                dir /= dist;
                float displacement = dist - springRestLength;
                force += dir * displacement * springForce;
            }

            // ★ 漂浮力（Perlin 噪声）
            float noiseX = Mathf.PerlinNoise(node.elementID.GetHashCode() * 0.1f, Time.time * driftFrequency) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(node.elementID.GetHashCode() * 0.1f + 100f, Time.time * driftFrequency) * 2f - 1f;
            force += new Vector2(noiseX, noiseY) * driftAmplitude;

            node.velocity += force * dt;
            node.velocity *= damping;
            node.position += node.velocity * dt;
            node.nodeObject.transform.position = node.position;
        }
    }

    void ApplyInertia()
    {
        if (draggedNode != null) return;  // 正在拖拽时不处理惯性

        if (dragVelocity.magnitude < 0.01f)
        {
            dragVelocity = Vector2.zero;
            return;
        }

        // 对正在拖拽的节点施加惯性位移
        foreach (var node in allNodes)
        {
            node.position += dragVelocity * Time.deltaTime;
            node.nodeObject.transform.position = node.position;
        }

        // 衰减
        dragVelocity *= inertiaDamping;
    }

    void HandleDrag()
    {
        if (draggedNode == null || draggedNode.nodeObject == null) return;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        // 记录拖拽速度（本帧位移 / 时间）
        Vector2 previousPos = draggedNode.position;
        draggedNode.position = mouseWorld;
        draggedNode.nodeObject.transform.position = mouseWorld;

        dragVelocity = (draggedNode.position - previousPos) / Time.deltaTime;
    }

    void HandlePan()
    {
        if (Input.GetMouseButtonDown(2))
        {
            isPanning = true;
            panStartMouseScreen = Input.mousePosition;
            panStartMouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            panStartMouseWorld.z = 0;
            panStartPosition = transform.position;
        }
        if (Input.GetMouseButtonUp(2))
        {
            isPanning = false;
        }

        if (isPanning)
        {
            float dragDistance = Vector3.Distance(Input.mousePosition, panStartMouseScreen);
            if (dragDistance < 5f) return;

            Vector3 currentMouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            currentMouseWorld.z = 0;
            Vector3 offset = currentMouseWorld - panStartMouseWorld;
            Vector3 newPosition = panStartPosition + offset;

            Vector3 delta = newPosition - transform.position;
            transform.position = newPosition;

            foreach (var node in allNodes)
            {
                node.position += (Vector2)delta;
                node.nodeObject.transform.position = node.position;
            }
        }
    }

    void HandleZoom()
    {
        Vector3 mousePos = Input.mousePosition;
        if (mousePos.x < 0 || mousePos.x > Screen.width ||
            mousePos.y < 0 || mousePos.y > Screen.height)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
            targetScale = Mathf.Clamp(targetScale + scroll * zoomSpeed, minScale, maxScale);

        float newScale = Mathf.SmoothDamp(transform.localScale.x, targetScale, ref scaleVelocity, zoomSmoothTime);
        if (Mathf.Abs(newScale - transform.localScale.x) < 0.001f) return;

        Vector3 mouseWorldBefore = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldBefore.z = 0;

        transform.localScale = Vector3.one * newScale;

        Vector3 mouseWorldAfter = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldAfter.z = 0;

        transform.position += mouseWorldBefore - mouseWorldAfter;
    }

    void UpdateLines()
    {
        if (starChartMesh == null) return;

        var lines = new List<(Vector2 from, Vector2 to)>();
        foreach (var node in allNodes)
        {
            foreach (var neighbor in GetConnectedNodes(node))
            {
                if (node.elementID.CompareTo(neighbor.elementID) < 0)
                {
                    Vector2 localFrom = transform.InverseTransformPoint(node.nodeObject.transform.position);
                    Vector2 localTo = transform.InverseTransformPoint(neighbor.nodeObject.transform.position);
                    lines.Add((localFrom, localTo));
                }
            }
        }
        starChartMesh.UpdateLines(lines);
    }

    /// <summary>
    /// 确保所有节点的包围盒不超出固定的星图边界。
    /// 如果超出，整体平移所有节点回到边界内。
    /// </summary>
    void ClampNodesToMap()
    {
        if (allNodes.Count == 0) return;

        float mapMinX = mapCenter.x - mapSize.x / 2f;
        float mapMaxX = mapCenter.x + mapSize.x / 2f;
        float mapMinY = mapCenter.y - mapSize.y / 2f;
        float mapMaxY = mapCenter.y + mapSize.y / 2f;

        foreach (var node in allNodes)
        {
            if (node.nodeObject == null) continue;
            Vector2 pos = node.position;

            // X 轴橡皮筋
            if (pos.x < mapMinX)
            {
                float overshoot = mapMinX - pos.x;
                float force = Mathf.Min(overshoot, maxOverdrag) * rubberBandForce;
                node.velocity += new Vector2(force * Time.deltaTime, 0);
            }
            else if (pos.x > mapMaxX)
            {
                float overshoot = pos.x - mapMaxX;
                float force = Mathf.Min(overshoot, maxOverdrag) * rubberBandForce;
                node.velocity -= new Vector2(force * Time.deltaTime, 0);
            }

            // Y 轴橡皮筋
            if (pos.y < mapMinY)
            {
                float overshoot = mapMinY - pos.y;
                float force = Mathf.Min(overshoot, maxOverdrag) * rubberBandForce;
                node.velocity += new Vector2(0, force * Time.deltaTime);
            }
            else if (pos.y > mapMaxY)
            {
                float overshoot = pos.y - mapMaxY;
                float force = Mathf.Min(overshoot, maxOverdrag) * rubberBandForce;
                node.velocity -= new Vector2(0, force * Time.deltaTime);
            }

            // 硬限制：绝对不能超出 maxOverdrag
            pos.x = Mathf.Clamp(pos.x, mapMinX - maxOverdrag, mapMaxX + maxOverdrag);
            pos.y = Mathf.Clamp(pos.y, mapMinY - maxOverdrag, mapMaxY + maxOverdrag);

            node.position = pos;
            node.nodeObject.transform.position = pos;
        }
    }
    public void OnNodeHoverStart(SynthesisNodeData node)
    {
        var related = GetAllRelated(node);
        foreach (var n in allNodes)
        {
            var sr = n.nodeObject?.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            Color targetColor = related.Contains(n) ? highlightColor : dimColor;
            sr.material.SetColor("_Color", targetColor);
        }
    }

    public void OnNodeHoverEnd()
    {
        foreach (var n in allNodes)
        {
            var sr = n.nodeObject?.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.material.SetColor("_Color", normalColor);
        }
    }

    public void OnNodeDragStart(SynthesisNodeData node)
    {
        draggedNode = node;
        var related = GetAllRelated(node);
        foreach (var n in allNodes)
        {
            var sr = n.nodeObject?.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            Color targetColor = (n == node || related.Contains(n)) ? highlightColor : dimColor;
            sr.material.SetColor("_Color", targetColor);
        }
    }

    public void OnNodeDragEnd()
    {
        draggedNode = null;
        foreach (var n in allNodes)
        {
            var sr = n.nodeObject?.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.material.SetColor("_Color", normalColor);
        }
    }
    HashSet<SynthesisNodeData> GetAllRelated(SynthesisNodeData start)
    {
        var result = new HashSet<SynthesisNodeData> { start };
        foreach (var nb in GetConnectedNodes(start))
            result.Add(nb);
        return result;
    }

    public void HighlightNode(string elementID)
    {
        var node = allNodes.Find(n => n.elementID == elementID);
        if (node == null) return;

        var related = GetAllRelated(node);
        foreach (var n in allNodes)
        {
            var sr = n.nodeObject?.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            // 改成 material.SetColor
            sr.material.SetColor("_Color", related.Contains(n) ? highlightColor : dimColor);
        }

        Vector3 targetWorldPos = node.nodeObject.transform.position;
        Vector3 screenCenter = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        screenCenter.z = 0;
        Vector3 newPos = transform.position + screenCenter - targetWorldPos;
        transform.position = newPos;
        transform.localScale = Vector3.one * 1.2f;
    }
    bool IsProduct(string id)
    {
        return AlchemyManager.Instance.allRecipes.Any(r => r.product != null && r.product.elementID == id);
    }

    public ElementData GetElementDataByID(string id) => GetElementData(id);

    ElementData GetElementData(string id)
    {
        return AlchemyManager.Instance?.allElements?.FirstOrDefault(e => e != null && e.elementID == id);
    }

    List<SynthesisNodeData> GetConnectedNodes(SynthesisNodeData node)
    {
        return node.connectedNodeIDs
            .Select(id => allNodes.Find(n => n.elementID == id))
            .Where(found => found != null)
            .ToList();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(mapCenter, mapSize);

        Gizmos.color = new Color(1, 1, 1, 0.5f);
        Gizmos.DrawWireCube(mapCenter, spawnArea);
    }
}