using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName="ElementData",menuName="ScriptableObjects/Element",order=0)]
public class ElementData : ScriptableObject
{
    // Start is called before the first frame update
    public string elementName;
    public string elementID;
    [TextArea]
    public string description;
    public List<string> tags;
    
    public List<ElementProperty> properties;

}

[System.Serializable]
public class ElementProperty
{
    public string key;
    public float value;
}