using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class SynthesisGraph : MonoBehaviour
{
    [Header("节点预制体")]
    public GameObject nodePrefab;

    [Header("节点视觉")]
    public float nodeBaseScale = 1f;
    public float productScaleMultiplier = 1.5f;
    public Color normalColor = new Color(1f, 0.95f, 0.7f, 1f);   // 暖金白星光色
    public Color dimColor = new Color(1f, 1f, 1f, 0.3f);
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
    [Header("生成偏移")]
    public Vector2 spawnOffset = new Vector2(0f, 0f);  // 在 Inspector 里调整

    [Header("星图边界（固定，硬边界）")]
    public Vector2 mapSize = new Vector2(16f, 12f);
    public Vector2 mapCenter = Vector2.zero;

    [Header("漂浮动画")]
    public float driftAmplitude = 0.3f;
    public float driftFrequency = 0.5f;

    [Header("拖拽惯性")]
    public float inertiaDamping = 0.95f;

    [Header("橡皮筋边界")]
    public float maxOverdrag = 1.5f;
    public float rubberBandForce = 8f;

    [HideInInspector]
    public bool enableZoom = true;

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
        HashSet<string> unlockedIDs = new HashSet<string>();

        if (AlchemyManager.Instance != null)
        {
            foreach (var recipe in AlchemyManager.Instance.allRecipes)
            {
                foreach (var ing in recipe.ingredients)
                {
                    if (ing != null && AlchemyManager.Instance.IsElementUnlocked(ing.elementID))
                        unlockedIDs.Add(ing.elementID);
                }
                if (recipe.product != null && AlchemyManager.Instance.IsElementUnlocked(recipe.product.elementID))
                    unlockedIDs.Add(recipe.product.elementID);
            }
        }

        foreach (string id in unlockedIDs)
        {
            CreateSingleNode(id);
        }

        // 建立连接关系
        foreach (var node in allNodes)
        {
            if (synthesisMap.ContainsKey(node.elementID))
            {
                foreach (string neighborID in synthesisMap[node.elementID])
                {
                    var neighbor = allNodes.Find(n => n.elementID == neighborID);
                    if (neighbor != null && !node.connectedNodeIDs.Contains(neighborID))
                        node.connectedNodeIDs.Add(neighborID);
                }
            }
        }
    }

    public void AddNode(string elementID)
    {
        if (allNodes.Any(n => n.elementID == elementID)) return;
        CreateSingleNode(elementID);

        var node = allNodes.Find(n => n.elementID == elementID);
        if (node != null && synthesisMap.ContainsKey(elementID))
        {
            foreach (string neighborID in synthesisMap[elementID])
            {
                var neighbor = allNodes.Find(n => n.elementID == neighborID);
                if (neighbor != null && !node.connectedNodeIDs.Contains(neighborID))
                    node.connectedNodeIDs.Add(neighborID);
                if (neighbor != null && !neighbor.connectedNodeIDs.Contains(elementID))
                    neighbor.connectedNodeIDs.Add(elementID);
            }
        }
    }

    void CreateSingleNode(string id)
    {
        if (allNodes.Any(n => n.elementID == id)) return;

        var node = new SynthesisNodeData();
        node.elementID = id;
        node.position = new Vector2(
            Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f) + spawnOffset.x,
            Random.Range(-spawnArea.y / 2f, spawnArea.y / 2f) + spawnOffset.y
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
            var elemData = GetElementData(id);
            if (elemData != null && elemData.elementIcon != null)
                sr.sprite = elemData.elementIcon;

            // 和 CreateNodes 保持完全一致的缩放
            float scale = nodeBaseScale * (IsProduct(id) ? productScaleMultiplier : 1f);
            node.nodeObject.transform.localScale = Vector3.one * scale;

            sr.material.SetColor("_Color", normalColor);
        }

        allNodes.Add(node);
    }

    void Update()
    {
        ApplyForces();
        HandleDrag();
        ApplyInertia();
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

            // 漂浮力
            float noiseX = Mathf.PerlinNoise(node.elementID.GetHashCode() * 0.1f, Time.time * driftFrequency) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(node.elementID.GetHashCode() * 0.1f + 100f, Time.time * driftFrequency) * 2f - 1f;
            force += new Vector2(noiseX, noiseY) * driftAmplitude;

            node.velocity += force * dt;
            node.velocity *= damping;
            node.position += node.velocity * dt;
            node.nodeObject.transform.localPosition = transform.InverseTransformPoint(node.position);
        }
    }

    void ApplyInertia()
    {
        if (draggedNode != null) return;

        if (dragVelocity.magnitude < 0.01f)
        {
            dragVelocity = Vector2.zero;
            return;
        }

        // 只衰减速度，不移动任何节点
        dragVelocity *= inertiaDamping;
    }

    void HandleDrag()
    {
        if (draggedNode == null || draggedNode.nodeObject == null) return;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        // 记录拖拽速度（只用于被拖拽的节点）
        Vector2 previousPos = draggedNode.position;
        draggedNode.position = mouseWorld;
        draggedNode.nodeObject.transform.localPosition = transform.InverseTransformPoint(mouseWorld);

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
                node.nodeObject.transform.localPosition = transform.InverseTransformPoint(node.position);
            }
        }
    }

    void HandleZoom()
    {
        if (!enableZoom) return; 
        Vector3 mousePos = Input.mousePosition;
        if (mousePos.x < 0 || mousePos.x > Screen.width ||
            mousePos.y < 0 || mousePos.y > Screen.height)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            // ★ 用非线性映射让缩放更细腻
            // scroll 的值通常为 0.1 或 -0.1（每格滚轮）
            float delta = scroll * zoomSpeed;
            
            // 使用平方根曲线：小幅度滚动时变化更小，大幅度滚动时变化更大
            // 但限制了最大单帧变化量
            delta = Mathf.Sign(delta) * Mathf.Pow(Mathf.Abs(delta), 1.5f) * 0.5f;
            
            targetScale = Mathf.Clamp(targetScale + delta, minScale, maxScale);
        }

        float newScale = Mathf.SmoothDamp(transform.localScale.x, targetScale, ref scaleVelocity, zoomSmoothTime);
        if (Mathf.Abs(newScale - transform.localScale.x) < 0.001f) return;

        Vector3 mouseWorldBefore = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldBefore.z = 0;

        float scaleFactor = newScale / transform.localScale.x;

        transform.localScale = Vector3.one * newScale;

        Vector3 mouseWorldAfter = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldAfter.z = 0;

        transform.position += mouseWorldBefore - mouseWorldAfter;

        Vector3 chartCenter = transform.position;
        foreach (var node in allNodes)
        {
            Vector3 relativePos = node.position - (Vector2)chartCenter;
            relativePos *= scaleFactor;
            node.position = (Vector2)chartCenter + (Vector2)relativePos;
            node.nodeObject.transform.position = node.position;
        }
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

            // ★ 对所有越界节点施加回弹力（不仅仅是 draggedNode）
            

           // ★ 只施加回弹力，不钳制位置，让节点自然弹回
            if (pos.x < mapMinX)
            {
                float overshoot = mapMinX - pos.x;
                float force = Mathf.Clamp(overshoot * rubberBandForce, 0f, maxOverdrag * rubberBandForce);
                node.velocity += new Vector2(force * Time.deltaTime, 0f);
            }
            else if (pos.x > mapMaxX)
            {
                float overshoot = pos.x - mapMaxX;
                float force = Mathf.Clamp(overshoot * rubberBandForce, 0f, maxOverdrag * rubberBandForce);
                node.velocity -= new Vector2(force * Time.deltaTime, 0f);
            }

            if (pos.y < mapMinY)
            {
                float overshoot = mapMinY - pos.y;
                float force = Mathf.Clamp(overshoot * rubberBandForce, 0f, maxOverdrag * rubberBandForce);
                node.velocity += new Vector2(0f, force * Time.deltaTime);
            }
            else if (pos.y > mapMaxY)
            {
                float overshoot = pos.y - mapMaxY;
                float force = Mathf.Clamp(overshoot * rubberBandForce, 0f, maxOverdrag * rubberBandForce);
                node.velocity -= new Vector2(0f, force * Time.deltaTime);
            }

            // 位置不钳制，直接更新
            node.position = pos;
            node.nodeObject.transform.localPosition = transform.InverseTransformPoint(pos);
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
            if (sr != null)
                sr.material.SetColor("_Color", related.Contains(n) ? highlightColor : dimColor);
        }

        StartCoroutine(FlyToCenter(node));
    }

    System.Collections.IEnumerator FlyToCenter(SynthesisNodeData targetNode)
    {
        Vector2 center = new Vector2(mapCenter.x, mapCenter.y);
        Vector2 totalOffset = center - targetNode.position;

        var startPositions = new Dictionary<SynthesisNodeData, Vector2>();
        foreach (var n in allNodes)
        {
            if (n.nodeObject != null)
                startPositions[n] = n.position;
        }

        float duration = 0.6f;
        float elapsed = 0f;
        Vector2 currentOffset = Vector2.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = 1f - Mathf.Pow(1f - t, 3f);

            Vector2 targetOffset = totalOffset * t;
            currentOffset = Vector2.Lerp(currentOffset, targetOffset, 0.3f);

            foreach (var n in allNodes)
            {
                if (n.nodeObject == null || !startPositions.ContainsKey(n)) continue;
                n.position = startPositions[n] + currentOffset;
                n.nodeObject.transform.position = n.position;
            }

            yield return null;
        }

        foreach (var n in allNodes)
        {
            if (n.nodeObject == null || !startPositions.ContainsKey(n)) continue;
            n.position = startPositions[n] + totalOffset;
            n.nodeObject.transform.position = n.position;
        }

        dragVelocity = Vector2.zero;
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
    /// <summary>
