using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 商店场景附加加载处理器。
/// 在 Awake 阶段立即销毁/禁用商店场景中与主场景冲突的组件，
/// 避免 EventSystem / AudioListener / Camera 重复错误。
/// 
/// 注意：使用 DestroyImmediate 确保在 OnEnable 之前完成。
/// </summary>
public class ShopSceneAdditiveHandler : MonoBehaviour
{
    void Awake()
    {
        // 在 Awake 阶段（早于任何 OnEnable）立即销毁冲突组件
        RemoveConflictingComponents();
    }

    void RemoveConflictingComponents()
    {
        // ⚠️ EventSystem 必须在 OnEnable 之前处理掉
        // DestroyImmediate 立即执行，不等到帧结束
        // 这样 EventSystem.OnEnable 永远不会被调用
        EventSystem[] events = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (var e in events)
        {
            if (e != null && e.gameObject.scene.name == "Shop")
            {
                DestroyImmediate(e.gameObject);
            }
        }

        // 禁用本场景的相机
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in cameras)
        {
            if (c != null && c.gameObject.scene.name == "Shop")
            {
                c.gameObject.SetActive(false);
            }
        }

        // 禁用本场景的 AudioListener
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (var l in listeners)
        {
            if (l != null && l.gameObject.scene.name == "Shop")
            {
                l.enabled = false;
            }
        }
    }
}
