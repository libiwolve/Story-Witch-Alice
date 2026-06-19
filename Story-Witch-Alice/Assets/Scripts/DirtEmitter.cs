using UnityEngine;

public class DirtEmitter : MonoBehaviour
{
    public GameObject dirtBitPrefab;
    public float emitRate = 0.05f;
    public float initialDownSpeed = 0.5f;   // 初始向下速度
    public float inheritVelocity = 0.5f;    // 继承父物体速度的比例

    private float timer;
    private Rigidbody2D parentRb;

    void Start()
    {
        parentRb = GetComponentInParent<Rigidbody2D>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= emitRate)
        {
            timer = 0f;
            EmitBit();
        }
    }

    void EmitBit()
    {
        if (dirtBitPrefab == null) return;

        // 在物体底部附近随机位置生成
        Vector3 spawnPos = transform.position + Vector3.down * 0.3f + (Vector3)(Random.insideUnitCircle * 0.15f);
        GameObject bit = Instantiate(dirtBitPrefab, spawnPos, Quaternion.identity);

        Rigidbody2D bitRb = bit.GetComponent<Rigidbody2D>();
        if (bitRb != null)
        {
            // 只给向下的初始速度
            bitRb.velocity = Vector2.down * initialDownSpeed;

            // 继承父物体速度（拖拽甩动时渣渣会跟着甩出去一点）
            if (parentRb != null)
            {
                bitRb.velocity += parentRb.velocity * inheritVelocity;
            }
        }
    }
}