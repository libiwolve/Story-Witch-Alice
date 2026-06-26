using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class StarChartMesh : MonoBehaviour
{
    [Header("连线设置")]
    public float lineWidth = 0.08f;
    public Color lineColor = Color.white;
    public Material lineMaterial;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    // 顶点和索引
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Color> colors = new List<Color>();

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        mesh = new Mesh();
        mesh.MarkDynamic();
        meshFilter.mesh = mesh;
        meshRenderer.material = lineMaterial;
        meshRenderer.sortingLayerName = "UI";
        meshRenderer.sortingOrder = 2;
    }

    /// <summary>
    /// 每帧调用，传入所有连线（每对起止点）
    /// </summary>
    public void UpdateLines(List<(Vector2 from, Vector2 to)> lines)
    {
        vertices.Clear();
        triangles.Clear();
        colors.Clear();

        int idx = 0;
        foreach (var (from, to) in lines)
        {
            Vector2 dir = (to - from).normalized;
            Vector2 perpendicular = new Vector2(-dir.y, dir.x) * lineWidth * 0.5f;

            // 四个顶点组成一个细长矩形
            vertices.Add(from - perpendicular);  // 0
            vertices.Add(from + perpendicular);  // 1
            vertices.Add(to + perpendicular);    // 2
            vertices.Add(to - perpendicular);    // 3

            // 两个三角形
            triangles.Add(idx + 0); triangles.Add(idx + 1); triangles.Add(idx + 2);
            triangles.Add(idx + 0); triangles.Add(idx + 2); triangles.Add(idx + 3);

            // 顶点颜色
            colors.Add(lineColor); colors.Add(lineColor); colors.Add(lineColor); colors.Add(lineColor);

            idx += 4;
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateBounds();
    }
}