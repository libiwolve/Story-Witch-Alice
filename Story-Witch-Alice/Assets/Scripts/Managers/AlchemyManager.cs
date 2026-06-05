using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class AlchemyManager : MonoBehaviour
{
    public RecipeData[] allRecipes;
    

    private Dictionary<string, ElementData> unlockedRecipeDictionary = new Dictionary<string, ElementData>();
    private Dictionary<string, List<RecipeData>> ingredientToRecipesDictionary = new Dictionary<string, List<RecipeData>>();
    private HashSet<string> unlockedElementIDs = new HashSet<string>();
    public Text logText;              // Inspector 里拖入
    private List<string> logLines = new List<string>();
    private const int maxLogLines = 6; // 最多显示 6 行

    public static AlchemyManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 构建原料 → 配方索引
        foreach (var recipe in allRecipes)
        {
            foreach (var ingredient in recipe.ingredients)
            {
                if (!ingredientToRecipesDictionary.ContainsKey(ingredient.elementID))
                {
                    ingredientToRecipesDictionary[ingredient.elementID] = new List<RecipeData>();
                }
                ingredientToRecipesDictionary[ingredient.elementID].Add(recipe);
            }
        }

        // 初始解锁 5 个基础元素
        UnlockBaseElement("water");
        UnlockBaseElement("fire");
        UnlockBaseElement("stone");
        UnlockBaseElement("air");
        UnlockBaseElement("time");

        // 初始解锁 alice_base（爱丽丝本体应该一开始就能用）
        UnlockBaseElement("alice_base");

        // 触发首批配方解锁
        TryUnlockRecipent("water");
        TryUnlockRecipent("fire");
        TryUnlockRecipent("stone");
        TryUnlockRecipent("air");
        TryUnlockRecipent("time");
        TryUnlockRecipent("alice_base");
    }

    void UnlockBaseElement(string elementID)
    {
        unlockedElementIDs.Add(elementID);
    }

    public void OnElementCrafted(ElementData newElement)
    {
        if (unlockedElementIDs.Contains(newElement.elementID)) return;
        unlockedElementIDs.Add(newElement.elementID);
        TryUnlockRecipent(newElement.elementID);
    }

    void TryUnlockRecipent(string elementID)
    {
        if (!ingredientToRecipesDictionary.ContainsKey(elementID)) return;

        foreach (var recipe in ingredientToRecipesDictionary[elementID])
        {
            bool canUnlock = true;
            foreach (var ingredient in recipe.ingredients)
            {
                if (!unlockedElementIDs.Contains(ingredient.elementID))
                {
                    canUnlock = false;
                    break;
                }
            }
            if (canUnlock)
            {
                string key = GetRecipeKey(recipe.ingredients);
                if (!unlockedRecipeDictionary.ContainsKey(key))
                {
                    unlockedRecipeDictionary.Add(key, recipe.product);
                }
            }
        }
    }

    public ElementData TryCombine(List<ElementData> ingredients)
    {
        string key = GetRecipeKey(ingredients);
        if (unlockedRecipeDictionary.TryGetValue(key, out ElementData product))
        {
            return product;
        }
        return null;
    }

    private List<ElementData> currentIngredients = new List<ElementData>();

    public void AddIngredient(ElementData element)
    {
        if (currentIngredients.Count >= 3)
        {
            AddLog("锅里已经满了，最多放3个原料");
            return;
        }

        currentIngredients.Add(element);
        AddLog($"放入原料: {element.elementName}，当前锅里有 {currentIngredients.Count} 个原料");
    }

    public void ManualCombine()
    {
        if (currentIngredients.Count < 2)
        {
            AddLog("至少需要2个原料才能合成");
            return;
        }

        ElementData result = TryCombine(currentIngredients);

        if (result != null)
        {
            AddLog($"合成成功！产物: {result.elementName}");
            OnElementCrafted(result);
            currentIngredients.Clear();
        }
        else
        {
            AddLog("合成失败，原料不匹配任何配方");
            currentIngredients.Clear();
        }
    }
    void AddLog(string msg)
    {
        logLines.Add(msg);
        if (logLines.Count > maxLogLines)
            logLines.RemoveAt(0);

        if (logText != null)
            logText.text = string.Join("\n", logLines);
    }

    public void ClearPot()
    {
        currentIngredients.Clear();
        AddLog("锅已清空");
    }

    string GetRecipeKey(List<ElementData> ingredients)
    {
        List<string> ids = ingredients.Select(x => x.elementID).ToList();
        ids.Sort();
        return string.Join("_", ids);
    }
}