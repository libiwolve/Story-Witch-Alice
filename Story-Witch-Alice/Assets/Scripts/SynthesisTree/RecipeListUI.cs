using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class RecipeListUI : MonoBehaviour
{
    [Header("UI引用")]
    public TMP_InputField searchInput;
    public RectTransform contentParent;   // ScrollView的Content
    public GameObject recipeEntryPrefab;  // 每条配方条目预制体

    [Header("星盘引用")]
    public SynthesisGraph synthesisGraph;

    private List<RecipeData> allRecipes = new List<RecipeData>();
    private List<GameObject> entries = new List<GameObject>();

    void Start()
    {
        allRecipes = AlchemyManager.Instance.allRecipes.ToList();
        searchInput.onValueChanged.AddListener(OnSearchChanged);
        RefreshList("");
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
        foreach (var recipe in allRecipes)
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
            
            // 尝试获取 TMP_Text 或 旧版 Text
            var formulaText = entry.GetComponentInChildren<TMP_Text>();
            if (formulaText != null)
            {
                string ingStr = string.Join(" + ", recipe.ingredients.Select(i => i.elementName));
                formulaText.text = $"{ingStr} → {recipe.product.elementName}";
            }

            // 设置产物图标
            var iconImage = entry.transform.Find("ProductIcon")?.GetComponent<Image>();
            if (iconImage != null && recipe.product.elementIcon != null)
                iconImage.sprite = recipe.product.elementIcon;

            // 点击事件
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