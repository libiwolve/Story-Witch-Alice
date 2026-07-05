using UnityEngine;
using UnityEngine.EventSystems;

public class StarChartToggle : MonoBehaviour,IPointerClickHandler
{
    public GameObject starChart;
    public GameObject starChartCanvas;
    public GameObject recipePanelCanvas;
    public GameObject starChartBackground; // 新增：星空背景 SpriteRenderer

    void Start()
    {
        if (starChart != null) starChart.SetActive(false);
        if (starChartCanvas != null) starChartCanvas.SetActive(false);
        if (recipePanelCanvas != null) recipePanelCanvas.SetActive(false);
        if (starChartBackground != null) starChartBackground.SetActive(false);
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

    void Toggle()
    {
        Debug.Log("Toggle called");
        bool isActive = starChart != null && !starChart.activeSelf;
        if (starChart != null) starChart.SetActive(isActive);
        if (starChartCanvas != null) starChartCanvas.SetActive(isActive);
        if (recipePanelCanvas != null) recipePanelCanvas.SetActive(isActive);
        if (starChartBackground != null) starChartBackground.SetActive(isActive);
    }

    /// <summary>
    /// 供关闭按钮调用，强制关闭所有窗口
    /// </summary>
    public void CloseAll()
    {
        if (starChart != null) starChart.SetActive(false);
        if (starChartCanvas != null) starChartCanvas.SetActive(false);
        if (recipePanelCanvas != null) recipePanelCanvas.SetActive(false);
        if (starChartBackground != null) starChartBackground.SetActive(false);
    }
}