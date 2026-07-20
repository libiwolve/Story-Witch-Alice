using UnityEngine;

public class Funnel : MonoBehaviour
{
    public SynthesisGraph synthesisGraph;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 检测被拖入的物理元素
        PhysicsElement pe = other.GetComponent<PhysicsElement>();
        if (pe == null) return;
        if (pe.elementData == null) return;

        // 加入星图
        if (synthesisGraph != null)
            synthesisGraph.AddNode(pe.elementData.elementID);

        Debug.Log($"漏斗接收元素: {pe.elementData.elementName}，已加入星图");

        // 销毁拖入的物理元素
        Destroy(other.gameObject);
    }
}