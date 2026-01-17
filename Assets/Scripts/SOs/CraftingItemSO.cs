using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingItemSO", menuName = "ScriptableObjects/Crafting/CraftingItemSO", order = 9)]
public class CraftingItemSO : CraftingRecipeSO {
    [VerticalGroup("Gamepaly/1")]
    public ItemQuantity CraftingResult;
    public override void Execute(ExecutionContext context) {
        if (context.Player != null) {
            context.Player.GetInventory().AddItem(CraftingResult.item.ID, CraftingResult.quantity);
        }
    }
}