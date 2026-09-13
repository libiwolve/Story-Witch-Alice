using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EventCardSystem : MonoBehaviour
{
    public static EventCardSystem Instance { get; private set; }
    public static bool BlocksInput => Instance != null && Instance.modalOpen;
    public EventDeckState State { get; } = new EventDeckState();
    private EventCardCatalog catalog;
    private EventCardView view;
    private EventCardKind currentCard;
    private bool revealing;
    private bool modalOpen;
    private RemnantData visitor;
    private readonly System.Random random = new System.Random();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        Instance = null;
        SceneManager.sceneLoaded -= Bootstrap;
        SceneManager.sceneLoaded += Bootstrap;
    }

    private static void Bootstrap(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SampleScene" || Instance != null) return;
        var go = new GameObject("Event Card System");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<EventCardSystem>();
    }

    private void Awake()
    {
        Instance = this;
        catalog = Resources.Load<EventCardCatalog>("EventCardCatalog");
        if (catalog == null)
        {
            Debug.LogError("Missing Resources/EventCardCatalog asset.");
            enabled = false;
            return;
        }
        // The original project creates currency only in Shop. Events need it before the first visit.
        if (CurrencyManager.Instance == null)
            new GameObject("CurrencyManager").AddComponent<CurrencyManager>();
        view = gameObject.AddComponent<EventCardView>();
        view.Build(this, catalog);
        Refresh();
    }

    private void Update()
    {
        if (view != null) view.SetVisible(LoadShopScene.Instance == null || !LoadShopScene.Instance.IsShopOpen);
    }

    public void AdvanceTime()
    {
        if (modalOpen || ShopOpen || !State.TryAdvance(out currentCard)) return;
        visitor = null; // A visit remains available until the next time tick.
        modalOpen = true;
        revealing = true;
        view.ShowCard(currentCard);
        Refresh();
        StartCoroutine(Reveal());
    }

    private IEnumerator Reveal()
    {
        yield return view.Flip();
        revealing = false;
        view.EnableChoices();
    }

    public void ResolveCard(bool enterShop)
    {
        if (revealing || !State.HasPendingCard) return;
        if (currentCard == EventCardKind.Meteor)
        {
            if (CurrencyManager.Instance == null)
            {
                view.ShowError("思绪系统尚未就绪，请稍后重试。");
                return;
            }
        }
        if (currentCard == EventCardKind.Victoria)
        {
            visitor = catalog.victoria;
            if (enterShop && !CanVisit())
            {
                view.ShowError("商店或遗民数据未就绪，可选择稍后再说。");
                return;
            }
        }
        if (!State.Resolve()) return;
        // Mark resolved before notifying currency listeners, preventing reentrant reward claims.
        if (currentCard == EventCardKind.Meteor) CurrencyManager.Instance.AddThoughts(200);
        modalOpen = false;
        view.HideModal();
        if (enterShop && currentCard == EventCardKind.Victoria) VisitShop();
        if (State.NeedsDeck) OpenDeckEditor();
        Refresh();
    }

    private bool ShopOpen => LoadShopScene.Instance != null && LoadShopScene.Instance.IsShopOpen;
    private bool CanVisit() => visitor != null && LoadShopScene.Instance != null
        && Application.CanStreamedLevelBeLoaded(LoadShopScene.Instance.shopSceneName);

    public void VisitShop()
    {
        if (CanVisit() && !ShopOpen) LoadShopScene.Instance.OpenShopForRemnant(visitor);
    }

    public void OpenDeckEditor()
    {
        if (!State.NeedsDeck || State.HasPendingCard) return;
        modalOpen = true;
        view.ShowDeck(State.CopyCounts());
        Refresh();
    }

    public void ConfirmDeck(int[] counts)
    {
        if (!State.BeginRound(counts, random)) return;
        modalOpen = false;
        view.HideModal();
        Refresh();
    }

    private void Refresh()
    {
        view.Refresh(State, !modalOpen && !State.NeedsDeck, !modalOpen && CanVisit());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
