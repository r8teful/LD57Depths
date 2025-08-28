using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeRecipeValue", menuName = "ScriptableObjects/Upgrades/UpgradeRecipeValue", order = 2)]
public class UpgradeRecipeValue : UpgradeRecipeBase {
    public IncreaseType increaseType;
    public float value;

    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        return true; // Handled by events in UpgradeManager.OnUpgradePurchased
        // This is because we'd need mining controller, movement, vision, and more, in the context. It could work, but 
        // gets quite messy to setup the context when calling execute recipe.
        // Trust me, I've tried it, it will just get messy, and in the end we check the individual ID anyway, so if we're 
        // Already doing that, why not just stick with it?
    }
}