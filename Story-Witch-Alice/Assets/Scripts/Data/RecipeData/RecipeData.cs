using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="newRecipeData", menuName="ScriptableObjects/Recipe", order=1)]
public class RecipeData : ScriptableObject
{
    public List<ElementData> ingredients;
    public ElementData product;
}
