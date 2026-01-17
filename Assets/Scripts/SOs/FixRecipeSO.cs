using UnityEngine;

[CreateAssetMenu(fileName = "FixRecipeSO", menuName = "ScriptableObjects/Crafting/FixRecipeSO", order = 9)]
public class FixRecipeSO : RecipeBaseSO {
    public override void Execute(ExecutionContext context) {
        if (context.Source == null) return;
        if (context.Source.TryGetComponent<FixableEntity>(out var f)) {
            f.SetFixedRpc(true); // Send message to server and then we change visuals via OnChange
        }
    }
}