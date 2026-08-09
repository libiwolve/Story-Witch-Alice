using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadShopScene : MonoBehaviour
{
    public string shopSceneName = "Shop";
    public RemnantData remnantData;
    public GameObject mainSceneRoot;

    public static LoadShopScene Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void OpenShop()
    {
        if (mainSceneRoot != null)
            mainSceneRoot.SetActive(false);

        Camera mainCam = Camera.main;
        if (mainCam != null)
            mainCam.gameObject.SetActive(false);

        SceneManager.LoadScene(shopSceneName, LoadSceneMode.Additive);
        SceneManager.sceneLoaded += OnShopSceneLoaded;
    }

    void OnShopSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != shopSceneName) return;
        SceneManager.sceneLoaded -= OnShopSceneLoaded;

        ShopManager shop = FindObjectOfType<ShopManager>();
        if (shop != null && remnantData != null)
            shop.OpenShop(remnantData);
    }
}