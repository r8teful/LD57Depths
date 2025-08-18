using UnityEngine;

[CreateAssetMenu(fileName = "FixRecipeSO", menuName = "ScriptableObjects/Crafting/FixRecipeSO", order = 9)]
public class FixRecipeSO : RecipeBaseSO {
    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        if (context.FixableEntity == null) return false;

        if (IsControlPanell(context)) {
            // Tell subInterior on the server
            SubInterior.Instance.SetLadderActiveRpc();
        }
        context.FixableEntity.SetFixedRpc(true); // Send message to server and then we change visuals via OnChange
        return true;
    }

    private bool IsControlPanell(RecipeExecutionContext context) => context.FixableEntity.fixRecipe.ID == ResourceSystem.ControlPanellRecipeID;
}