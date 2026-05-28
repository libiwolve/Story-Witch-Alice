using UnityEngine;

public class PhysicsElement : MonoBehaviour
{
    public ElementData elementData;
    public UIDraggableElement sourceSlot;   // 生成它的槽位脚本

    // 掉进锅里
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pot"))
        {
            // 通知合成管理器
            AlchemyManager.Instance?.AddIngredient(elementData);
            // 恢复物品栏图标
            if (sourceSlot != null)
                sourceSlot.RestoreIcon();
            // 销毁自身
            Destroy(gameObject);
        }
    }

    // 碰到地面（或其他非锅物体）时飞回
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            // 通知槽位开始飞回
            if (sourceSlot != null)
                sourceSlot.OnElementMissedPot(gameObject);
        }
    }
}