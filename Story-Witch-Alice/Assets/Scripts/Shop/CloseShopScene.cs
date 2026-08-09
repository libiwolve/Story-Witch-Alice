using UnityEngine;
using UnityEngine.SceneManagement;

public class CloseShopScene : MonoBehaviour
{
    public string shopSceneName = "Shop";

    public void CloseShop()
    {
        SceneManager.UnloadSceneAsync(shopSceneName);

        if (LoadShopScene.Instance != null && LoadShopScene.Instance.mainSceneRoot != null)
        {
            LoadShopScene.Instance.mainSceneRoot.SetActive(true);
        }
    }
}