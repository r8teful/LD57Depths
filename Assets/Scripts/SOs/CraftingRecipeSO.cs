using Sirenix.OdinInspector;
using UnityEngine;
[CreateAssetMenu(fileName = "CraftingRecipeSO", menuName = "ScriptableObjects/CraftingRecipeSO", order = 9)]
public class CraftingRecipeSO : RecipeBaseSO {
    [VerticalGroup("Gamepaly/1")]
    public ItemQuantity CraftingResult;
    public override bool ExecuteRecipe(InventoryManager playerInv) {
        var added = false;
        Debug.Log("crafted complete!");
        if (playerInv != null) {
            added = playerInv.AddItem(CraftingResult.item.ID, CraftingResult.quantity);
        }
        return added;
    }
}