/// 窗口拖拽后，同步所有节点的 position 数据
/// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(mapCenter, mapSize);

        Gizmos.color = new Color(1, 1, 1, 0.5f);
        Gizmos.DrawWireCube(mapCenter, spawnArea);
    }
    /// <summary>
    /// 根据一组元素ID，高亮并定位星盘视野到这些元素的中心点
    /// </summary>
    public void HighlightNodesByTag(List<string> elementIDs)
    {
        Debug.Log($"HighlightNodesByTag 被调用，elementIDs 数量: {elementIDs?.Count ?? 0}");
        if (elementIDs == null || elementIDs.Count == 0) return;

        var targetNodes = allNodes.Where(n => elementIDs.Contains(n.elementID)).ToList();
        if (targetNodes.Count == 0) return;

        // 1. 高亮目标节点，其他变暗
        foreach (var n in allNodes)
        {
            var sr = n.nodeObject?.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            if (targetNodes.Contains(n))
            {
                sr.material.SetColor("_Color", highlightColor);
                n.nodeObject.transform.localScale = Vector3.one * nodeBaseScale * productScaleMultiplier * 1.3f;
            }
            else
            {
                sr.material.SetColor("_Color", dimColor);
                n.nodeObject.transform.localScale = Vector3.one * nodeBaseScale;
            }
        }

        // 2. 计算包围盒中心
        Vector3 center = Vector3.zero;
        foreach (var node in targetNodes)
            center += node.nodeObject.transform.position;
        center /= targetNodes.Count;

        // 3. 平滑移动视野
        StartCoroutine(MoveViewToCenter(center));
    }

    /// <summary>
    /// 平滑移动视野，使目标世界坐标位于屏幕中心
    /// </summary>
    IEnumerator MoveViewToCenter(Vector3 targetWorldPos)
    {
        Debug.Log($"MoveViewToCenter 启动，目标世界坐标: {targetWorldPos}");
        Vector3 screenCenter = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        screenCenter.z = 0;

        Vector3 targetChartPos = transform.position + (screenCenter - targetWorldPos);

        // 如果超出硬边界，钳制到边界内
        float mapHalfW = mapSize.x / 2f;
        float mapHalfH = mapSize.y / 2f;
        targetChartPos.x = Mathf.Clamp(targetChartPos.x, mapCenter.x - mapHalfW, mapCenter.x + mapHalfW);
        targetChartPos.y = Mathf.Clamp(targetChartPos.y, mapCenter.y - mapHalfH, mapCenter.y + mapHalfH);

        Vector3 startPos = transform.position;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = 1f - Mathf.Pow(1f - t, 3f); // ease-out
            transform.position = Vector3.Lerp(startPos, targetChartPos, t);
            yield return null;
        }

        transform.position = targetChartPos;
    }
}