using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Batch: -batchmode -executeMethod EventCardValidation.Run -logFile <path>
public static class EventCardValidation
{
    private const string Running = "EventCardValidation.Running";
    private const string PreviewOnly = "EventCardValidation.PreviewOnly";
    private static int step, balance;
    private static double deadline, nextAction;
    private static bool capturedCard;

    [MenuItem("Tools/Event Cards/Validate Deck Logic")]
    public static void ValidateLogic()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            var state = new EventDeckState();
            Check(!state.BeginRound(new[] { 8, 4, 7 }, new System.Random(seed)), "Reject 19 cards");
            Check(!state.BeginRound(new[] { 8, -1, 13 }, new System.Random(seed)), "Reject negative copies");
            Check(state.BeginRound(new[] { 8, 4, 8 }, new System.Random(seed)), "Accept 20 cards");
            var order = state.Queue.ToArray();
            Check(order.Length == 12, "Exactly 12 draws");
            Check(order.Count(x => x == EventCardKind.Meteor) <= 8 && order.Count(x => x == EventCardKind.Victoria) <= 4
                && order.Count(x => x == EventCardKind.Quiet) <= 8, "Draw without replacement");
            Check(!state.BeginRound(new[] { 20, 0, 0 }, new System.Random(seed)), "Cannot edit locked round");
            for (int tick = 0; tick < 12; tick++)
            {
                Check(state.TryAdvance(out var card) && card == order[tick], "Stable round order");
                Check(!state.TryAdvance(out _), "Pending card blocks second advance");
                Check(!state.NeedsDeck, "Do not edit before last resolution");
                Check(state.Resolve() && !state.Resolve(), "Resolve once only");
            }
            Check(state.NeedsDeck && !state.TryAdvance(out _), "Round ends exactly at 12");
            Check(state.BeginRound(new[] { 0, 0, 20 }, new System.Random(seed)), "Start next round");
            Check(state.TotalTicks == 12 && state.Tick == 0 && state.Round == 2, "Round and total counters");
        }
        var data = Resources.Load<EventCardCatalog>("EventCardCatalog");
        Check(data != null && data.cardBack != null && data.font != null && data.victoria != null, "Card asset references");
        Debug.Log("EVENT_CARDS_LOGIC_PASS: 300 seeds, round boundaries, duplicate protection, asset references.");
    }

    public static void Run()
    {
        try
        {
            ValidateLogic();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Running, true);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception e) { Fail(e); }
    }

    public static void RunPreview()
    {
        SessionState.SetBool(PreviewOnly, true);
        Run();
    }

    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (!SessionState.GetBool(Running, false)) return;
        step = 0;
        capturedCard = false;
        deadline = EditorApplication.timeSinceStartup + 100;
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        try
        {
            double now = EditorApplication.timeSinceStartup;
            if (now > deadline) throw new Exception("Play smoke test timed out at step " + step);
            if (!EditorApplication.isPlaying || now < nextAction) return;
            var system = EventCardSystem.Instance;
            if (system == null) return;
            switch (step)
            {
                case 0:
                    system.OpenDeckEditor();
                    var buttons = system.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                    buttons.First(b => b.name == "−").onClick.Invoke();
                    Check(!buttons.First(b => b.name == "确认牌组并开始下一轮").interactable, "19-card UI disables confirm");
                    buttons.First(b => b.name == "+").onClick.Invoke();
                    Check(buttons.First(b => b.name == "确认牌组并开始下一轮").interactable, "20-card UI enables confirm");
                    step = 10; nextAction = now + .5; break;
                case 10:
                    Capture("Temp/event-deck.png");
                    step = 11; nextAction = now + .5; break;
                case 11:
                    system.ConfirmDeck(new[] { 20, 0, 0 });
                    balance = CurrencyManager.Instance.CurrentThoughts;
                    system.AdvanceTime(); system.AdvanceTime();
                    Check(system.State.Tick == 1, "Double advance guarded");
                    step = 1; nextAction = now + 1; break;
                case 1:
                    if (!capturedCard)
                    {
                        Capture("Temp/event-card.png"); capturedCard = true;
                        if (SessionState.GetBool(PreviewOnly, false))
                        {
                            SessionState.SetBool(PreviewOnly, false);
                            SessionState.SetBool(Running, false);
                            EditorApplication.update -= Update;
                            Debug.Log("EVENT_CARDS_PREVIEW_PASS");
                            EditorApplication.Exit(0);
                        }
                        nextAction = now + .5; return;
                    }
                    int before = CurrencyManager.Instance.CurrentThoughts;
                    system.ResolveCard(false); system.ResolveCard(false);
                    Check(CurrencyManager.Instance.CurrentThoughts == before + 200, "Meteor grants exactly 200");
                    if (system.State.Tick < 12) { system.AdvanceTime(); nextAction = now + 1; }
                    else
                    {
                        Check(CurrencyManager.Instance.CurrentThoughts == balance + 2400, "Whole round rewards");
                        Check(system.State.NeedsDeck && EventCardSystem.BlocksInput, "Round editor opens");
                        system.ConfirmDeck(new[] { 0, 0, 20 });
                        system.AdvanceTime(); step = 2; nextAction = now + 1;
                    }
                    break;
                case 2:
                    int quietBalance = CurrencyManager.Instance.CurrentThoughts;
                    system.ResolveCard(false);
                    Check(CurrencyManager.Instance.CurrentThoughts == quietBalance, "Quiet card has no reward");
                    if (system.State.Tick < 12) { system.AdvanceTime(); nextAction = now + 1; }
                    else
                    {
                        system.ConfirmDeck(new[] { 0, 20, 0 });
                        system.AdvanceTime(); step = 3; nextAction = now + 1;
                    }
                    break;
                case 3:
                    system.ResolveCard(true);
                    step = 4; nextAction = now + 2; break;
                case 4:
                    var shop = UnityEngine.Object.FindObjectOfType<ShopManager>();
                    if (shop == null) return;
                    Check(shop.currentRemnant == Resources.Load<EventCardCatalog>("EventCardCatalog").victoria, "Victoria shop context");
                    Check(system.State.Round == 3 && system.State.Tick == 1, "Round preserved in shop");
                    UnityEngine.Object.FindObjectOfType<CloseShopScene>().CloseShop();
                    step = 5; nextAction = now + 2; break;
                case 5:
                    if (SceneManager.GetSceneByName("Shop").isLoaded) return;
                    Check(!LoadShopScene.Instance.IsShopOpen && Camera.main != null, "Camera restored after shop");
                    Check(system.State.Tick == 1 && !system.State.HasPendingCard, "Card settled across shop return");
                    system.AdvanceTime(); step = 6; nextAction = now + 1; break;
                case 6:
                    bool last = system.State.Tick == 12;
                    system.ResolveCard(last);
                    if (!last) { system.AdvanceTime(); nextAction = now + 1; }
                    else { step = 7; nextAction = now + 2; }
                    break;
                case 7:
                    if (!SceneManager.GetSceneByName("Shop").isLoaded) return;
                    Check(system.State.NeedsDeck, "Final visit keeps round complete");
                    UnityEngine.Object.FindObjectOfType<CloseShopScene>().CloseShop();
                    step = 8; nextAction = now + 2; break;
                case 8:
                    if (SceneManager.GetSceneByName("Shop").isLoaded) return;
                    Check(EventCardSystem.BlocksInput && system.State.NeedsDeck, "Editor resumes after final-card shop");
                    system.ConfirmDeck(new[] { 8, 4, 8 });
                    Check(system.State.Round == 4 && system.State.Tick == 0, "Next round after final visit");
                    SessionState.SetBool(Running, false);
                    EditorApplication.update -= Update;
                    Debug.Log("EVENT_CARDS_PLAY_PASS: meteor/quiet rounds, deck buttons, Victoria shop and final-card return.");
                    EditorApplication.Exit(0);
                    break;
            }
        }
        catch (Exception e) { Fail(e); }
    }

    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception("Event cards: " + message); }

    private static void Capture(string path)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        // Batch mode has no Game view to capture. Render the UI explicitly to a texture.
        var canvas = EventCardSystem.Instance.GetComponentInChildren<Canvas>();
        var originalMode = canvas.renderMode;
        var originalCamera = canvas.worldCamera;
        var cameraObject = new GameObject("Card Preview Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.08f, .07f, .12f);
        camera.cullingMask = 1 << 31;
        var transforms = canvas.GetComponentsInChildren<Transform>(true);
        var layers = transforms.Select(t => t.gameObject.layer).ToArray();
        foreach (var t in transforms) t.gameObject.layer = 31;
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previousTarget = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            string directory = System.IO.Path.Combine(Application.dataPath, "../Logs");
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, System.IO.Path.GetFileName(path)), texture.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode = originalMode;
            canvas.worldCamera = originalCamera;
            for (int i = 0; i < transforms.Length; i++) transforms[i].gameObject.layer = layers[i];
            RenderTexture.active = previousTarget;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static void Fail(Exception e)
    {
        SessionState.SetBool(Running, false);
        SessionState.SetBool(PreviewOnly, false);
        EditorApplication.update -= Update;
        Debug.LogException(e);
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
