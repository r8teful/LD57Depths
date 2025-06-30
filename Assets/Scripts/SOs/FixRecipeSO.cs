using UnityEngine;

[CreateAssetMenu(fileName = "FixRecipeSO", menuName = "ScriptableObjects/FixRecipeSO", order = 9)]
public class FixRecipeSO : RecipeBaseSO {
    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        if(context.Entity != null) {
            context.Entity.SetFixed();
            return true;
        } else {
            return false;
        }
    }
}