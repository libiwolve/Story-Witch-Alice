using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 爱丽丝的「身体」。
///
/// 它只做三件事：
///   1. 接住鼠标事件（悬停 / 按下 / 拖拽 / 松手）；
///   2. 每帧算出「脚底离地面多高」，按两个高度阈值判断她现在属于
///      站着 / 被拎在空中 / 正在坠落 哪一种；
///   3. 把这个判断写成 Animator 的参数（IsHeldAir、IsNearGround）和几个 Trigger，
///      交给 Assets/Animations/CharacterAnimation/alice.controller 决定播哪一段动画。
///
/// 分工：动画状态机负责「表演」，本脚本负责「物理与判定」，两者只通过参数通信。
/// 所以你在 Animator 窗口里怎么连线都行，不用回来改这个脚本。
/// </summary>
[RequireComponent(typeof(Animator))]
public class AliceDriver : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    IDragHandler
{
    /// <summary>身体状态：站着 / 被拎着 / 正在坠落。播哪段动画由 Animator 决定，这里只是「物理上发生了什么」。</summary>
    public enum BodyState { Grounded, Held, Falling }

    // ==================== 引用 ====================

    [Header("引用（留空会自动找）")]
    [Tooltip("爱丽丝的 Animator。留空 = 取同一物体上的 Animator")]
    public Animator animator;

    [Tooltip("脚底锚点。建议在 alice 下建一个空物体，拖到她的脚底，再拖到这里。留空 = 退而用图片框的底边（会随动画帧上下跳）")]
    public Transform footPoint;

    [Tooltip("地面参考。把一个处在「地面高度」的物体拖进来（桌面 / 最下面 / 自建空物体都行）。留空 = 用下面记录的地面高度")]
    public Transform groundReference;

    [Tooltip("可选。填了就用它移动爱丽丝（她的 Body Type 必须是 Kinematic）。留空 = 直接改 transform，也就是不加刚体的走法。两种视觉上一样，加了刚体对物理系统更规范")]
    public Rigidbody2D body;

    // ==================== 地面感知 ====================

    [Header("地面感知（单位：米）")]
    [Tooltip("脚底离地超过这个高度，才算「被拎到空中」→ 播 alicedrag1。调大 = 要拎更高她才开始惊慌")]
    public float airborneHeight = 1.2f;

    [Tooltip("脚底离地低于这个高度，就算「到地面了」→ 播 alicelanding 并吸附落地。必须小于上面那个值，两个值之间那段是防抖死区")]
    public float landedHeight = 0.3f;

    [Tooltip("地面高度。没拖 groundReference 时生效。可以在组件右上角菜单里选「把当前位置记为地面」一键写入")]
    public float recordedGroundY = 0f;

    // ==================== 拖拽与坠落 ====================

    [Header("拖拽")]
    [Tooltip("抓起时保持抓取点与爱丽丝的相对位置。勾上 = 不会一按就跳到鼠标正中心")]
    public bool keepGrabOffset = true;

    [Tooltip("拖拽时不允许把她拖到地面以下")]
    public bool clampAboveGroundOnDrag = true;

    [Header("坠落")]
    [Tooltip("松手后延迟多久才开始走抛物线与下落（秒）。用「最高点对齐 putdown 末帧」这套方案时保持 0；它只在你想要「松手先定住一下再掉」时才用")]
    public float fallStartDelay = 0f;

    [Tooltip("坠落加速度（米/秒²）。想要匀速下落，就把这个值调很大，再用下面的最大速度限制住")]
    public float gravity = 8f;

    [Tooltip("坠落最大速度（米/秒）")]
    public float maxFallSpeed = 6f;

    [Header("松手惯性（甩出去的手感）")]
    [Tooltip("松手时是否带上拖拽的惯性。取消勾选 = 松手她只垂直下落")]
    public bool releaseInertia = true;

    [Tooltip("用最近几帧的位移算速度。元素的惯性脚本里用的是 5，保持一致手感就填 5")]
    public int velocitySampleFrames = 5;

    [Tooltip("惯性倍率。1 = 原样；调小更像轻轻放下，调大更像被甩出去")]
    public float inertiaScale = 0.6f;

