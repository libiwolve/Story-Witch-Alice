using UnityEngine;
using UnityEngine.SceneManagement;

public class CloseShopScene : MonoBehaviour
{
    public string shopSceneName = "Shop";
    private bool closing;

    public void CloseShop()
    {
        if (closing) return;
        closing = true;
        var operation = SceneManager.UnloadSceneAsync(shopSceneName);
        if (operation != null) operation.completed += _ =>
        {
            if (LoadShopScene.Instance != null) LoadShopScene.Instance.RestoreMainScene();
        };
        else closing = false;
    }
}
