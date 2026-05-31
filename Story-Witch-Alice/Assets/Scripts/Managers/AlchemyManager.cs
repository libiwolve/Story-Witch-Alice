using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
public class AlchemyManager : MonoBehaviour
{
    //public List<SynthesisNode> synthesisRoots = new List<SynthesisNode>(); // 用于构建合成树的根节点列表
    //public System.Action<SynthesisNode> OnSynthesisCompleted;
    public RecipeData[] allRecipes;
    //已经解锁的配方，Key: 由配方原料的elementID和“_”组成的字符串，Value: 这个配方合成出来的元素数据
    private Dictionary<string, ElementData> unlockedRecipeDictionary = new Dictionary<string, ElementData>();
    // Key: ingredient elementID, Value: List of recipes that can be made with that ingredient
    private Dictionary<string,List<RecipeData>> ingredientToRecipesDictionary = new Dictionary<string, List<RecipeData>>();
    private HashSet<string> unlockedElementIDs = new HashSet<string>();
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
        foreach (var recipe in allRecipes)
        {
            foreach (var ingredient in recipe.ingredients)
            {   
                // Build the ingredient to recipes dictionary,如果不存在这个ingredient的elementID，就创建一个新的List<RecipeData>，然后把这个recipe添加到这个List中
                // 这样就可以快速地根据ingredient的elementID找到所有包含这个ingredient的recipe了
                if (!ingredientToRecipesDictionary.ContainsKey(ingredient.elementID))
                {
                    ingredientToRecipesDictionary[ingredient.elementID] = new List<RecipeData>();
                }
                ingredientToRecipesDictionary[ingredient.elementID].Add(recipe);
            }
        }
        UnlockBaseElement("water");
        UnlockBaseElement("fire");
        UnlockBaseElement("earth");
        UnlockBaseElement("air");
        // 初始的时候，解锁四个基础元素的配方
        TryUnlockRecipent("water");
        TryUnlockRecipent("fire");
        TryUnlockRecipent("earth");
        TryUnlockRecipent("air");
    }
    //解锁一个基础元素
    void UnlockBaseElement(string elementID)
    {
        unlockedElementIDs.Add(elementID);
    }
    //当一个新的元素被合成出来的时候，调用此方法，解锁以它为原料的配方
    public void OnElementCrafted(ElementData newElement)
    {
        if (unlockedElementIDs.Contains(newElement.elementID)) return; // 已经解锁过这个元素了，直接返回
        unlockedElementIDs.Add(newElement.elementID);
        TryUnlockRecipent(newElement.elementID);
    }
    /*
        用一个元素的elementID，尝试解锁以它为原料的配方，如果这个元素没有被任何配方使用过就直接返回，如果有，就检查这些配方的其他原料是否都已经解锁了，如果都解锁了，
        就把这个配方加入到unlockedRecipeDictionary中
    */
    void TryUnlockRecipent(string elementID)
    {
        if(!ingredientToRecipesDictionary.ContainsKey(elementID)) return; // 没有任何配方包含这个元素，直接返回
        foreach ( var recipe in ingredientToRecipesDictionary[elementID])
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
    //尝试合成一个元素，输入是一个元素数据的列表，输出是合成出来的元素数据，如果配方不存在则返回null
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

// 添加原料到锅里
    public void AddIngredient(ElementData element)
    {
        // 最多放3个原料
        if (currentIngredients.Count >= 3)
        {
            Debug.Log("锅里已经满了，最多放3个原料");
            return;
        }

        currentIngredients.Add(element);
        Debug.Log($"放入原料: {element.elementName}，当前锅里有 {currentIngredients.Count} 个原料");

    }

// 手动点击合成按钮，尝试合成当前锅里的原料
    public void ManualCombine()
{
    if (currentIngredients.Count < 2)
    {
        Debug.Log("至少需要2个原料才能合成");
        return;
    }

    ElementData result = TryCombine(currentIngredients);

    if (result != null)
    {
        Debug.Log($"合成成功！产物: {result.elementName}");
        OnElementCrafted(result);
        currentIngredients.Clear();
    } 
    else
    {
        Debug.Log("合成失败，原料不匹配任何配方");
        currentIngredients.Clear();
        // 这里可以触发失败特效（冒烟、抖动等）
    }
}


// 手动清空锅（可以给 UI 的清空按钮调用）
    public void ClearPot()
    {
        currentIngredients.Clear();
        Debug.Log("锅已清空");
    }
    //根据配方原料的elementID和“_”组成一个字符串，作为配方的key
    string GetRecipeKey(List<ElementData> ingredients)
    {
        List<string> ids = ingredients.Select(x => x.elementID).ToList();
        ids.Sort();
        return string.Join("_", ids);
    }
}
