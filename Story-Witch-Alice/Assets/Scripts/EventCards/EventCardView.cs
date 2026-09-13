using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class EventCardView : MonoBehaviour
{
    private EventCardSystem system;
    private EventCardCatalog catalog;
    private GameObject canvasObject, modal;
    private RectTransform panel, card;
    private Text status, deckTotal, error;
    private Button advance, edit, visit, confirm;
    private readonly Image[] ticks = new Image[12];
    private readonly Text[] countsText = new Text[3];
    private readonly Button[] plus = new Button[3], minus = new Button[3];
    private int[] draft;
    private GameObject front, back;
    private CanvasGroup choices;
    private static readonly Color Ink = new Color(.12f, .1f, .19f);
    private static readonly Color Gold = new Color(.83f, .68f, .39f);

    public void Build(EventCardSystem owner, EventCardCatalog data)
    {
        system = owner;
        catalog = data;
        canvasObject = new GameObject("Event Cards Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        RectTransform hud = Box(canvasObject.transform, "Time HUD", Ink, new Vector2(520, 112), Vector2.zero);
        hud.anchorMin = hud.anchorMax = hud.pivot = new Vector2(0, 1);
        hud.anchoredPosition = new Vector2(16, -16);
        status = Label(hud, "", 18, new Vector2(490, 26), new Vector2(0, 35));
        for (int i = 0; i < ticks.Length; i++)
            ticks[i] = Box(hud, "Tick " + (i + 1), Color.gray, new Vector2(34, 8),
                new Vector2(-231 + i * 42, 12)).GetComponent<Image>();
        advance = Button(hud, "调试：推进一格", new Vector2(170, 35), new Vector2(-164, -25), system.AdvanceTime);
        edit = Button(hud, "编辑牌组", new Vector2(140, 35), new Vector2(0, -25), system.OpenDeckEditor);
        visit = Button(hud, "遗民商店", new Vector2(140, 35), new Vector2(150, -25), system.VisitShop);
        modal = Box(canvasObject.transform, "Modal Blocker", new Color(0, 0, 0, .78f), Vector2.zero, Vector2.zero).gameObject;
        var full = (RectTransform)modal.transform;
        full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one;
        full.offsetMin = full.offsetMax = Vector2.zero;
        modal.SetActive(false);
    }

    public void SetVisible(bool visible) => canvasObject.SetActive(visible);
    public void HideModal() => modal.SetActive(false);

    public void Refresh(EventDeckState state, bool canAdvance, bool canVisit)
    {
        status.text = $"第 {Mathf.Max(1, state.Round)} 轮  ·  时间 {state.Tick}/12  ·  剩余 {12 - state.Tick} 张";
        for (int i = 0; i < ticks.Length; i++) ticks[i].color = i < state.Tick ? Gold : new Color(.28f, .26f, .34f);
        advance.interactable = canAdvance;
        edit.interactable = state.NeedsDeck && !state.HasPendingCard;
        visit.interactable = canVisit;
    }

    private void NewPanel(string title)
    {
        if (panel != null) { panel.gameObject.SetActive(false); Destroy(panel.gameObject); }
        modal.SetActive(true);
        panel = Box(modal.transform, title, Ink, new Vector2(960, 620), Vector2.zero);
        Label(panel, title, 28, new Vector2(880, 48), new Vector2(0, 270));
    }

    public void ShowCard(EventCardKind kind)
    {
        NewPanel("命运翻开一页");
        card = Box(panel, "Card", Gold, new Vector2(290, 390), new Vector2(-235, -5));
        back = Box(card, "Back", Color.white, new Vector2(282, 382), Vector2.zero).gameObject;
        var backImage = back.GetComponent<Image>();
        backImage.sprite = catalog.cardBack; backImage.preserveAspect = true;
        front = Box(card, "Front", new Color(.22f, .19f, .29f), new Vector2(282, 382), Vector2.zero).gameObject;
        Label(front.transform, EventCardCatalog.Title(kind), 27, new Vector2(260, 90), new Vector2(0, 105));
        Label(front.transform, EventCardCatalog.Description(kind), 21, new Vector2(250, 230), new Vector2(0, -45));
        front.SetActive(false);
        choices = Box(panel, "Choices", Color.clear, new Vector2(390, 400), new Vector2(215, -5)).gameObject.AddComponent<CanvasGroup>();
        choices.GetComponent<Image>().raycastTarget = false;
        Label(choices.transform, EventCardCatalog.Description(kind), 24, new Vector2(360, 200), new Vector2(0, 90));
        if (kind == EventCardKind.Victoria)
        {
            Button(choices.transform, "前往商店", new Vector2(290, 48), new Vector2(0, -60), () => system.ResolveCard(true));
            Button(choices.transform, "稍后再说", new Vector2(290, 48), new Vector2(0, -120), () => system.ResolveCard(false));
        }
        else Button(choices.transform, kind == EventCardKind.Meteor ? "领取 200 思绪" : "继续", new Vector2(290, 48),
            new Vector2(0, -90), () => system.ResolveCard(false));
        error = Label(panel, "", 18, new Vector2(880, 40), new Vector2(0, -270));
        choices.alpha = 0; choices.interactable = false; choices.blocksRaycasts = false;
    }

    public IEnumerator Flip()
    {
        yield return new WaitForSecondsRealtime(.2f);
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .25f)
        { card.localScale = new Vector3(1 - t, 1, 1); yield return null; }
        back.SetActive(false); front.SetActive(true);
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .25f)
        { card.localScale = new Vector3(t, 1, 1); yield return null; }
        card.localScale = Vector3.one;
    }

    public void EnableChoices()
    { choices.alpha = 1; choices.interactable = true; choices.blocksRaycasts = true; }
    public void ShowError(string message) => error.text = message;

    public void ShowDeck(int[] counts)
    {
        draft = (int[])counts.Clone();
        NewPanel("编辑下一轮牌组");
        Label(panel, "每种牌调试持有 99 张 · 配满 20 张后随机抽取 12 张 · 本轮牌序锁定", 19,
            new Vector2(900, 45), new Vector2(0, 218));
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            RectTransform tile = Box(panel, "Deck Card " + i, new Color(.22f, .19f, .29f),
                new Vector2(280, 320), new Vector2((i - 1) * 300, 10));
            Label(tile, EventCardCatalog.Title((EventCardKind)i), 23, new Vector2(270, 55), new Vector2(0, 120));
            Label(tile, EventCardCatalog.Description((EventCardKind)i), 18, new Vector2(250, 140), new Vector2(0, 28));
            countsText[i] = Label(tile, "", 18, new Vector2(260, 50), new Vector2(0, -68));
            minus[i] = Button(tile, "−", new Vector2(85, 42), new Vector2(-60, -120), () => Change(index, -1));
            plus[i] = Button(tile, "+", new Vector2(85, 42), new Vector2(60, -120), () => Change(index, 1));
        }
        deckTotal = Label(panel, "", 22, new Vector2(880, 36), new Vector2(0, -186));
        confirm = Button(panel, "确认牌组并开始下一轮", new Vector2(350, 50), new Vector2(0, -245), () => system.ConfirmDeck(draft));
        RefreshDraft();
    }

    private void Change(int index, int amount)
    { draft[index] = Mathf.Clamp(draft[index] + amount, 0, 99); RefreshDraft(); }

    private void RefreshDraft()
    {
        int total = draft[0] + draft[1] + draft[2];
        for (int i = 0; i < 3; i++)
        {
            countsText[i].text = $"已放入 {draft[i]} 张 · 剩余 {99 - draft[i]} 张";
            minus[i].interactable = draft[i] > 0;
            plus[i].interactable = total < 20 && draft[i] < 99;
        }
        deckTotal.text = $"牌组：{total} / 20" + (total == 20 ? " · 可以开始" : " · 请配满 20 张");
        confirm.interactable = EventDeckState.IsValidDeck(draft);
    }

    private RectTransform Box(Transform parent, string name, Color color, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = size; rect.anchoredPosition = position;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private Text Label(Transform parent, string content, int size, Vector2 dimensions, Vector2 position)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = dimensions; rect.anchoredPosition = position;
        var text = go.GetComponent<Text>();
        text.font = catalog.font; text.fontSize = size; text.text = content; text.color = new Color(.96f, .9f, .77f);
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        return text;
    }

    private Button Button(Transform parent, string title, Vector2 size, Vector2 position, UnityAction action)
    {
        var rect = Box(parent, title, new Color(.38f, .29f, .23f), size, position);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>(); button.onClick.AddListener(action);
        Label(rect, title, 18, size - new Vector2(10, 0), Vector2.zero);
        return button;
    }
}
