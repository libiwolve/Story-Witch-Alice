using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 思绪货币管理器（单例）。
/// 管理玩家的「思绪」余额。
/// 起始 99999（测试用）。
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("货币设置")]
    public int startingThoughts = 99999;

    public int CurrentThoughts { get; private set; }

    // 事件
    public UnityAction<int> OnThoughtsChanged;   // 余额变化
    public UnityAction<int> OnThoughtsGained;    // 获取思绪
    public UnityAction<int> OnThoughtsSpent;     // 花费思绪
    public UnityAction OnPurchaseFailed;         // 余额不足

    void Awake()
    {
        if (Instance != null)
        {
            // 保留已有实例的余额数据，销毁新实例
            CurrentThoughts = Instance.CurrentThoughts;
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;
        CurrentThoughts = startingThoughts;
    }

    public void AddThoughts(int amount)
    {
        if (amount <= 0) return;
        CurrentThoughts += amount;
        OnThoughtsChanged?.Invoke(CurrentThoughts);
        OnThoughtsGained?.Invoke(amount);
    }

    public bool SpendThoughts(int amount)
    {
        if (amount <= 0) return true;
        if (CurrentThoughts < amount)
        {
            OnPurchaseFailed?.Invoke();
            return false;
        }
        CurrentThoughts -= amount;
        OnThoughtsChanged?.Invoke(CurrentThoughts);
        OnThoughtsSpent?.Invoke(amount);
        return true;
    }

    public void SetThoughts(int amount)
    {
        CurrentThoughts = Mathf.Max(0, amount);
        OnThoughtsChanged?.Invoke(CurrentThoughts);
    }
}