    [Tooltip("惯性最大横向速度（米/秒）。只管横向；竖直方向由下面的抛物线规则管，所以甩得快也不会把她甩飞")]
    public float maxInertiaSpeed = 4f;

    [Tooltip("横向减速（米/秒²）。横向速度按这个值衰减到 0；填 0 = 一直飘不减速")]
    public float horizontalDamping = 4f;

    // ==================== 抛物线（最高点对齐 putdown 末帧） ====================

    [Header("抛物线（最高点对齐 putdown 末帧）")]
    [Tooltip("可选。把 aliceputdown 这个 AnimationClip 拖进来，脚本就自动按它的时长算最高点时间 —— 以后改了 clip 长度也不用回来改数字")]
    public AnimationClip putDownClip;

    [Tooltip("最高点要在松手后第几秒到达（没拖上面那个 clip 时才用这个值）。aliceputdown 36 帧 @60fps = 0.6 秒")]
    public float apexTime = 0.6f;

    [Tooltip("勾上 = 不管怎么松手都保证她先升到最高点再落（她一定会上升）；不勾 = 只把「上提速度」的上限压在这条抛物线上，手感优先（松得慢就早点到顶、直接落）")]
    public bool alwaysRiseToApex = false;

    // ==================== Animator 参数名 ====================

    [Header("Animator 参数名")]
    [Tooltip("鼠标移上去时发的触发器")]
    public string hoverTrigger = "Hover";
    [Tooltip("鼠标移开时发的触发器")]
    public string idleTrigger = "Idle";
    [Tooltip("左键按下时发的触发器。留给音效/特效，动画可以不用它")]
    public string liftedTrigger = "Lifted";
    [Tooltip("左键松开时发的触发器")]
    public string releasedTrigger = "Released";
    [Tooltip("布尔：正在被拎在空中（按住左键 且 离地够高）")]
    public string isHeldAirBool = "IsHeldAir";
    [Tooltip("布尔：脚底已经贴近/接触地面")]
    public string isNearGroundBool = "IsNearGround";

    // ==================== 运行时只读 ====================

    [Header("运行时（只读，调参时盯着这里对）")]
    [SerializeField] private BodyState currentState = BodyState.Grounded;
    [SerializeField] private float airHeight;
    [SerializeField] private float footWorldY;
    [SerializeField] private float groundY;
    [SerializeField] private bool allParamsFound;
    [SerializeField] private bool useRigidbody;
    [SerializeField] private Vector2 lastReleaseVelocity;
    [SerializeField] private float resolvedApexTime;
    [SerializeField] private float maxUpwardSpeed;

    // ---- 内部状态 ----
    private SpriteRenderer spriteRenderer;
    private bool isHovering;
    private bool isHolding;
    private Vector3 grabOffset;
    private Vector2 velocity;                       // 世界速度，y 向上为正
    private float fallTimer;
    private int holdAirLatchFrame = -1;
    private bool groundRecorded;

    // 拖拽时采样最近几帧的位置，松手时用它算惯性速度（和 PhysicElement 同一套做法）
    private readonly Queue<(Vector3 pos, float time)> recentPositions = new Queue<(Vector3, float)>();

    private int hHover, hIdle, hLifted, hReleased, hIsHeldAir, hIsNearGround;
    private bool pHover, pIdle, pLifted, pReleased, pIsHeldAir, pIsNearGround;

    // ==================== 生命周期 ====================

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (body == null)
        {
            // 只自动认领 Kinematic 刚体。Dynamic 刚体会和「直接改 transform」打架，忽略并提醒。
            Rigidbody2D found = GetComponent<Rigidbody2D>();
            if (found != null && found.bodyType == RigidbodyType2D.Kinematic) body = found;
        }

        useRigidbody = body != null;

