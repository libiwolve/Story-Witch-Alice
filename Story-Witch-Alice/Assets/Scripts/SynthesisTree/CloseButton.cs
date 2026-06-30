using UnityEngine;
using UnityEngine.UI;

public class CloseButton : MonoBehaviour
{
    public StarChartToggle toggle;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => toggle.CloseAll());
    }
}