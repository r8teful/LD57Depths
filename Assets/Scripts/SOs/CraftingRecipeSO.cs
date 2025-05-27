using FishNet.Connection;
using UnityEngine;
[CreateAssetMenu(fileName = "CraftingRecipeSO", menuName = "ScriptableObjects/CraftingRecipeSO", order = 9)]
public class CraftingRecipeSO : RecipeBaseSO {
    public override bool ExecuteRecipe(NetworkConnection crafterConnection, InventoryManager clientInventory) {
        Debug.Log("crafted complete!");
        return true;
    }
}