        Rigidbody2D any = GetComponent<Rigidbody2D>();
        if (any != null && any.bodyType == RigidbodyType2D.Dynamic)
        {
            Debug.LogWarning("[AliceDriver] 检测到 Dynamic 的 Rigidbody2D。本脚本是直接改位置的，动态刚体会和它互相打架（抖动/下坠/漂移）。请把 Body Type 改成 Kinematic，或者把刚体删掉。");
        }
    }

    void Start()
    {
        if (groundReference == null && !groundRecorded)
        {
            recordedGroundY = FootWorldYNow();
            groundRecorded = true;
            Debug.Log($"[AliceDriver] 没有指定地面参考，已把开始时的脚底位置记为地面 y = {recordedGroundY:F3}");
        }

        CacheParameters();
        RefreshSensing();
    }

    void Update()
    {
        if (currentState == BodyState.Falling) UpdateFalling();

        RefreshSensing();
        PushAnimatorParams();
        UpdateApexReadout();
    }

    // ==================== 地面感知 ====================

    /// <summary>每帧重算：地面高度、脚底高度、离地高度。</summary>
    void RefreshSensing()
    {
        groundY = GroundYNow();
        footWorldY = FootWorldYNow();
        airHeight = footWorldY - groundY;
    }

    float GroundYNow()
    {
        return groundReference != null ? groundReference.position.y : recordedGroundY;
    }

    /// <summary>脚底的世界高度。优先用 footPoint，其次用图片框底边（会随动画帧跳动，只适合先跑起来看效果）。</summary>
    float FootWorldYNow()
    {
        if (footPoint != null) return footPoint.position.y;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) return spriteRenderer.bounds.min.y;

        return transform.position.y;
    }

    /// <summary>当前「权威位置」：有刚体就用刚体的，没有就用 transform 的。</summary>
    Vector3 CurrentPosition
    {
        get { return body != null ? (Vector3)body.position : transform.position; }
    }

    /// <summary>移动爱丽丝。有 Kinematic 刚体就写刚体的位置，否则直接写 transform。</summary>
    void MoveTo(Vector3 p)
    {
        if (body != null) body.position = p;
        else transform.position = p;
    }

    // ==================== 鼠标事件 ====================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovering) return;         // 防重入：Unity 有时会连发两次 Enter
        isHovering = true;
        if (isHolding) return;          // 拎着她的时候不播悬停
        Fire(hHover, pHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (isHolding) return;
        Fire(hIdle, pIdle);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (isHolding) return;

        isHolding = true;
        currentState = BodyState.Held;
        fallTimer = 0f;
        velocity = Vector2.zero;                          // 空中重新抓住时，先把坠落速度清掉
        recentPositions.Clear();                          // 上一次拖拽的采样不能带进来
        if (body != null) body.velocity = Vector2.zero;

        // 抓取偏移：记住「按下那一刻，爱丽丝相对于鼠标在哪」，拖的时候原样带着走。
        // 不记的话，一按下她就会跳到鼠标正中心。
        if (keepGrabOffset)
        {
            Vector3 mouse = CameraUtility.ScreenToWorld(eventData.position, transform.position.z);
            grabOffset = CurrentPosition - mouse;
        }
        else
        {
            grabOffset = Vector3.zero;
        }

        Fire(hLifted, pLifted);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isHolding) return;

        RefreshSensing();

        Vector3 target = CameraUtility.ScreenToWorld(eventData.position, transform.position.z) + grabOffset;

        if (clampAboveGroundOnDrag)
        {
            // 脚底刚好落在地面时，原点应该在的高度
            float footOffsetBelowOrigin = CurrentPosition.y - footWorldY;
            float minOriginY = groundY + footOffsetBelowOrigin;
            if (target.y < minOriginY) target.y = minOriginY;
        }

        MoveTo(target);
        RefreshSensing();

        // 记下这一帧的位置，松手时用它算惯性
        recentPositions.Enqueue((CurrentPosition, Time.time));
        while (recentPositions.Count > Mathf.Max(2, velocitySampleFrames)) recentPositions.Dequeue();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!isHolding) return;

        isHolding = false;
        fallTimer = 0f;

        // 松手惯性：横向用最近几帧的位移算（和元素同一套算法），竖直用抛物线反推
        velocity = BuildReleaseVelocity();
        lastReleaseVelocity = velocity;
        recentPositions.Clear();

        if (body != null) body.velocity = Vector2.zero;

        // 松手这一帧先别把 IsHeldAir 放下，免得它和 Released 抢同一条连线
        holdAirLatchFrame = Time.frameCount;

        Fire(hReleased, pReleased);

        if (airHeight <= landedHeight)
        {
            currentState = BodyState.Grounded;      // 本来就在地上，松手就地站住（惯性不生效）
            velocity = Vector2.zero;
        }
        else
        {
            currentState = BodyState.Falling;       // 离地还有距离，开始掉
        }
    }

    // ==================== 坠落 ====================

    /// <summary>用最近几帧的位移算出拖拽速度（和 PhysicElement 同一套算法，手感才对得上）。</summary>
    Vector2 MeasureDragVelocity()
    {
        if (recentPositions.Count < 2) return Vector2.zero;

        (Vector3 pos, float time) oldest = recentPositions.Peek();
        float span = Time.time - oldest.time;
        if (span <= 0.02f) return Vector2.zero;     // 采样时间太短，算出来会是天文数字

        Vector3 delta = (CurrentPosition - oldest.pos) / span;
        return new Vector2(delta.x, delta.y) * inertiaScale;
    }

    /// <summary>putdown 的时长：拖了 clip 就用 clip 的，否则用填的秒数。</summary>
    float ResolveApexTime()
    {
        if (putDownClip != null && putDownClip.length > 0.01f) return putDownClip.length;
        return Mathf.Max(apexTime, 0.01f);
    }

    /// <summary>
    /// 上提速度的上限：抛物线里「最高点的时间 = 初速度 ÷ 重力」，反推就是 初速度 = 重力 × 到顶时间。
    /// 把这个值当作上提速度的天花板，最快的一次上提最高点就刚好落在 putdown 的末帧；
    /// 松得慢的话最高点来得更早 —— 所以不管什么方向什么速度，apexTime 之后她一定在下落。
    /// </summary>
    float MaxUpwardSpeed()
    {
        return gravity * Mathf.Max(ResolveApexTime() - fallStartDelay, 0f);
    }

    Vector2 BuildReleaseVelocity()
    {
        if (!releaseInertia) return Vector2.zero;

        Vector2 v = MeasureDragVelocity();

        // 横向单独夹（不再夹整条向量）：否则一次猛上提会把横向的额度吃光
        v.x = Mathf.Clamp(v.x, -maxInertiaSpeed, maxInertiaSpeed);

        float ceiling = MaxUpwardSpeed();
        if (alwaysRiseToApex) v.y = ceiling;        // 保证一定先上升，最高点正好在末帧
        else if (v.y > ceiling) v.y = ceiling;      // 只压上限，手感优先

        return v;
    }

    void UpdateApexReadout()
    {
        resolvedApexTime = ResolveApexTime();
        maxUpwardSpeed = MaxUpwardSpeed();
    }

    void UpdateFalling()
    {
        fallTimer += Time.deltaTime;

        if (fallTimer >= fallStartDelay)
        {
            // 竖直：重力加速，向下速度不超过上限（松手时若是往上甩的，速度为正，会先升后落）
            velocity.y -= gravity * Time.deltaTime;
            if (velocity.y < -maxFallSpeed) velocity.y = -maxFallSpeed;

            // 横向：按减速度衰减到 0（填 0 就一直飘）
            if (horizontalDamping > 0f)
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, horizontalDamping * Time.deltaTime);

            Vector3 p = CurrentPosition;
            p.x += velocity.x * Time.deltaTime;
            p.y += velocity.y * Time.deltaTime;
            MoveTo(p);
        }

        RefreshSensing();

        if (airHeight <= landedHeight) Land();
    }

    void Land()
    {
        // 吸附：把脚底恰好挪到地面高度，避免她陷进桌面里
        Vector3 p = CurrentPosition;
        p.y += groundY - footWorldY;
        MoveTo(p);

        velocity = Vector2.zero;
        fallTimer = 0f;
        currentState = BodyState.Grounded;
        RefreshSensing();
    }

    // ==================== 推参数给 Animator ====================

    void PushAnimatorParams()
    {
        if (animator == null) return;

        // 松手那一帧继续把 IsHeldAir 报成「还在空中」，这样状态机在同一瞬间只会看到 Released 一条线是通的
        bool heldAirNow = (isHolding || Time.frameCount == holdAirLatchFrame) && airHeight > airborneHeight;

        SetBool(hIsHeldAir, pIsHeldAir, heldAirNow);
        SetBool(hIsNearGround, pIsNearGround, airHeight <= landedHeight);
    }

    // ==================== 参数缓存与自检 ====================

    void CacheParameters()
    {
        hHover = Animator.StringToHash(hoverTrigger);
        hIdle = Animator.StringToHash(idleTrigger);
        hLifted = Animator.StringToHash(liftedTrigger);
        hReleased = Animator.StringToHash(releasedTrigger);
        hIsHeldAir = Animator.StringToHash(isHeldAirBool);
        hIsNearGround = Animator.StringToHash(isNearGroundBool);

        pHover = HasParameter(hoverTrigger);
        pIdle = HasParameter(idleTrigger);
        pLifted = HasParameter(liftedTrigger);
        pReleased = HasParameter(releasedTrigger);
        pIsHeldAir = HasParameter(isHeldAirBool);
        pIsNearGround = HasParameter(isNearGroundBool);

        List<string> missing = new List<string>();
        if (!pHover) missing.Add(hoverTrigger);
        if (!pIdle) missing.Add(idleTrigger);
        if (!pLifted) missing.Add(liftedTrigger);
        if (!pReleased) missing.Add(releasedTrigger);
        if (!pIsHeldAir) missing.Add(isHeldAirBool);
        if (!pIsNearGround) missing.Add(isNearGroundBool);

        allParamsFound = missing.Count == 0;

        if (!allParamsFound)
        {
            Debug.LogWarning($"[AliceDriver] Animator 里还没有这些参数：{string.Join(", ", missing)}。缺的那个本脚本就不会去设置它。请在 Animator 窗口的 Parameters 页建好，再在组件右上角菜单选「重新检查 Animator 参数」。");
        }
    }

    bool HasParameter(string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        if (string.IsNullOrEmpty(paramName)) return false;

        // animator.parameters 每次访问都会新分配一个数组，所以只在 Start / 手动重查时调
        AnimatorControllerParameter[] ps = animator.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].name == paramName) return true;
        }
        return false;
    }

    void SetBool(int hash, bool exists, bool value)
    {
        if (!exists || animator == null) return;
        animator.SetBool(hash, value);
    }

    void Fire(int hash, bool exists)
    {
        if (!exists || animator == null) return;
        animator.SetTrigger(hash);
    }

    // ==================== 组件右键菜单 ====================

    [ContextMenu("把当前位置记为地面")]
    void RecordGroundHere()
    {
        RefreshSensing();
        recordedGroundY = footWorldY;
        groundRecorded = true;
        Debug.Log($"[AliceDriver] 已把当前脚底位置记为地面 y = {recordedGroundY:F3}（青点所在的位置）");
    }

    [ContextMenu("重新检查 Animator 参数")]
    void RecheckParameters()
    {
        CacheParameters();
        Debug.Log($"[AliceDriver] 参数检查完成，六个参数齐全 = {allParamsFound}");
    }

    // ==================== 场景视图辅助线 ====================

    void OnDrawGizmosSelected()
    {
        float g = (groundReference != null) ? groundReference.position.y : recordedGroundY;
        float x = transform.position.x;

        Gizmos.color = new Color(0.95f, 0.25f, 0.25f);      // 红：地面线
        Gizmos.DrawLine(new Vector3(x - 2.5f, g, 0f), new Vector3(x + 2.5f, g, 0f));

        Gizmos.color = new Color(1f, 0.85f, 0.15f);         // 黄：起飞线（超过它才算「被拎在空中」）
        Gizmos.DrawLine(new Vector3(x - 1.2f, g + airborneHeight, 0f), new Vector3(x + 1.2f, g + airborneHeight, 0f));

        Gizmos.color = new Color(1f, 0.55f, 0.1f);          // 橙：落地线（低于它就算「到地面了」）
        Gizmos.DrawLine(new Vector3(x - 0.8f, g + landedHeight, 0f), new Vector3(x + 0.8f, g + landedHeight, 0f));

        Gizmos.color = Color.cyan;                          // 青点：当前脚底
        Gizmos.DrawWireSphere(new Vector3(x, FootWorldYNow(), 0f), 0.12f);
    }
}
