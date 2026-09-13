using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadShopScene : MonoBehaviour
{
    public string shopSceneName = "Shop";
    public RemnantData remnantData;
    public GameObject mainSceneRoot;

    public static LoadShopScene Instance { get; private set; }
    public bool IsShopOpen { get; private set; }
    private Camera hiddenCamera;
    private bool rootWasActive;

    void Awake()
    {
        Instance = this;
    }

    public void OpenShop()
    {
        OpenShopForRemnant(remnantData);
    }

    public void OpenShopForRemnant(RemnantData remnant)
    {
        if (IsShopOpen || !Application.CanStreamedLevelBeLoaded(shopSceneName)) return;
        remnantData = remnant;
        IsShopOpen = true;
        BookPet bookPet = FindObjectOfType<BookPet>();
        if (bookPet != null)
            bookPet.PrepareForSceneTransition();

        hiddenCamera = Camera.main;
        rootWasActive = mainSceneRoot != null && mainSceneRoot.activeSelf;
        if (mainSceneRoot != null)
            mainSceneRoot.SetActive(false);

        if (hiddenCamera != null)
            hiddenCamera.gameObject.SetActive(false);

        SceneManager.sceneLoaded += OnShopSceneLoaded;
        SceneManager.LoadScene(shopSceneName, LoadSceneMode.Additive);
    }

    void OnShopSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != shopSceneName) return;
        SceneManager.sceneLoaded -= OnShopSceneLoaded;

        ShopManager shop = FindObjectOfType<ShopManager>();
        if (shop != null && remnantData != null)
            shop.OpenShop(remnantData);
    }

    public void RestoreMainScene()
    {
        if (mainSceneRoot != null && rootWasActive) mainSceneRoot.SetActive(true);
        if (hiddenCamera != null) hiddenCamera.gameObject.SetActive(true);
        IsShopOpen = false;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnShopSceneLoaded;
        if (Instance == this) Instance = null;
    }
}
