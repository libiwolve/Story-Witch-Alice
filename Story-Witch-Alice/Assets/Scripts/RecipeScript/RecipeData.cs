using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="newRecipeData", menuName="ScriptableObjects/Recipe", order=1)]
public class RecipeData : ScriptableObject
{
    public List<ElementData> ingredients;
    public ElementData product;
    [Header("Synthesis conditions")]
    [Tooltip("留空表示无条件合成。未来可填入属性要求，如 'temperature>=100'")]
    public string condition; 
}
