using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ElementDisposalManager : MonoBehaviour
{
    public static ElementDisposalManager Instance { get; private set; }

    [Header("烟雾粒子")]
    public GameObject disposeSmokePrefab;

    [Header("按键")]
    public KeyCode clearAllKey = KeyCode.Delete;

    private List<PhysicsElement> markedElements = new List<PhysicsElement>();
    private bool isDisposing = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(clearAllKey) && !isDisposing)
        {
            StartCoroutine(DisposeAllMarkedElements());
        }
    }

    public void MarkElement(PhysicsElement element)
    {
        if (!markedElements.Contains(element))
            markedElements.Add(element);
    }

    public void UnmarkElement(PhysicsElement element)
    {
        markedElements.Remove(element);
    }

    public List<PhysicsElement> GetMarkedElements()
    {
        return markedElements;
    }

    /// <summary>
    /// 点击魔法棒或按 Delete 键调用
    /// </summary>
    public IEnumerator DisposeAllMarkedElements()
    {
        if (isDisposing) yield break;
        yield return StartCoroutine(DisposeSequence());
    }
    public bool IsDisposing()
    {
        return isDisposing;
    }
    /// <summary>
    /// 供外部（墨水瓶、按钮等）调用，内部启动清理协程
    /// </summary>
    public void TriggerDispose()
    {
        if (!isDisposing)
            StartCoroutine(DisposeAllMarkedElements());
    }

    IEnumerator DisposeSequence()
    {
        isDisposing = true;

        for (int i = markedElements.Count - 1; i >= 0; i--)
        {
            PhysicsElement element = markedElements[i];
            if (element == null)
            {
                markedElements.RemoveAt(i);
                continue;
            }

            if (disposeSmokePrefab != null)
            {
                GameObject smoke = Instantiate(disposeSmokePrefab, element.transform.position, Quaternion.identity);
                Destroy(smoke, 1.5f);
            }

            yield return StartCoroutine(PopAndFade(element.gameObject));

            Destroy(element.gameObject);
            markedElements.RemoveAt(i);

            yield return new WaitForSeconds(0.1f);
        }

        isDisposing = false;

        // ★ 通知墨水瓶播放关闭动画
        InkBottle bottle = FindFirstObjectByType<InkBottle>();
        if (bottle != null)
            bottle.PlayCloseAnimation();
    }

    IEnumerator PopAndFade(GameObject obj)
    {
        Vector3 origScale = obj.transform.localScale;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        Color origColor = sr != null ? sr.color : Color.white;

        float duration = 0.2f;

        // 先放大
        float elapsed = 0f;
        while (elapsed < duration * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration * 0.3f);
            obj.transform.localScale = Vector3.Lerp(origScale, origScale * 1.3f, t);
            yield return null;
        }

        // 缩小 + 淡出
        elapsed = 0f;
        while (elapsed < duration * 0.7f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration * 0.7f);
            obj.transform.localScale = Vector3.Lerp(origScale * 1.3f, Vector3.zero, t);
            if (sr != null)
                sr.color = new Color(origColor.r, origColor.g, origColor.b, 1f - t);
            yield return null;
        }
    }
    
}