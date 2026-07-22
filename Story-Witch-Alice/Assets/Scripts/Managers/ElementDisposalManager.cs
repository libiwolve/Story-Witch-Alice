using UnityEngine;
using System.Collections.Generic;

public class ElementDisposalManager : MonoBehaviour
{
    public static ElementDisposalManager Instance { get; private set; }

    [Header("清除按钮")]
    public KeyCode clearAllKey = KeyCode.Delete;  // 按 Delete 键一键清除

    [Header("UI提示")]
    public GameObject disposalPanel;               // 显示有多少元素被标记的 UI
    public TMPro.TextMeshProUGUI countText;        // 标记数量文字

    private List<PhysicsElement> markedElements = new List<PhysicsElement>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 按 Delete 键一键清除所有标记元素
        if (Input.GetKeyDown(clearAllKey))
        {
            ClearAllMarkedElements();
        }

        // 更新 UI
        UpdateUI();
    }

    /// <summary>
    /// 标记一个元素
    /// </summary>
    public void MarkElement(PhysicsElement element)
    {
        if (!markedElements.Contains(element))
        {
            markedElements.Add(element);
        }
    }

    /// <summary>
    /// 取消标记一个元素
    /// </summary>
    public void UnmarkElement(PhysicsElement element)
    {
        markedElements.Remove(element);
    }

    /// <summary>
    /// 一键清除所有被标记的元素
    /// </summary>
    public void ClearAllMarkedElements()
    {
        if (markedElements.Count == 0) return;

        Debug.Log($"正在清除 {markedElements.Count} 个元素...");

        // 倒序销毁，避免列表修改冲突
        for (int i = markedElements.Count - 1; i >= 0; i--)
        {
            if (markedElements[i] != null)
            {
                markedElements[i].ForceDestroy();
            }
        }

        markedElements.Clear();
    }

    /// <summary>
    /// 更新 UI 显示
    /// </summary>
    void UpdateUI()
    {
        if (countText != null)
        {
            countText.text = markedElements.Count.ToString();
        }

        if (disposalPanel != null)
        {
            disposalPanel.SetActive(markedElements.Count > 0);
        }
    }
}