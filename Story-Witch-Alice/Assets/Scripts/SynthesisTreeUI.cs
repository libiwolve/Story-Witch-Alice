using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SynthesisTreeUI : MonoBehaviour
{
   public GameObject nodePrefab;
   public Transform content;
   void Start()
    {
        if(AlchemyManager.Instance !=null)
            AlchemyManager.Instance.OnSynthesisCompleted += AddNode;
    }
        void AddNode(SynthesisNode node)
    {
        // 只添加根节点（最终产物），不递归展开子节点
        GameObject go = Instantiate(nodePrefab, content);
        Text text = go.GetComponent<Text>();

        if (text != null)
        {
            // 显示合成公式：原料 → 产物
            string formula = "";
            if (node.children.Count > 0)
            {
                string childrenNames = string.Join(" + ", node.children.Select(c => c.elementData.elementName));
                formula = childrenNames + " → " + node.elementData.elementName;
            }
            else
            {
                formula = node.elementData.elementName;
            }
            text.text = formula;
        }

        Canvas.ForceUpdateCanvases();
        ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.horizontalNormalizedPosition = 1f;
        }
    }
    void OnDestroy()
    {
        if(AlchemyManager.Instance !=null)
            AlchemyManager.Instance.OnSynthesisCompleted -= AddNode;
    }

}
