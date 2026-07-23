using UnityEngine;
using UnityEngine.UI;

public class CloseButton : MonoBehaviour
{
    public StarChartToggle toggle;
    public Animator cartographerAnimator;      // 制图机的 Animator
    public string closeTrigger = "Close";

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            // 先播放关闭动画
            if (cartographerAnimator != null)
                cartographerAnimator.SetTrigger(closeTrigger);

            // 等动画播完再关闭（或者直接关闭，看你设计）
            toggle?.CloseAll();
        });
    }
}