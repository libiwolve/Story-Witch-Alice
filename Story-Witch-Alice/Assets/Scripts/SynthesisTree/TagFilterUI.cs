using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class TagFilterUI : MonoBehaviour
{
    [Header("UI引用")]
    public Transform tagButtonContainer;
    public GameObject tagButtonPrefab;

    [Header("星盘引用")]
    public SynthesisGraph synthesisGraph;

    [Header("防止滚轮冲突")]
    public ScrollRect tagScrollRect;

    [Header("管理面板")]
    public GameObject tagManagerPanel;

    [Header("Tag配置文件")]
    public TagConfig tagConfig;

    private List<string> enabledTags = new List<string>();
    private Dictionary<string, Button> tagButtons = new Dictionary<string, Button>();

    void Start()
    {
        if (tagConfig != null)
            enabledTags = new List<string>(tagConfig.enabledTags);

        CreateTagButtons();
        SetupScrollEventBlocker();
    }

    void SetupScrollEventBlocker()
    {
        if (tagScrollRect == null || synthesisGraph == null) return;

        EventTrigger trigger = tagScrollRect.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = tagScrollRect.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { synthesisGraph.enableZoom = false; });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { synthesisGraph.enableZoom = true; });
        trigger.triggers.Add(exitEntry);
    }

    void CreateTagButtons()
    {
        foreach (var kvp in tagButtons)
            Destroy(kvp.Value.gameObject);
        tagButtons.Clear();

        foreach (string tag in enabledTags)
        {
            GameObject btnGO = Instantiate(tagButtonPrefab, tagButtonContainer);
            var tmpText = btnGO.GetComponentInChildren<TMP_Text>();
            if (tmpText != null) tmpText.text = tag;

            Button btn = btnGO.GetComponent<Button>();
            string capturedTag = tag;
            btn.onClick.AddListener(() => OnTagButtonClicked(capturedTag));
            tagButtons[tag] = btn;
        }
    }

    public void RefreshTagButtons()
    {
        if (tagConfig != null)
            enabledTags = new List<string>(tagConfig.enabledTags);
        CreateTagButtons();
    }

    void OnTagButtonClicked(string tag)
    {
        List<string> targetIDs = new List<string>();
        if (AlchemyManager.Instance != null && AlchemyManager.Instance.allElements != null)
        {
            foreach (var element in AlchemyManager.Instance.allElements)
            {
                if (element != null && element.tags != null && element.tags.Contains(tag))
                    targetIDs.Add(element.elementID);
            }
        }

        if (synthesisGraph != null)
            synthesisGraph.HighlightNodesByTag(targetIDs);
    }

    public void OpenManagerPanel()
    {
        if (tagManagerPanel != null)
            tagManagerPanel.SetActive(true);
    }

    private bool IsSystemTag(string tag)
    {
        string lower = tag.ToLower();
        return lower == "basic" || lower == "material" || lower == "relic" || 
               lower == "reward" || lower == "alice" || lower == "initial" || 
               lower == "accessory";
    }

#if UNITY_EDITOR
    [ContextMenu("从数据库追加新Tag")]
    void ImportTagsFromDatabase()
    {
        if (tagConfig == null)
        {
            Debug.LogError("请先拖入 TagConfig.asset");
            return;
        }

        HashSet<string> existing = new HashSet<string>(tagConfig.enabledTags);
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ElementData");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ElementData elem = UnityEditor.AssetDatabase.LoadAssetAtPath<ElementData>(path);
            if (elem == null || elem.tags == null) continue;
            foreach (string tag in elem.tags)
            {
                if (!IsSystemTag(tag))
                    existing.Add(tag);
            }
        }

        tagConfig.enabledTags = existing.OrderBy(t => t).ToList();
        UnityEditor.EditorUtility.SetDirty(tagConfig);
        Debug.Log($"Tag 列表已更新，目前共 {tagConfig.enabledTags.Count} 个Tag。");
    }
#endif
}