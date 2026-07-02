using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AlchemyManager : MonoBehaviour
{
    public RecipeData[] allRecipes;
    public ElementData[] allElements;
    
    private Dictionary<string, ElementData> unlockedRecipeDictionary = new Dictionary<string, ElementData>();
    private Dictionary<string, List<RecipeData>> ingredientToRecipesDictionary = new Dictionary<string, List<RecipeData>>();
    private HashSet<string> unlockedElementIDs = new HashSet<string>();
    private HashSet<string> synthesizedProductIDs = new HashSet<string>();
    public Text logText;
    private List<string> logLines = new List<string>();
    private const int maxLogLines = 6;

    public static AlchemyManager Instance { get; private set; }

    // 预制体和场景引用
    public GameObject physicsElementPrefab;     // 有图标的产物用
    public GameObject defaultElementPrefab;     // 无图标的产物用（粉紫色马赛克）
    public Transform potTransform;
    public Transform treePanelTransform;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip synthesisSound;       // 合成瞬间音效
    public AudioClip newProductSound;       // 新产物音效
    public AudioClip existingProductSound;  // 已有产物普通音效
    public AudioClip failSound;             // 合成失败音效
    public AudioClip ingredientSound;       // 原料入锅音效
    
    [Header("Synthesis Animation")]
    public float flyUpDuration = 0.5f;  // 产物从锅内飞到悬停位置的时长

    [Header("Orbit System")]
    public ThoughtOrbit thoughtOrbit;

    [Header("Glow Effects")]
    public RuntimeAnimatorController goldGlowController;  // 新产物：金色光晕
    public RuntimeAnimatorController blueGlowController; 
    
    [Header("Pot Animation")]
    public PotGemController potGemController; // 已有产物：蓝色光晕

    [Header("Star Chart")]
    public SynthesisGraph synthesisGraph;

    [Header("Recipe Panel")]
    public RecipeListUI recipeListUI;

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

#if UNITY_EDITOR
        AutoLoadAssets();
#endif

        foreach (var recipe in allRecipes)
        {
            if (recipe == null || recipe.ingredients == null) continue;
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient == null) continue;
                if (!ingredientToRecipesDictionary.ContainsKey(ingredient.elementID))
                {
                    ingredientToRecipesDictionary[ingredient.elementID] = new List<RecipeData>();
                }
                ingredientToRecipesDictionary[ingredient.elementID].Add(recipe);
            }
        }

        UnlockBaseElement("fire");
        UnlockBaseElement("air");
        UnlockBaseElement("water");
        UnlockBaseElement("soil");

        TryUnlockRecipent("fire");
        TryUnlockRecipent("air");
        TryUnlockRecipent("water");
        TryUnlockRecipent("soil");
    }

#if UNITY_EDITOR
    /// <summary>
    /// 自动从项目加载所有 RecipeData 和 ElementData，无需手动拖拽
    /// </summary>
    public void AutoLoadAssets()
    {
        // 强制重新加载，覆盖掉之前拖入的 Missing 引用
        {
            string[] guids = AssetDatabase.FindAssets("t:RecipeData");
            List<RecipeData> valid = new List<RecipeData>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                RecipeData r = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                if (r != null && r.product != null && r.ingredients != null && r.ingredients.Count > 0)
                    valid.Add(r);
            }
            allRecipes = valid.ToArray();
        }

        // 强制重新加载所有 ElementData
        {
            string[] guids = AssetDatabase.FindAssets("t:ElementData");
            allElements = new ElementData[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                allElements[i] = AssetDatabase.LoadAssetAtPath<ElementData>(path);
            }
        }
    }
#endif

#if UNITY_EDITOR
    [MenuItem("Tools/Refresh AlchemyManager")]
    public static void RefreshFromProject()
    {
        var mgr = FindFirstObjectByType<AlchemyManager>();
        if (mgr != null)
        {
            mgr.AutoLoadAssets();
            EditorUtility.SetDirty(mgr);
            AssetDatabase.SaveAssets();
            Debug.Log("AlchemyManager refreshed");
        }
        else
        {
            Debug.LogWarning("场景中找不到 AlchemyManager");
        }
    }
