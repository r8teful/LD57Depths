using UnityEngine;

[CreateAssetMenu(fileName = "FixRecipeSO", menuName = "ScriptableObjects/Crafting/FixRecipeSO", order = 9)]
public class FixRecipeSO : RecipeBaseSO {
    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        if (context.Source == null) return false;

        if (context.Source.TryGetComponent<FixableEntity>(out var f)) {
            f.SetFixedRpc(true); // Send message to server and then we change visuals via OnChange
            return true;
        } else {
            Debug.LogError("Could not find fixableentity on called component!");
            return false;
        }

    }
}