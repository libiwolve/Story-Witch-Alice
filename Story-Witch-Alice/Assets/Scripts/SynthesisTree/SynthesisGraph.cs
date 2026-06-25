using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SynthesisGraph : MonoBehaviour
{
    [Header("节点预制体")]
    public GameObject nodePrefab;           // 节点预制体（需挂 NodeDragHandler）

    [Header("节点参数")]
    public float nodeBaseScale = 1f;        // 基础缩放
    public float productScaleMultiplier = 1.5f; // 产物节点放大倍数
    public Color normalColor = Color.white;
    public Color dimColor = new Color(1, 1, 1, 0.3f);
    public Color highlightColor = Color.yellow;

    [Header("力导向参数")]
    public float repulsionForce = 5f;       // 排斥力
    public float springForce = 2f;          // 弹簧力
    public float springRestLength = 3f;     // 弹簧自然长度
    public float damping = 0.9f;            // 阻尼

    [Header("连线")]
    public Material lineMaterial;           // 连线材质（默认Sprites/Default即可）
    public float lineWidth = 0.08f;

    [Header("拖拽")]
    public float dragForce = 10f;

    // 内部数据
    private List<SynthesisNodeData> allNodes = new List<SynthesisNodeData>();
    private SynthesisNodeData draggedNode;
    private Camera mainCamera;

    // 合成关系：元素ID -> 可合成出的产物ID列表
    private Dictionary<string, List<string>> synthesisMap = new Dictionary<string, List<string>>();

    void Start()
    {
        mainCamera = Camera.main;
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

                // 双向关联（方便连线）
                synthesisMap[ingID].Add(productID);
                synthesisMap[productID].Add(ingID);
            }
        }
        // 去重
        foreach (var key in synthesisMap.Keys.ToList())
            synthesisMap[key] = synthesisMap[key].Distinct().ToList();
    }

    void CreateNodes()
    {
        // 收集所有出现过的元素ID（从配方的原料和产物中）
        HashSet<string> allIDs = new HashSet<string>();
        foreach (var recipe in AlchemyManager.Instance.allRecipes)
        {
            foreach (var ing in recipe.ingredients)
                if (ing != null) allIDs.Add(ing.elementID);
            if (recipe.product != null) allIDs.Add(recipe.product.elementID);
        }

        // 为每个ID创建节点
        foreach (string id in allIDs)
        {
            var node = new SynthesisNodeData();
            node.elementID = id;
            node.position = Random.insideUnitCircle * 3f;  // 随机初始位置
            node.nodeObject = Instantiate(nodePrefab, node.position, Quaternion.identity, transform);
            node.nodeObject.name = id;

            // 设置拖拽脚本
            var drag = node.nodeObject.GetComponent<NodeDragHandler>();
            if (drag == null) drag = node.nodeObject.AddComponent<NodeDragHandler>();
            drag.graph = this;
            drag.nodeData = node;

            // 设置图标（如果有elementData就设置，没有就用默认星星）
            SpriteRenderer sr = node.nodeObject.GetComponent<SpriteRenderer>();
            var elemData = GetElementData(id);
            if (sr != null && elemData != null && elemData.elementIcon != null)
                sr.sprite = elemData.elementIcon;
            // 统一调整颜色和大小
            if (sr != null)
            {
                sr.color = normalColor;
                bool isProduct = IsProduct(id);
                float scale = nodeBaseScale * (isProduct ? productScaleMultiplier : 1f);
                node.nodeObject.transform.localScale = Vector3.one * scale;
            }

            // 添加连线LineRenderer
            var lr = node.nodeObject.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.startColor = Color.white;
            lr.endColor = Color.white;
            lr.positionCount = 0;
            lr.enabled = false;
            node.lineRenderer = lr;

            allNodes.Add(node);
        }

        // 建立连接关系
        foreach (var node in allNodes)
        {
            if (synthesisMap.ContainsKey(node.elementID))
            {
                foreach (string neighborID in synthesisMap[node.elementID])
                {
                    var neighbor = allNodes.Find(n => n.elementID == neighborID);
                    if (neighbor != null && !node.connectedNodes.Contains(neighbor))
                    {
                        node.connectedNodes.Add(neighbor);
                        // 邻居的connectedNodes会在它自己被遍历时添加，这里不重复
                    }
                }
            }
        }
    }

    void Update()
    {
        ApplyForces();
        HandleDrag();
        UpdateLines();
    }

    void ApplyForces()
    {
        float dt = Time.deltaTime;
        foreach (var node in allNodes)
        {
            Vector2 force = Vector2.zero;

            // 排斥力（所有节点之间）
            foreach (var other in allNodes)
            {
                if (other == node) continue;
                Vector2 dir = node.position - other.position;
                float dist = dir.magnitude;
                if (dist < 0.01f) dist = 0.01f;
                dir /= dist;
                force += dir * repulsionForce / (dist * dist);
            }

            // 弹簧力（有连接关系的节点）
            foreach (var connected in node.connectedNodes)
            {
                Vector2 dir = connected.position - node.position;
                float dist = dir.magnitude;
                if (dist < 0.01f) dist = 0.01f;
                dir /= dist;
                float displacement = dist - springRestLength;
                force += dir * displacement * springForce;
            }

            node.velocity += force * dt;
            node.velocity *= damping;
            node.position += node.velocity * dt;
            node.nodeObject.transform.position = node.position;
        }
    }

    void HandleDrag()
    {
        if (draggedNode == null) return;
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        Vector2 target = mouseWorld;
        Vector2 dir = target - draggedNode.position;
        draggedNode.velocity += dir * dragForce * Time.deltaTime;
    }

    void UpdateLines()
    {
        foreach (var node in allNodes)
        {
            var lr = node.lineRenderer;
            int count = node.connectedNodes.Count;
            if (count == 0)
            {
                lr.enabled = false;
                continue;
            }
            lr.enabled = true;
            lr.positionCount = count * 2;
            int idx = 0;
            foreach (var neighbor in node.connectedNodes)
            {
                lr.SetPosition(idx, node.position);
                lr.SetPosition(idx + 1, neighbor.position);
                idx += 2;
            }
        }
    }

    // 拖拽开始（由NodeDragHandler调用）
    public void OnNodeDragStart(SynthesisNodeData node)
    {
        draggedNode = node;
        // 高亮相关节点，非相关变暗
        var related = GetAllRelated(node);
        foreach (var n in allNodes)
        {
            var sr = n.nodeObject.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            if (n == node || related.Contains(n))
                sr.color = highlightColor;
            else
                sr.color = dimColor;
        }
    }

    // 拖拽结束
    public void OnNodeDragEnd()
    {
        draggedNode = null;
        foreach (var n in allNodes)
        {
            var sr = n.nodeObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = normalColor;
        }
    }

    // BFS获取所有关联节点
    HashSet<SynthesisNodeData> GetAllRelated(SynthesisNodeData start)
    {
        var visited = new HashSet<SynthesisNodeData>();
        var queue = new Queue<SynthesisNodeData>();
        queue.Enqueue(start);
        visited.Add(start);
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var nb in cur.connectedNodes)
            {
                if (!visited.Contains(nb))
                {
                    visited.Add(nb);
                    queue.Enqueue(nb);
                }
            }
        }
        return visited;
    }

    // 高亮指定节点（供左侧列表调用）
    public void HighlightNode(string elementID)
    {
        var node = allNodes.Find(n => n.elementID == elementID);
        if (node == null) return;

        foreach (var n in allNodes)
        {
            var sr = n.nodeObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = dimColor;
        }
        var related = GetAllRelated(node);
        foreach (var n in related)
        {
            var sr = n.nodeObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;
        }
        // 目标节点额外放大
        node.nodeObject.transform.localScale = Vector3.one * nodeBaseScale * productScaleMultiplier * 1.3f;
    }

    // 判断某个ID是否是产物（至少一个配方的product）
    bool IsProduct(string id)
    {
        foreach (var recipe in AlchemyManager.Instance.allRecipes)
        {
            if (recipe.product != null && recipe.product.elementID == id)
                return true;
        }
        return false;
    }

    // 从AlchemyManager获取ElementData
    ElementData GetElementData(string id)
    {
        if (AlchemyManager.Instance == null) return null;
        if (AlchemyManager.Instance.allElements != null)
        {
            foreach (var e in AlchemyManager.Instance.allElements)
                if (e != null && e.elementID == id) return e;
        }
        return null;
    }

    // 供外部获取节点（拖拽用）
    public ElementData GetElementDataByID(string id) => GetElementData(id);
}