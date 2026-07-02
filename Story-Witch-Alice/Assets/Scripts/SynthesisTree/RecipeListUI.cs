using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class RecipeListUI : MonoBehaviour
{
    [Header("UI引用")]
    public TMP_InputField searchInput;
    public RectTransform contentParent;
    public GameObject recipeEntryPrefab;
    public SynthesisGraph synthesisGraph;

    private List<RecipeData> unlockedRecipes = new List<RecipeData>();
    private List<GameObject> entries = new List<GameObject>();

    void Start()
    {
        // 初始加载已解锁的配方
        RefreshUnlockedRecipes();
        RefreshList("");

        searchInput.onValueChanged.AddListener(OnSearchChanged);
    }

    /// <summary>
    /// 从 AlchemyManager 获取所有配方，只保留产物已解锁的
    /// </summary>
    void RefreshUnlockedRecipes()
    {
        unlockedRecipes.Clear();

        if (AlchemyManager.Instance == null) return;

        foreach (var recipe in AlchemyManager.Instance.allRecipes)
        {
            if (recipe.product == null) continue;

            // 只显示产物已经被玩家合成过的配方
            if (AlchemyManager.Instance.IsRecipeUnlocked(recipe.product.elementID))
                unlockedRecipes.Add(recipe);
        }
    }

    /// <summary>
    /// 当有新元素解锁时，由外部调用，刷新列表
    /// </summary>
    public void OnNewElementUnlocked()
    {
        RefreshUnlockedRecipes();
        RefreshList(searchInput != null ? searchInput.text : "");
    }

    void OnSearchChanged(string text)
    {
        RefreshList(text);
    }

    void RefreshList(string filter)
    {
        foreach (var go in entries) Destroy(go);
        entries.Clear();

        string lowerFilter = filter.ToLower();

        foreach (var recipe in unlockedRecipes)
        {
            if (recipe.product == null) continue;

            bool match = string.IsNullOrEmpty(filter);
            if (!match)
            {
                if (recipe.product.elementName.ToLower().Contains(lowerFilter)) match = true;
                else
                {
                    foreach (var ing in recipe.ingredients)
                    {
                        if (ing != null && ing.elementName.ToLower().Contains(lowerFilter))
                        { match = true; break; }
                    }
                }
            }
            if (!match) continue;

            GameObject entry = Instantiate(recipeEntryPrefab, contentParent);

            var formulaText = entry.GetComponentInChildren<TMP_Text>();
            if (formulaText != null)
            {
                string ingStr = string.Join(" + ", recipe.ingredients.Select(i => i.elementName));
                formulaText.text = $"{ingStr} → {recipe.product.elementName}";
            }

            var iconImage = entry.transform.Find("ProductIcon")?.GetComponent<Image>();
            if (iconImage != null && recipe.product.elementIcon != null)
                iconImage.sprite = recipe.product.elementIcon;

            Button btn = entry.GetComponent<Button>();
            if (btn != null)
            {
                string pid = recipe.product.elementID;
                btn.onClick.AddListener(() => synthesisGraph.HighlightNode(pid));
            }

            entries.Add(entry);
        }
    }
}