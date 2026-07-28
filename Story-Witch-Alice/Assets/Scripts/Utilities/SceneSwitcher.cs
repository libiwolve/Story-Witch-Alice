using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换工具。
/// 使用 Additive 模式加载商店场景，不卸载主场景，零数据丢失。
/// </summary>
public class SceneSwitcher : MonoBehaviour
{
    [Header("场景名称")]
    public string mainSceneName = "SampleScene";
    public string shopSceneName = "Shop";

    /// <summary>
    /// 附加加载商店场景（保留主场景全部数据）
    /// </summary>
    public void GoToShopScene()
    {
        // 避免重复加载
        if (SceneManager.GetSceneByName(shopSceneName).isLoaded) return;
        SceneManager.LoadScene(shopSceneName, LoadSceneMode.Additive);
    }

    /// <summary>
    /// 卸载商店场景，回到主场景
    /// </summary>
    public void GoToMainScene()
    {
        SceneManager.UnloadSceneAsync(shopSceneName);
    }
}
