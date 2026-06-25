using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SynthesisNodeData
{
    public string elementID;
    public Vector2 position;
    public Vector2 velocity;
    public GameObject nodeObject;          // 场景中的节点实例
    public LineRenderer lineRenderer;      // 连线
    public List<SynthesisNodeData> connectedNodes = new List<SynthesisNodeData>();
}