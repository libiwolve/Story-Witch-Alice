using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TagConfig", menuName = "TagConfig", order = 0)]
public class TagConfig : ScriptableObject
{
    [Tooltip("当前启用的 Tag 列表")]
    public List<string> enabledTags = new List<string>();
}