#endif

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
        PlaySound(ingredientSound);
        thoughtOrbit?.MoveToFront(element.elementID);
        AddLog($"放入原料: {element.elementName}，当前锅里有 {currentIngredients.Count} 个原料");

        if (potGemController != null)
            potGemController.OnIngredientAdded(currentIngredients.Count);
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
            bool isNew = !unlockedElementIDs.Contains(result.elementID);

            if (isNew)
            {
                OnElementCrafted(result);
                thoughtOrbit?.AddToFront(result);
            }

            AddLog($"合成成功！产物: {result.elementName}");
            synthesizedProductIDs.Add(result.elementID);
            // 通知星盘添加新节点（后台更新，不自动打开界面）
            if (synthesisGraph != null)
                synthesisGraph.AddNode(result.elementID);
            if (recipeListUI != null)
            {
                recipeListUI.OnNewElementUnlocked();
                Debug.Log($"已通知合成书刷新，当前已合成产物数: {synthesizedProductIDs.Count}");
            }

            // 播放合成瞬间音效 + 新/旧产物音效（在飞出前就播）
            PlaySound(synthesisSound);
            PlaySound(isNew ? newProductSound : existingProductSound);

            // 智能选 prefab：icon → 通用 / 同名专属 → 专属 / 默认 → 马赛克
            GameObject prefabToUse = GetPrefabForElement(result);

            // 在锅内生成产物，然后飞出到锅上方悬停
            if (prefabToUse != null && potTransform != null)
            {
                Vector3 spawnPos = potTransform.position; // 从锅内出发
                Vector3 targetPos = potTransform.position + Vector3.up * 2f; // 悬停在锅上方2单位
                GameObject product = Instantiate(prefabToUse, spawnPos, Quaternion.identity);

                PhysicsElement pe = product.GetComponent<PhysicsElement>();
                if (pe != null)
                {
                    pe.elementData = result;
                    pe.sourceSlot = null;
                }

                Rigidbody2D rb = product.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.gravityScale = 0f;
                    rb.velocity = Vector2.zero;
                }

                // 飞行途中禁用碰撞体，避免误触锅触发器
                Collider2D[] cols = product.GetComponents<Collider2D>();
                foreach (var col in cols)
                    col.enabled = false;

                StartCoroutine(FlyProductUp(product, spawnPos, targetPos, isNew));
            }
            
            

            currentIngredients.Clear();
            if (potGemController != null)
            {    
                potGemController.OnPotCleared();
            }
        }
        else
        {
            PlaySound(failSound);
            AddLog("合成失败，原料不匹配任何配方");
            currentIngredients.Clear();
            if (potGemController != null)  
            {
                potGemController.OnPotCleared();
            }
        }
    }

    // ========== 音效 ==========
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // ========== 产物飞出动画 ==========
    IEnumerator FlyProductUp(GameObject product, Vector3 from, Vector3 to, bool isNew)
    {
        float elapsed = 0f;
        while (elapsed < flyUpDuration)
        {
            if (product == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / flyUpDuration;
            // ease-out cubic：先快后慢，有"弹出来"的感觉
            t = 1f - Mathf.Pow(1f - t, 3f);
            product.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        if (product == null) yield break;
        product.transform.position = to;

        // 飞行结束，重新启用碰撞体，允许玩家拖拽
        Collider2D[] cols = product.GetComponents<Collider2D>();
        foreach (var col in cols)
            col.enabled = true;

        // 播放光效（音效已经在合成瞬间播过了）
        if (isNew && goldGlowController != null)
        {
            GameObject glowObj = new GameObject("GoldenGlow");
            glowObj.transform.SetParent(product.transform);
            glowObj.transform.localPosition = Vector3.zero;
            glowObj.transform.localScale = Vector3.one * 0.6f;

            SpriteRenderer sr = glowObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Foreground";

            Animator anim = glowObj.AddComponent<Animator>();
            anim.runtimeAnimatorController = goldGlowController;

            Destroy(glowObj, 3f);  // 3 秒后自动销毁
        }
        else if (!isNew && blueGlowController != null)
        {
            GameObject glowObj = new GameObject("BlueGlow");
            glowObj.transform.SetParent(product.transform);
            glowObj.transform.localPosition = Vector3.zero;
            glowObj.transform.localScale = Vector3.one * 0.6f;

            SpriteRenderer sr = glowObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Foreground";

            Animator anim = glowObj.AddComponent<Animator>();
            anim.runtimeAnimatorController = blueGlowController;

            Destroy(glowObj, 3f);
        }
    }

    public void AddLog(string msg)
    {
        logLines.Add(msg);
        if (logLines.Count > maxLogLines)
            logLines.RemoveAt(0);

        if (logText != null)
            logText.text = string.Join("\n", logLines);
    }

    public bool IsElementUnlocked(string elementID)
    {
        return unlockedElementIDs.Contains(elementID);
    }

    public void ClearPot()
    {
        currentIngredients.Clear();
        AddLog("锅已清空");
        if (potGemController != null)
            potGemController.OnPotCleared();
    }

    /// <summary>
    /// 按优先级选择产物 prefab：
    /// 1. 存在 Physic{id}.prefab → 专属 prefab（自带碰撞箱和贴图）
    /// 2. elementData 有 icon → physicsElementPrefab（通用模板，Start 会设正确贴图）
    /// 3. 都没有 → defaultElementPrefab（粉紫色马赛克）
    /// </summary>
    public GameObject GetPrefabForElement(ElementData element)
    {
        // 1. 有专属 prefab → 直接用，碰撞箱、贴图都是自己的
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(element.elementID))
        {
            string path = $"Assets/Prefabs/Physic{element.elementID}.prefab";
            GameObject specific = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (specific != null) return specific;
        }
#endif

        // 2. 有 icon 但没专属 prefab → 通用模板（Start 会设正确贴图）
        if (element.elementIcon != null && physicsElementPrefab != null)
            return physicsElementPrefab;

        // 3. 回退 → 粉紫色马赛克
        return defaultElementPrefab;
    }

    string GetRecipeKey(List<ElementData> ingredients)
    {
        List<string> ids = ingredients.Select(x => x.elementID).ToList();
        ids.Sort();
        return string.Join("_", ids);
    }

    public bool IsRecipeUnlocked(string productID)
    {
        return synthesizedProductIDs.Contains(productID);
    }
}