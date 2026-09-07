using UnityEngine;
using UnityEngine.EventSystems;

public class AliceInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    [Header("Animator")]
    public Animator animator;

    [Header("抓取阈值")]
    public float grabSpeedThreshold = 500f;    // 速度阈值，超过则进入挣扎

    [Header("触发器名称")]
    public string hoverTrigger = "Hover";
    public string liftedTrigger = "Lifted";
    public string releasedTrigger = "Released";

    private Camera mainCamera;
    private bool isHovering = false;
    private Vector3 grabStartMousePos;
    private float grabStartTime;

    void Start()
    {
        mainCamera = Camera.main;
    }

    // ========== 鼠标悬停 ==========

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (animator != null)
            animator.SetTrigger(hoverTrigger);  // AliceIdle1 → AliceIdle2
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        // 如果没有被抓，回到待机
        if (animator != null)
            animator.SetTrigger("Idle");  // AliceIdle2 → AliceIdle1
    }

    // ========== 鼠标按下 ==========

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isHovering) return;

        // 记录按下时间和位置，用于计算抓取速度
        grabStartMousePos = eventData.position;
        grabStartTime = Time.time;

        // 判断抓取位置：头发（上方）还是后颈（中下方）
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(eventData.position);
        Vector3 relativePos = transform.InverseTransformPoint(worldPos);
        int struggleType = (relativePos.y > 0.3f) ? 0 : 1; // 0=头发, 1=后颈

        if (animator != null)
        {
            animator.SetInteger("StruggleType", struggleType);
            animator.SetTrigger(liftedTrigger);  // → Lifted_Start
        }
    }

    // ========== 拖拽 ==========

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = transform.position.z;
        transform.position = worldPos;
    }

    // ========== 松手 ==========

    public void OnEndDrag(PointerEventData eventData)
    {
        // 计算抓取速度
        float grabSpeed = Vector3.Distance(eventData.position, grabStartMousePos) / (Time.time - grabStartTime);

        // 判断是否进入挣扎
        bool shouldStruggle = grabSpeed >= grabSpeedThreshold;
        if (animator != null)
        {
            animator.SetBool("ShouldStruggle", shouldStruggle);
            animator.SetTrigger(releasedTrigger);
        }

        // 如果鼠标还在爱丽丝上方，重新进入悬停状态
        // （PointerExit/PointerEnter 会自动处理，不需要额外操作）
    }
}