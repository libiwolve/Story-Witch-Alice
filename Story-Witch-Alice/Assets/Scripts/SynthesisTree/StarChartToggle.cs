using UnityEngine;

public class StarChartToggle : MonoBehaviour
{
    public GameObject starChart;          // 星盘根物体（世界空间）
    public GameObject starChartCanvas;    // 星盘背景 Canvas（含 ViewportMask）
    public GameObject recipePanelCanvas;  // 合成书 Canvas

    void Start()
    {
        // 初始隐藏
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
        // 判断当前状态（以其中一个物体为准）
        bool isActive = starChart != null && !starChart.activeSelf;

        if (starChart != null) starChart.SetActive(isActive);
        if (starChartCanvas != null) starChartCanvas.SetActive(isActive);
        if (recipePanelCanvas != null) recipePanelCanvas.SetActive(isActive);
    }
}