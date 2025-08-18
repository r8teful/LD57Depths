using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingItemSO", menuName = "ScriptableObjects/Crafting/CraftingItemSO", order = 9)]
public class CraftingItemSO : CraftingRecipeSO {
    [VerticalGroup("Gamepaly/1")]
    public ItemQuantity CraftingResult;
    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        var added = false;
        if (context.PlayerInventory != null) {
            added = context.PlayerInventory.AddItem(CraftingResult.item.ID, CraftingResult.quantity);
        }
        return added;
    }
}