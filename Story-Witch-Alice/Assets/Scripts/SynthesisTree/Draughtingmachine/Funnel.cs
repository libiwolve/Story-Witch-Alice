using UnityEngine;

public class Funnel : MonoBehaviour
{
    public SynthesisGraph synthesisGraph;

    void OnTriggerEnter2D(Collider2D other)
    {
        PhysicsElement pe = other.GetComponent<PhysicsElement>();
        if (pe == null || pe.elementData == null) return;

        // 只接收向下掉落的元素
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb == null || rb.velocity.y >= 0) return;

        // 加入星图（待欢迎队列）
        if (synthesisGraph != null)
            synthesisGraph.AddNodePendingWelcome(pe.elementData.elementID);

        Debug.Log($"漏斗接收元素: {pe.elementData.elementName}，已加入星图");

        Destroy(other.gameObject);
    }
}