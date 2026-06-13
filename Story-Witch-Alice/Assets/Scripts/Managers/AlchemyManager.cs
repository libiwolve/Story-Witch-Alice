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
    
    private Dictionary<string, ElementData> unlockedRecipeDictionary = new Dictionary<string, ElementData>();
    private Dictionary<string, List<RecipeData>> ingredientToRecipesDictionary = new Dictionary<string, List<RecipeData>>();
    private HashSet<string> unlockedElementIDs = new HashSet<string>();
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
    public AudioClip newProductSound;       // 新产物 "不灵不灵" 音效
    public AudioClip existingProductSound;  // 已有产物普通音效
    
    [Header("Synthesis Animation")]
    public float flyUpDuration = 0.5f;  // 产物从锅内飞到悬停位置的时长

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
                if (!ingredientToRecipesDictionary.ContainsKey(ingredient.elementID))
                {
                    ingredientToRecipesDictionary[ingredient.elementID] = new List<RecipeData>();
                }
                ingredientToRecipesDictionary[ingredient.elementID].Add(recipe);
            }
        }

        UnlockBaseElement("water");
        UnlockBaseElement("fire");
        UnlockBaseElement("stone");
        UnlockBaseElement("air");
        UnlockBaseElement("time");
        UnlockBaseElement("alice_base");

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
            bool isNew = !unlockedElementIDs.Contains(result.elementID);

            if (isNew)
            {
                OnElementCrafted(result);
            }

            AddLog($"合成成功！产物: {result.elementName}");

            // 播放合成瞬间音效
            PlaySound(synthesisSound);

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
        }
        else
        {
            AddLog("合成失败，原料不匹配任何配方");
            currentIngredients.Clear();
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

        // 播放特殊音效 + 光效
        SpriteRenderer sr = product.GetComponent<SpriteRenderer>();
        if (isNew)
        {
            // 新产物：不灵不灵音效 + 金光
            PlaySound(newProductSound);
            if (sr != null)
                SynthesisGlow.AttachTo(product, new Color(1f, 0.85f, 0.2f, 0.8f), sr.sprite);
        }
        else
        {
            // 已有产物：普通音效 + 蓝光
            PlaySound(existingProductSound);
            if (sr != null)
                SynthesisGlow.AttachTo(product, new Color(0.3f, 0.5f, 1f, 0.8f), sr.sprite);
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

    public void ClearPot()
    {
        currentIngredients.Clear();
        AddLog("锅已清空");
    }

    /// <summary>
    /// 按优先级选择产物 prefab：
    /// 1. elementData 有 icon → physicsElementPrefab
    /// 2. 存在 Physic{id}.prefab → 专属 prefab（如 magma → Physicmagma）
    /// 3. 都没有 → defaultElementPrefab（粉紫色马赛克）
    /// </summary>
    GameObject GetPrefabForElement(ElementData element)
    {
        // 有 icon → 通用模板（Start() 里会覆盖 sprite 为 icon）
        if (element.elementIcon != null && physicsElementPrefab != null)
            return physicsElementPrefab;

        // 按 elementID 自动查找同名专属 prefab
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(element.elementID))
        {
            string path = $"Assets/Prefabs/Physic{element.elementID}.prefab";
            GameObject specific = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (specific != null) return specific;
        }
#endif

        // 回退 → 默认粉紫色马赛克
        if (defaultElementPrefab != null) return defaultElementPrefab;

        // 最终 fallback
        return physicsElementPrefab;
    }

    string GetRecipeKey(List<ElementData> ingredients)
    {
        List<string> ids = ingredients.Select(x => x.elementID).ToList();
        ids.Sort();
        return string.Join("_", ids);
    }
}
