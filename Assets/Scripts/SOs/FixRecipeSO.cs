using UnityEngine;

[CreateAssetMenu(fileName = "FixRecipeSO", menuName = "ScriptableObjects/FixRecipeSO", order = 9)]
public class FixRecipeSO : RecipeBaseSO {
    public override bool ExecuteRecipe(InventoryManager playerInv) {
        throw new System.NotImplementedException();
    }
}