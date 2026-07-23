using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换工具。
/// 挂到任意 GameObject 上，在 Button.onClick 中引用其方法。
/// </summary>
public class SceneSwitcher : MonoBehaviour
{
    /// <summary>
    /// 切换到主场景（SampleScene）
    /// </summary>
    public void GoToMainScene()
    {
        SceneManager.LoadScene("SampleScene");
    }

    /// <summary>
    /// 切换到商店场景（Shop）
    /// </summary>
    public void GoToShopScene()
    {
        SceneManager.LoadScene("Shop");
    }
}
