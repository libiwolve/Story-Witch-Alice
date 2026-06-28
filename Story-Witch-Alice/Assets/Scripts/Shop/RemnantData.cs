using UnityEngine;

/// <summary>
/// 遗民数据资产。右键 Create → RemnantData 创建。
/// </summary>
[CreateAssetMenu(fileName = "NewRemnant", menuName = "Shop/RemnantData", order = 1)]
public class RemnantData : ScriptableObject
{
    public string remnantID;
    public string remnantName;

    [TextArea(3, 6)]
    public string description;

    public Sprite portrait;            // 立绘（左 1/4 区域展示）
    public Color accentColor = Color.white; // 主题色（扩展预留）

    [Header("Affinity (Future)")]
    public int baseAffinity;
}
