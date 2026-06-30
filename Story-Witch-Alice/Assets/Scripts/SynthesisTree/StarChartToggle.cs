using UnityEngine;

public class StarChartToggle : MonoBehaviour
{
    public GameObject starChart;
    public GameObject starChartCanvas;
    public GameObject recipePanelCanvas;

    void Start()
    {
        if (starChart != null) starChart.SetActive(false);
        if (starChartCanvas != null) starChartCanvas.SetActive(false);
        if (recipePanelCanvas != null) recipePanelCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            Toggle();
    }

    void OnMouseDown()
    {
        Toggle();
    }

    void Toggle()
    {
        bool isActive = starChart != null && !starChart.activeSelf;
        if (starChart != null) starChart.SetActive(isActive);
        if (starChartCanvas != null) starChartCanvas.SetActive(isActive);
        if (recipePanelCanvas != null) recipePanelCanvas.SetActive(isActive);
    }

    /// <summary>
    /// 供关闭按钮调用，强制关闭所有窗口
    /// </summary>
    public void CloseAll()
    {
        if (starChart != null) starChart.SetActive(false);
        if (starChartCanvas != null) starChartCanvas.SetActive(false);
        if (recipePanelCanvas != null) recipePanelCanvas.SetActive(false);
    }
}