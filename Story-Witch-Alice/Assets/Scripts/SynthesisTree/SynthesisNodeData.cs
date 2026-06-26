using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SynthesisNodeData
{
    public string elementID;
    public Vector2 position;
    public Vector2 velocity;
    public GameObject nodeObject;
   // 改成存 ID，而不是直接存节点引用，避免循环序列化
    public List<string> connectedNodeIDs = new List<string>();
}