using UnityEngine;
using UnityEngine.EventSystems;

public class StarChartToggle : MonoBehaviour, IPointerClickHandler
{
    public GameObject starChart; // 所有星盘相关物体的父物体

    void Start()
    {
        if (starChart != null) starChart.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            Toggle();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Toggle();
    }

    public void CloseAll()
    {
        if (starChart != null) starChart.SetActive(false);
    }

    void Toggle()
    {
        if (starChart != null)
            starChart.SetActive(!starChart.activeSelf);
    }
}